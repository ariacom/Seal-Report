//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Seal.AI;
using Seal.Model;

namespace SealWebServer.Controllers
{
    /// <summary>
    /// Model of the page McpAuthorize: login and consent of the user for an OAuth client of the MCP server
    /// </summary>
    public class McpAuthorizeModel
    {
        /// <summary>The repository of the session</summary>
        public Repository Repository;

        /// <summary>The authorization request, null if there is an error</summary>
        public McpAuthorizationRequest Request;

        /// <summary>Error shown instead of the consent</summary>
        public string Error;

        /// <summary>The agent of the request</summary>
        public AIAgentConfiguration Agent;

        /// <summary>Name of the agent translated</summary>
        public string AgentName;

        /// <summary>Tools of the agent and of its skills</summary>
        public List<AITool> Tools = new List<AITool>();

        /// <summary>The user logged in (null if not logged in)</summary>
        public SecurityUser User;

        /// <summary>True if a strong login is required and the user has not logged in with it</summary>
        public bool StrongLoginRequired;

        /// <summary>True if the OpenID Connect single sign-on is configured</summary>
        public bool SsoEnabled;
    }

    public partial class HomeController
    {
        const string McpAuthorizeView = "~/Views/Home/McpAuthorize.cshtml";

        void setMcpAuthorizeHeaders()
        {
            //No framing of the consent page (clickjacking), no cache
            Response.Headers["X-Frame-Options"] = "DENY";
            Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'";
            Response.Headers.CacheControl = "no-store";
        }

        McpAuthorizeModel getMcpAuthorizeModel(McpAuthorizationRequest request, string error)
        {
            var model = new McpAuthorizeModel() { Repository = Repository, Request = request, Error = error, SsoEnabled = AuthenticationConfig != null && AuthenticationConfig.Enabled };
            //A session without user name (e.g. anonymous access of the Security Provider) must log in: the tokens are bound to the user name
            if (WebUser != null && WebUser.IsAuthenticated && (!string.IsNullOrEmpty(WebUser.WebUserName) || WebUser.StrongAuthentication)) model.User = WebUser;
            if (request == null) return model;

            model.Agent = McpOAuth.FindMcpAgent(request.AgentSlug);
            if (model.Agent != null) model.AgentName = Repository.RepositoryTranslate("AIAgentName", "*", model.Agent.Name);
            if (model.User != null)
            {
                var agent = model.User.McpAgentConfigurations.FirstOrDefault(a => McpGateway.GetAgentSlug(a) == request.AgentSlug);
                if (agent == null) model.Error = string.Format(Translate("The agent '{0}' is not available on the MCP server for your user."), model.AgentName ?? request.AgentSlug);
                else if (string.IsNullOrEmpty(model.User.WebUserName)) model.Error = Translate("The MCP access tokens are not available for your authentication.");
                else
                {
                    model.Tools = McpGateway.GetToolHost(new McpRequestContext() { User = model.User, Agent = agent }).GetTools();
                    model.StrongLoginRequired = Repository.AIConfiguration.McpOAuthRequireStrongLogin && !model.User.StrongAuthentication;
                }
            }
            return model;
        }

