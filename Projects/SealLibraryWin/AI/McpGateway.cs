//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Seal.Model;

namespace Seal.AI
{
    /// <summary>
    /// Context of an authenticated request of the MCP server: the token, the user and the agent requested
    /// </summary>
    public class McpRequestContext
    {
        /// <summary>The access token of the request</summary>
        public McpAccessToken Token;

        /// <summary>The user authenticated from the token (loaded again for each request: his groups and rights are always the current ones)</summary>
        public SecurityUser User;

        /// <summary>The agent requested, assigned to the user and served on the MCP channel</summary>
        public AIAgentConfiguration Agent;
    }

    /// <summary>
    /// Server side of the MCP server independent of the transport: authentication of the requests with the personal access tokens, agents of a user, limit of the calls, execution of the tools with an <see cref="AIToolHost"/>.
    /// </summary>
    public static class McpGateway
    {
        /// <summary>Store of the access tokens</summary>
        public static IMcpTokenStore TokenStore
        {
            get { return _tokenStore ?? McpFileTokenStore.Instance; }
            set { _tokenStore = value; }
        }
        static IMcpTokenStore _tokenStore = null;

        /// <summary>
        /// Instructions added to the system prompt of an agent served by the MCP server
        /// </summary>
        public const string McpInstructions =
            "## Seal Report MCP server\r\n" +
            "You are connected to the Seal Report server with the tools of this agent, executed with the rights of the user. " +
            "A tool whose description starts with 'Requires the skill' belongs to a skill: call 'load_skill' with that skill once before its first call and follow the instructions returned. " +
            "A tool result starting with 'Error:' tells what to fix before calling the tool again.";

