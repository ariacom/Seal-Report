using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Seal.AI;
using Seal.Model;

namespace SealWebServer
{
    /// <summary>
    /// MCP server of the Web Report Server: the AI agents having the channel MCP are served at /mcp/{agent} to the external AI clients,
    /// the requests are authenticated with the personal access tokens of the users (see <see cref="McpGateway"/>).
    /// </summary>
    public partial class Startup
    {
        /// <summary>Base path of the MCP server</summary>
        public const string McpPath = "/mcp";

        const string McpContextKey = "SealMcpContext";

        class McpCancelOperation : ICancelOperation
        {
            public CancellationToken Token;
            public bool Cancel => Token.IsCancellationRequested;
        }

        static void ConfigureMcpServices(IServiceCollection services)
        {
            services.AddMcpServer(options =>
                {
                    options.ServerInfo = new Implementation()
                    {
                        Name = "Seal Report",
                        Version = Assembly.GetAssembly(typeof(Repository))?.GetName().Version?.ToString() ?? "1.0"
                    };
                })
                .WithHttpTransport(options =>
                {
                    //No session: each request is authenticated and executed on its own (the user and his rights are loaded again for each request)
                    options.SessionMode = HttpServerSessionMode.Stateless;
                    options.ConfigureSessionOptions = (httpContext, serverOptions, cancellationToken) =>
                    {
                        var context = httpContext.Items[McpContextKey] as McpRequestContext;
                        if (context?.Agent == null) throw new Exception("MCP request not authenticated");
                        configureAgent(context, serverOptions);
                        return Task.CompletedTask;
                    };
                });
        }

        static void configureAgent(McpRequestContext context, McpServerOptions serverOptions)
        {
            serverOptions.ServerInstructions = McpGateway.GetInstructions(context);
            serverOptions.Capabilities ??= new ServerCapabilities();
            serverOptions.Capabilities.Tools = new ToolsCapability();
            serverOptions.Capabilities.Prompts = new PromptsCapability();
            serverOptions.Capabilities.Resources = new ResourcesCapability();

            serverOptions.Handlers.ListToolsHandler = (request, cancellationToken) =>
            {
                var tools = McpGateway.GetToolHost(context).GetTools().Select(t => new Tool()
                {
                    Name = t.Name,
                    Description = t.Description,
                    InputSchema = getInputSchema(t.ParametersSchema),
                    Annotations = getAnnotations(t.Access)
                }).ToList();
                return ValueTask.FromResult(new ListToolsResult() { Tools = tools });
            };

            serverOptions.Handlers.CallToolHandler = async (request, cancellationToken) =>
            {
                var name = request.Params?.Name;
                var arguments = request.Params?.Arguments != null ? JsonSerializer.Serialize(request.Params.Arguments) : "{}";
                var cancel = new McpCancelOperation() { Token = cancellationToken };
                var result = await runWithProgress(request.Server, request.Params?.ProgressToken, () => McpGateway.GetProgressLabel(context, name, arguments),
                    () => McpGateway.ExecuteTool(context, name, arguments, cancel), cancellationToken);

                var callResult = new CallToolResult() { IsError = McpGateway.IsError(result) };
                callResult.Content.Add(new TextContentBlock() { Text = string.IsNullOrEmpty(result.Text) ? " " : result.Text });
                foreach (var image in result.Images) callResult.Content.Add(ImageContentBlock.FromBytes(image.Data, image.MediaType));
                return callResult;
            };

            serverOptions.Handlers.ListPromptsHandler = (request, cancellationToken) =>
            {
                var prompts = McpGateway.GetPrompts(context).Select(p => new Prompt() { Name = p.Name, Title = p.Text, Description = p.Text }).ToList();
                return ValueTask.FromResult(new ListPromptsResult() { Prompts = prompts });
            };

            serverOptions.Handlers.GetPromptHandler = (request, cancellationToken) =>
            {
                var prompt = McpGateway.GetPrompts(context).FirstOrDefault(p => p.Name == request.Params?.Name);
                if (prompt == null) throw new McpProtocolException($"Unknown prompt '{request.Params?.Name}'", McpErrorCode.InvalidParams);
                var result = new GetPromptResult() { Description = prompt.Text };
                result.Messages.Add(new PromptMessage() { Role = Role.User, Content = new TextContentBlock() { Text = prompt.Text } });
                return ValueTask.FromResult(result);
            };

            serverOptions.Handlers.ListResourcesHandler = (request, cancellationToken) =>
            {
                var resources = McpGateway.GetResources(context).Select(r => new Resource() { Uri = r.Uri, Name = r.Name, Description = r.Description, MimeType = "text/plain" }).ToList();
                return ValueTask.FromResult(new ListResourcesResult() { Resources = resources });
            };

            serverOptions.Handlers.ListResourceTemplatesHandler = (request, cancellationToken) =>
            {
                var templates = McpGateway.GetResourceTemplates(context).Select(r => new ResourceTemplate() { UriTemplate = r.UriTemplate, Name = r.Name, Description = r.Description, MimeType = "text/plain" }).ToList();
                return ValueTask.FromResult(new ListResourceTemplatesResult() { ResourceTemplates = templates });
            };

            serverOptions.Handlers.ReadResourceHandler = async (request, cancellationToken) =>
            {
                var uri = request.Params?.Uri;
                var cancel = new McpCancelOperation() { Token = cancellationToken };
                string error = null;
                var result = await Task.Run(() => McpGateway.ReadResource(context, uri, out error, cancel), cancellationToken);
                if (result == null) throw new McpProtocolException(error, McpErrorCode.ResourceNotFound);
                if (McpGateway.IsError(result)) throw new McpProtocolException(result.Text, McpErrorCode.InvalidParams);
                var readResult = new ReadResourceResult();
                readResult.Contents.Add(new TextResourceContents() { Uri = uri, MimeType = "text/plain", Text = result.Text ?? "" });
                return readResult;
            };
        }

