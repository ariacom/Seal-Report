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

            serverOptions.Handlers.ListToolsHandler = (request, cancellationToken) =>
            {
                var tools = McpGateway.GetToolHost(context).GetTools().Select(t => new Tool()
                {
                    Name = t.Name,
                    Description = t.Description,
                    InputSchema = getInputSchema(t.ParametersSchema)
                }).ToList();
                return ValueTask.FromResult(new ListToolsResult() { Tools = tools });
            };

            serverOptions.Handlers.CallToolHandler = async (request, cancellationToken) =>
            {
                var name = request.Params?.Name;
                var arguments = request.Params?.Arguments != null ? JsonSerializer.Serialize(request.Params.Arguments) : "{}";
                var cancel = new McpCancelOperation() { Token = cancellationToken };
                var result = await Task.Run(() => McpGateway.ExecuteTool(context, name, arguments, cancel), cancellationToken);

                var callResult = new CallToolResult() { IsError = McpGateway.IsError(result) };
                callResult.Content.Add(new TextContentBlock() { Text = string.IsNullOrEmpty(result.Text) ? " " : result.Text });
                foreach (var image in result.Images) callResult.Content.Add(ImageContentBlock.FromBytes(image.Data, image.MediaType));
                return callResult;
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
        /// Authenticates the requests of the MCP server before the MCP endpoint: 404 if the server is not enabled or the agent not available, 401 for a missing, invalid or expired token.
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
                if (unauthorized) httpContext.Response.Headers.WWWAuthenticate = $"Bearer realm=\"Seal Report MCP\", error=\"invalid_token\", error_description=\"{error?.Replace("\"", "'")}\"";
                await httpContext.Response.WriteAsync("Error: " + error);
            }));
        }

        static void MapMcp(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMcp(McpPath + "/{agent}");
        }
    }
}