        /// <summary>
        /// Returns the identifier of an agent in the URL of the MCP server (/mcp/{slug}): its name in lower case, the characters other than letters and digits replaced by '-'.
        /// </summary>
        public static string GetAgentSlug(AIAgentConfiguration agent)
        {
            var normalized = (agent?.Name ?? "").Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            return Regex.Replace(sb.ToString().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        }

        /// <summary>
        /// Authenticates a request of the MCP server from its 'Authorization' header and returns its context, or null with the reason of the refusal in <paramref name="error"/>.
        /// The user of the token is authenticated by the Security Provider from his name (<see cref="SecurityUser.ForSchedule"/>), then the agent <paramref name="agentSlug"/> must be one of his MCP agents.
        /// </summary>
        /// <param name="authorization">Value of the 'Authorization' header ('Bearer seal_mcp_...')</param>
        /// <param name="agentSlug">Identifier of the agent in the URL (see <see cref="GetAgentSlug"/>)</param>
        /// <param name="hostName">Host name of the client</param>
        /// <param name="error">The reason of the refusal</param>
        /// <param name="unauthorized">True if the refusal is an authentication failure (401), false if the agent is not available (404)</param>
        public static McpRequestContext Authenticate(string authorization, string agentSlug, string hostName, out string error, out bool unauthorized)
        {
            error = null;
            unauthorized = true;
            var configuration = Repository.Instance.AIConfiguration;
            if (!configuration.McpServerEnabled)
            {
                unauthorized = false;
                error = "the MCP server is not enabled";
                return null;
            }

            var clearToken = authorization != null && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization.Substring(7).Trim() : null;
            if (string.IsNullOrEmpty(clearToken))
            {
                error = "no access token: set the header 'Authorization: Bearer <token>' with a personal access token created in your profile";
                return null;
            }
            var token = TokenStore.Find(clearToken);
            if (token == null)
            {
                error = "invalid or revoked access token";
                return null;
            }
            var now = DateTime.Now;
            error = token.CheckValidity(now, configuration.McpTokenIdleDays);
            if (error != null)
            {
                error += ": create a new token in your profile";
                return null;
            }

            var user = new SecurityUser(Repository.Instance.Security) { WebUserName = token.UserName, WebHostName = hostName, ForSchedule = true };
            user.Authenticate();
            if (!user.IsAuthenticated)
            {
                error = $"the user '{token.UserName}' of the access token is not authenticated by the Security Provider" + (string.IsNullOrEmpty(user.Error) ? "" : ": " + user.Error);
                return null;
            }
            TokenStore.Touch(token, now);
            try
            {
                if (System.IO.File.Exists(user.ProfilePath)) user.Profile = SecurityUserProfile.LoadFromFile(user.ProfilePath);
                user.Profile.Path = user.ProfilePath;
                //Folder tree of the user, used by the tools (report_list, get_current_folder, file tools): the web interface builds it when the user browses the folders
                user.SetFolders();
            }
            catch (Exception ex)
            {
                Helpers.Helper.WriteLogException("McpGateway.Authenticate", ex);
            }

            var agent = user.McpAgentConfigurations.FirstOrDefault(a => GetAgentSlug(a) == agentSlug);
            if (agent == null)
            {
                unauthorized = false;
                error = $"the agent '{agentSlug}' is not available on the MCP server for the user '{token.UserName}'";
                return new McpRequestContext() { Token = token, User = user };
            }
            unauthorized = false;
            return new McpRequestContext() { Token = token, User = user, Agent = agent };
        }

        static readonly ConcurrentDictionary<string, Queue<DateTime>> _calls = new ConcurrentDictionary<string, Queue<DateTime>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Records a tool call of a user and returns false if the user has made more than <see cref="AIServerConfiguration.McpMaxCallsPerMinute"/> calls in the last minute (the call is then not counted).
        /// </summary>
        public static bool CheckCallRate(string userName, DateTime now)
        {
            var max = Repository.Instance.AIConfiguration.McpMaxCallsPerMinute;
            if (max <= 0) return true;
            var calls = _calls.GetOrAdd(userName ?? "", _ => new Queue<DateTime>());
            lock (calls)
            {
                while (calls.Count > 0 && now - calls.Peek() >= TimeSpan.FromMinutes(1)) calls.Dequeue();
                if (calls.Count >= max) return false;
                calls.Enqueue(now);
                return true;
            }
        }

        /// <summary>
        /// Returns the tool host of a request: all the tools of the agent and of its skills are active (the MCP client receives the tool list once), the result can contain images.
        /// </summary>
        public static AIToolHost GetToolHost(McpRequestContext context)
        {
            var host = new AIToolHost(context.Agent)
            {
                SecurityContext = context.User,
                AcceptsImages = true,
                Culture = context.User.Profile?.Culture
            };
            host.ActivateAllTools();
            return host;
        }

        /// <summary>
        /// Returns the instructions of the MCP server for a request: the system prompt of the agent for the user, followed by <see cref="McpInstructions"/>.
        /// </summary>
        public static string GetInstructions(McpRequestContext context)
        {
            string prompt = null;
            try
            {
                prompt = AIAgent.CreateWithoutProvider(context.Agent, context.User, context.User.Profile?.Culture).SystemPrompt;
            }
            catch (Exception ex)
            {
                Helpers.Helper.WriteLogException("McpGateway.GetInstructions", ex);
            }
            return string.IsNullOrWhiteSpace(prompt) ? McpInstructions : prompt.TrimEnd() + "\r\n\r\n" + McpInstructions;
        }

        /// <summary>
        /// Executes a tool call of a request (limit of the calls, tool host, audit) and returns its result.
        /// </summary>
        public static AIToolHostResult ExecuteTool(McpRequestContext context, string toolName, string arguments, ICancelOperation cancelOperation = null)
        {
            var now = DateTime.Now;
            if (!CheckCallRate(context.User.WebUserName, now))
            {
                var max = Repository.Instance.AIConfiguration.McpMaxCallsPerMinute;
                var refused = new AIToolHostResult() { Text = $"Error: the limit of {max} tool calls per minute is reached for the user, this call was not executed. Wait one minute before calling the tools again." };
                Audit.LogMcpAudit(AuditType.AIMcpRefused, context.User, context.Agent?.Name, toolName, "too many calls");
                return refused;
            }

            var host = GetToolHost(context);
            var toolCall = new AIToolCall() { Id = Guid.NewGuid().ToString("N"), Name = toolName, Arguments = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments };
            var result = host.Execute(toolCall, cancelOperation);
            var isError = IsError(result);
            Audit.LogMcpAudit(isError ? AuditType.AIMcpCallError : AuditType.AIMcpCall, context.User, context.Agent.Name, toolName, isError ? Truncate(result.Text, 250) : null, (int)Math.Round((DateTime.Now - now).TotalSeconds));
            return result;
        }

        /// <summary>True if the result of a tool is an error (text starting with 'Error:' or script failure)</summary>
        public static bool IsError(AIToolHostResult result)
        {
            return result.Error != null || (result.Text ?? "").TrimStart().StartsWith("Error:", StringComparison.OrdinalIgnoreCase);
        }

        static string Truncate(string text, int length) => text == null || text.Length <= length ? text : text.Substring(0, length);
    }
}
