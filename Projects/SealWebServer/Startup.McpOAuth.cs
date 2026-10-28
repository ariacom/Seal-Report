using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Seal.AI;
using Seal.Helpers;
using Seal.Model;

namespace SealWebServer
{
    /// <summary>
    /// OAuth 2.1 authorization server of the MCP server (see <see cref="McpOAuth"/>): metadata, token, dynamic registration and revocation endpoints.
    /// The authorization endpoint (login and consent of the user) is the action McpAuthorize of the Home controller.
    /// </summary>
    public partial class Startup
    {
        /// <summary>True if the MCP server and its OAuth authorization server are enabled (the repository is reloaded if it has been modified)</summary>
        public static bool IsMcpOAuthEnabled()
        {
            if (Repository.Instance.MustReload()) Repository.ReloadInstance();
            var configuration = Repository.Instance.AIConfiguration;
            return configuration.McpServerEnabled && configuration.McpOAuthEnabled;
        }

        /// <summary>Returns the public base URL of the server for a request (see <see cref="McpOAuth.GetPublicBaseUrl"/>)</summary>
        public static string GetMcpPublicBaseUrl(HttpRequest request)
        {
            return McpOAuth.GetPublicBaseUrl($"{request.Scheme}://{request.Host}{request.PathBase}");
        }

        static Task writeJson(HttpContext httpContext, object value, int statusCode = StatusCodes.Status200OK)
        {
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.Headers.CacheControl = "no-store";
            httpContext.Response.Headers.Pragma = "no-cache";
            return httpContext.Response.WriteAsync(JsonConvert.SerializeObject(value));
        }

        static Task writeOAuthError(HttpContext httpContext, McpOAuthException ex)
        {
            var status = ex.Error == "invalid_client" ? StatusCodes.Status401Unauthorized : StatusCodes.Status400BadRequest;
            return writeJson(httpContext, new Dictionary<string, string>() { { "error", ex.Error }, { "error_description", ex.Message } }, status);
        }

        static async Task<Dictionary<string, string>> readForm(HttpRequest request)
        {
            if (!request.HasFormContentType) throw new McpOAuthException("invalid_request", "the request must be 'application/x-www-form-urlencoded'");
            var form = await request.ReadFormAsync();
            return form.ToDictionary(i => i.Key, i => i.Value.ToString());
        }

        static void auditOAuth(HttpContext httpContext, string userName, string detail, string error)
        {
            var user = new SecurityUser(Repository.Instance.Security) { Name = userName ?? "" };
            Audit.LogMcpAudit(AuditType.AIMcpRefused, user, null, detail, error, null, httpContext.Connection.RemoteIpAddress?.ToString(), httpContext.Request.Headers.UserAgent.ToString());
        }

        static void MapMcpOAuth(IEndpointRouteBuilder endpoints)
        {
            //Protected resource metadata of the MCP server of an agent (RFC 9728), given in the header WWW-Authenticate of the 401 responses
            endpoints.MapGet("/.well-known/oauth-protected-resource" + McpPath + "/{agent}", (HttpContext httpContext, string agent) =>
            {
                var configuration = IsMcpOAuthEnabled() ? McpOAuth.FindMcpAgent(agent) : null;
                if (configuration == null)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    return Task.CompletedTask;
                }
                var baseUrl = GetMcpPublicBaseUrl(httpContext.Request);
                return writeJson(httpContext, McpOAuth.GetProtectedResourceMetadata(baseUrl, agent, configuration.Name));
            }).RequireCors("cors");

            //Metadata of the authorization server (RFC 8414), also served as OpenID Connect discovery for the issuers having a path (e.g. a virtual directory of IIS)
            RequestDelegate serverMetadata = httpContext =>
            {
                if (!IsMcpOAuthEnabled())
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    return Task.CompletedTask;
                }
                return writeJson(httpContext, McpOAuth.GetAuthorizationServerMetadata(GetMcpPublicBaseUrl(httpContext.Request)));
            };
            endpoints.MapGet("/.well-known/oauth-authorization-server", serverMetadata).RequireCors("cors");
            endpoints.MapGet("/.well-known/openid-configuration", serverMetadata).RequireCors("cors");

            //Token endpoint: authorization code and refresh token
            endpoints.MapPost(McpOAuth.OAuthPath + "/token", async httpContext =>
            {
                if (!IsMcpOAuthEnabled())
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                Dictionary<string, string> parameters = null;
                try
                {
                    parameters = await readForm(httpContext.Request);
                    var result = McpOAuth.ExecuteTokenRequest(parameters, DateTime.Now);
                    await writeJson(httpContext, result);
                }
                catch (McpOAuthException ex)
                {
                    auditOAuth(httpContext, null, "OAuth token " + (parameters != null && parameters.TryGetValue("grant_type", out var grantType) ? grantType : ""), ex.Error + ": " + ex.Message);
                    await writeOAuthError(httpContext, ex);
                }
                catch (Exception ex)
                {
                    Helper.WriteLogException("McpOAuth token", ex);
                    await writeOAuthError(httpContext, new McpOAuthException("server_error", "unexpected error"));
                }
            }).RequireCors("cors");

            //Dynamic client registration (RFC 7591)
            endpoints.MapPost(McpOAuth.OAuthPath + "/register", async httpContext =>
            {
                if (!IsMcpOAuthEnabled() || !Repository.Instance.AIConfiguration.McpOAuthDynamicRegistration)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                try
                {
                    string json;
                    using (var reader = new StreamReader(httpContext.Request.Body))
                    {
                        var buffer = new char[32 * 1024];
                        var length = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
                        json = new string(buffer, 0, length);
                    }
                    var result = McpOAuth.RegisterClient(json, DateTime.Now);
                    await writeJson(httpContext, result, StatusCodes.Status201Created);
                }
                catch (McpOAuthException ex)
                {
                    auditOAuth(httpContext, null, "OAuth client registration", ex.Error + ": " + ex.Message);
                    await writeOAuthError(httpContext, ex);
                }
                catch (Exception ex)
                {
                    Helper.WriteLogException("McpOAuth register", ex);
                    await writeOAuthError(httpContext, new McpOAuthException("server_error", "unexpected error"));
                }
            }).RequireCors("cors");

            //Token revocation (RFC 7009): always 200
            endpoints.MapPost(McpOAuth.OAuthPath + "/revoke", async httpContext =>
            {
                if (!IsMcpOAuthEnabled())
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                try
                {
                    var parameters = await readForm(httpContext.Request);
                    parameters.TryGetValue("client_id", out var clientId);
                    if (parameters.TryGetValue("token", out var token)) McpOAuth.RevokeToken(token, clientId);
                    httpContext.Response.StatusCode = StatusCodes.Status200OK;
                }
                catch (McpOAuthException ex)
                {
                    await writeOAuthError(httpContext, ex);
                }
                catch (Exception ex)
                {
                    Helper.WriteLogException("McpOAuth revoke", ex);
                    httpContext.Response.StatusCode = StatusCodes.Status200OK;
                }
            }).RequireCors("cors");

            //Authorization endpoint: login and consent of the user (Home controller)
            endpoints.MapControllerRoute(
                name: "mcpauthorize",
                pattern: McpOAuth.OAuthPath.TrimStart('/') + "/authorize",
                new { controller = "Home", action = "McpAuthorize" }
                );
        }
    }
}