        /// <summary>Interval of the progress notifications sent while a tool runs</summary>
        const int McpProgressIntervalSeconds = 5;

        /// <summary>
        /// Runs a tool and, if the client sent a progress token, notifies the progress label of the tool at the start and then every <see cref="McpProgressIntervalSeconds"/> seconds until the tool ends
        /// (the notifications are part of the response of the request, allowed by the stateless mode).
        /// </summary>
        static async Task<AIToolHostResult> runWithProgress(McpServer server, ProgressToken? progressToken, Func<string> getLabel, Func<AIToolHostResult> execute, CancellationToken cancellationToken)
        {
            var task = Task.Run(execute, cancellationToken);
            if (progressToken == null || server == null) return await task;

            string label = null;
            try { label = getLabel(); } catch { }
            var start = DateTime.Now;
            while (true)
            {
                var elapsed = (int)(DateTime.Now - start).TotalSeconds;
                try
                {
                    await server.NotifyProgressAsync(progressToken.Value, new ProgressNotificationValue() { Progress = elapsed, Message = elapsed == 0 ? label : $"{label} ({elapsed} s)" }, cancellationToken: cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    //A client that cannot receive the notifications must not fail the tool call
                    break;
                }
                if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(McpProgressIntervalSeconds), cancellationToken)) == task) break;
            }
            return await task;
        }

        static ToolAnnotations getAnnotations(AIToolAccess access)
        {
            return access switch
            {
                AIToolAccess.ReadOnly => new ToolAnnotations() { ReadOnlyHint = true },
                AIToolAccess.Write => new ToolAnnotations() { ReadOnlyHint = false, DestructiveHint = false },
                AIToolAccess.Destructive => new ToolAnnotations() { ReadOnlyHint = false, DestructiveHint = true },
                _ => null
            };
        }

        static JsonElement getInputSchema(string parametersSchema)
        {
            if (!string.IsNullOrWhiteSpace(parametersSchema))
            {
                try
                {
                    using var doc = JsonDocument.Parse(parametersSchema);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object) return doc.RootElement.Clone();
                }
                catch { }
            }
            using var empty = JsonDocument.Parse("{\"type\":\"object\",\"properties\":{}}");
            return empty.RootElement.Clone();
        }

        /// <summary>
        /// Authenticates the requests of the MCP server before the MCP endpoint: 404 if the server is not enabled or the agent not available, 401 for a missing, invalid or expired token
        /// (with the URL of the protected resource metadata if the OAuth authorization server is enabled).
        /// </summary>
        static void UseMcpAuthentication(IApplicationBuilder app)
        {
            app.UseWhen(context => context.Request.Path.StartsWithSegments(McpPath), branch => branch.Use(async (httpContext, next) =>
            {
                var segments = httpContext.Request.Path.Value.Substring(McpPath.Length).Trim('/').Split('/');
                var slug = segments.Length == 1 ? segments[0] : "";
                var clientAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                var clientApplication = httpContext.Request.Headers.UserAgent.ToString();
                var context = McpGateway.Authenticate(httpContext.Request.Headers.Authorization.ToString(), slug, clientAddress, out var error, out var unauthorized, clientApplication);
                if (context?.Agent != null)
                {
                    httpContext.Items[McpContextKey] = context;
                    await next();
                    return;
                }

                if (Repository.Instance.AIConfiguration.McpServerEnabled)
                {
                    var user = context?.User ?? new SecurityUser(Repository.Instance.Security);
                    if (string.IsNullOrEmpty(user.Name)) user.Name = context?.Token?.UserName;
                    Audit.LogMcpAudit(AuditType.AIMcpRefused, user, null, httpContext.Request.Path.Value, error, null, clientAddress, clientApplication);
                }
                httpContext.Response.StatusCode = unauthorized ? StatusCodes.Status401Unauthorized : StatusCodes.Status404NotFound;
                if (unauthorized)
                {
                    var header = "Bearer realm=\"Seal Report MCP\"";
                    //OAuth: the client discovers the authorization server from the protected resource metadata of the agent (RFC 9728)
                    if (Repository.Instance.AIConfiguration.McpOAuthEnabled && !string.IsNullOrEmpty(slug)) header += $", resource_metadata=\"{McpOAuth.GetResourceMetadataUrl(GetMcpPublicBaseUrl(httpContext.Request), slug)}\", scope=\"{McpOAuth.McpScope}\"";
                    //No error code when no credentials are given (RFC 6750 section 3.1)
                    if (!string.IsNullOrEmpty(httpContext.Request.Headers.Authorization.ToString())) header += $", error=\"invalid_token\", error_description=\"{error?.Replace("\"", "'")}\"";
                    httpContext.Response.Headers.WWWAuthenticate = header;
                }
                await httpContext.Response.WriteAsync("Error: " + error);
            }));
        }

        static void MapMcp(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMcp(McpPath + "/{agent}");
        }
    }
}