        /// <summary>
        /// Authorization endpoint of the OAuth authorization server of the MCP server: validates the request of the client, then shows the login and the consent page.
        /// </summary>
        [HttpGet]
        public ActionResult McpAuthorize(string request_id)
        {
            try
            {
                if (!Startup.IsMcpOAuthEnabled()) return NotFound();
                if (Repository == null) CreateRepository();
                setMcpAuthorizeHeaders();
                var now = DateTime.Now;
                var baseUrl = Startup.GetMcpPublicBaseUrl(Request);

                if (string.IsNullOrEmpty(request_id))
                {
                    //New request of a client: validated, then the page is reloaded with its identifier (the login reloads the page)
                    McpAuthorizationRequest request;
                    var parameters = Request.Query.ToDictionary(i => i.Key, i => i.Value.ToString());
                    try
                    {
                        request = McpOAuth.ValidateAuthorizationRequest(parameters, baseUrl, now);
                    }
                    catch (McpOAuthException ex)
                    {
                        Audit.LogMcpAudit(AuditType.AIMcpRefused, WebUser ?? new SecurityUser(Repository.Security), null, "OAuth authorization", ex.Error + ": " + ex.Message, null, Request.HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
                        if (ex.CanRedirect && parameters.TryGetValue("redirect_uri", out var redirectUri))
                        {
                            parameters.TryGetValue("state", out var state);
                            return Redirect(McpOAuth.GetRedirectUrl(redirectUri, state, baseUrl, null, ex.Error, ex.Message));
                        }
                        return View(McpAuthorizeView, getMcpAuthorizeModel(null, ex.Message));
                    }
                    request.SessionId = SessionKey;
                    return Redirect(Request.PathBase + McpOAuth.OAuthPath + "/authorize?request_id=" + request.Id);
                }

                var pending = McpOAuth.GetAuthorizationRequest(request_id, now);
                if (pending == null || pending.SessionId != SessionKey) return View(McpAuthorizeView, getMcpAuthorizeModel(null, Translate("This authorization request has expired. Please connect again from your AI client.")));
                return View(McpAuthorizeView, getMcpAuthorizeModel(pending, null));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        /// <summary>
        /// Consent of the user: redirects to the client with an authorization code, or with the error 'access_denied' if the user refused.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult McpAuthorize(string request_id, string decision)
        {
            try
            {
                if (!Startup.IsMcpOAuthEnabled()) return NotFound();
                if (Repository == null) CreateRepository();
                setMcpAuthorizeHeaders();
                var now = DateTime.Now;
                var baseUrl = Startup.GetMcpPublicBaseUrl(Request);
                var request = McpOAuth.GetAuthorizationRequest(request_id, now);
                if (request == null || request.SessionId != SessionKey) return View(McpAuthorizeView, getMcpAuthorizeModel(null, Translate("This authorization request has expired. Please connect again from your AI client.")));

                var model = getMcpAuthorizeModel(request, null);
                var allowed = decision == "allow";
                if (allowed && (model.User == null || model.Error != null || model.StrongLoginRequired)) return View(McpAuthorizeView, model);

                var userName = model.User?.WebUserName;
                var code = McpOAuth.Authorize(request, userName, allowed, now);
                var auditUser = model.User ?? new SecurityUser(Repository.Security);
                var detail = $"OAuth authorization of '{request.Client.ClientName}' ({request.RedirectHost})";
                Audit.LogMcpAudit(AuditType.AIMcpAuthorization, auditUser, model.Agent?.Name, detail, allowed ? null : "refused by the user", null, Request.HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
                if (allowed) WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, $"MCP OAuth: '{request.Client.ClientName}' ({request.RedirectHost}) authorized by '{userName}' for the agent '{model.Agent?.Name}'");

                return Redirect(allowed
                    ? McpOAuth.GetRedirectUrl(request.RedirectUri, request.State, baseUrl, code)
                    : McpOAuth.GetRedirectUrl(request.RedirectUri, request.State, baseUrl, null, "access_denied", "the user refused the authorization"));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        /// <summary>
        /// Revokes an OAuth authorization of the user (profile)
        /// </summary>
        public ActionResult SWIRevokeMcpAuthorization(string id, string sessionId)
        {
            writeDebug("SWIRevokeMcpAuthorization");
            try
            {
                SetSessionId(sessionId);
                checkSWIAuthentication();
                var userName = WebUser.WebUserName;
                if (!McpOAuth.RevokeGrant(userName, id)) throw new Exception(Translate("The authorization has not been found."));

                WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, $"MCP OAuth authorization '{id}' revoked by '{userName}'");
                Audit.LogMcpAudit(AuditType.AIMcpAuthorization, WebUser, null, $"OAuth authorization '{id}' revoked", null);
                return Json(WebUser.McpAgentConfigurations.Count > 0 && !string.IsNullOrEmpty(userName) ? getMcpTokensResult(userName) : null);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }
    }
}
