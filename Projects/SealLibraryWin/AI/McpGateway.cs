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
        /// <summary>The access token of the request (for an OAuth access token: a description of its grant)</summary>
        public McpAccessToken Token;

        /// <summary>The OAuth authorization of the request, null for a personal access token</summary>
        public McpOAuthGrant Grant;

        /// <summary>The user authenticated from the token (loaded again for each request: his groups and rights are always the current ones)</summary>
        public SecurityUser User;

        /// <summary>The agent requested, assigned to the user and served on the MCP channel</summary>
        public AIAgentConfiguration Agent;

        /// <summary>IP address of the MCP client</summary>
        public string ClientAddress;

        /// <summary>Application of the MCP client (HTTP header 'User-Agent')</summary>
        public string ClientApplication;
    }

    /// <summary>
    /// A prompt of the MCP server: a sample prompt of the agent
    /// </summary>
    public class McpPrompt
    {
        /// <summary>Name of the prompt (sample-1, sample-2...)</summary>
        public string Name;

        /// <summary>Text of the prompt translated for the user</summary>
        public string Text;
    }

    /// <summary>
    /// A resource of the MCP server: a report or a data source described by a tool of the agent
    /// </summary>
    public class McpResource
    {
        /// <summary>URI of the resource (seal://report/... or seal://source/...)</summary>
        public string Uri;

        /// <summary>Name of the resource</summary>
        public string Name;

        /// <summary>Description of the resource</summary>
        public string Description;
    }

    /// <summary>
    /// A resource template of the MCP server
    /// </summary>
    public class McpResourceTemplate
    {
        /// <summary>URI template (RFC 6570)</summary>
        public string UriTemplate;

        /// <summary>Name of the template</summary>
        public string Name;

        /// <summary>Description of the template</summary>
        public string Description;
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
        /// Authenticates a request of the MCP server from its 'Authorization' header (personal access token or OAuth access token) and returns its context, or null with the reason of the refusal in <paramref name="error"/>.
        /// The user of the token is authenticated by the Security Provider from his name (<see cref="SecurityUser.ForSchedule"/>), then the agent <paramref name="agentSlug"/> must be one of his MCP agents.
        /// </summary>
        /// <param name="authorization">Value of the 'Authorization' header ('Bearer seal_mcp_...')</param>
        /// <param name="agentSlug">Identifier of the agent in the URL (see <see cref="GetAgentSlug"/>)</param>
        /// <param name="hostName">Host name of the client</param>
        /// <param name="error">The reason of the refusal</param>
        /// <param name="unauthorized">True if the refusal is an authentication failure (401), false if the agent is not available (404)</param>
        /// <param name="clientApplication">Application of the client (HTTP header 'User-Agent'), audited with the requests</param>
        public static McpRequestContext Authenticate(string authorization, string agentSlug, string hostName, out string error, out bool unauthorized, string clientApplication = null)
        {
            error = null;
            unauthorized = true;
            //Configuration or security modified (e.g. by the Server Manager): reload the repository as the web menu does
            if (Repository.Instance.MustReload()) Repository.ReloadInstance();
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
            var now = DateTime.Now;
            McpAccessToken token;
            McpOAuthGrant grant = null;
            if (McpOAuth.IsAccessToken(clearToken))
            {
                //OAuth access token: valid only for the agent of its authorization
                if (!configuration.McpOAuthEnabled)
                {
                    error = "the OAuth authorization is not enabled on the MCP server";
                    return null;
                }
                grant = McpOAuth.AuthenticateAccessToken(clearToken, agentSlug, now, out error);
                if (grant == null) return null;
                token = new McpAccessToken() { Id = grant.Id, Name = grant.ClientName, UserName = grant.UserName, Creation = grant.Creation, Expiration = grant.AccessExpiration, LastUse = grant.LastUse };
            }
            else
            {
                token = TokenStore.Find(clearToken);
                if (token == null)
                {
                    error = "invalid or revoked access token";
                    return null;
                }
                error = token.CheckValidity(now, configuration.McpTokenIdleDays);
                if (error != null)
                {
                    error += ": create a new token in your profile";
                    return null;
                }
            }

            var user = new SecurityUser(Repository.Instance.Security) { WebUserName = token.UserName, WebHostName = hostName, ForSchedule = true };
            user.Authenticate();
            if (!user.IsAuthenticated)
            {
                error = $"the user '{token.UserName}' of the access token is not authenticated by the Security Provider" + (string.IsNullOrEmpty(user.Error) ? "" : ": " + user.Error);
                return null;
            }
            if (grant == null) TokenStore.Touch(token, now);
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
                return new McpRequestContext() { Token = token, Grant = grant, User = user, ClientAddress = hostName, ClientApplication = clientApplication };
            }
            unauthorized = false;
            return new McpRequestContext() { Token = token, Grant = grant, User = user, Agent = agent, ClientAddress = hostName, ClientApplication = clientApplication };
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
                Audit.LogMcpAudit(AuditType.AIMcpRefused, context.User, context.Agent?.Name, toolName, "too many calls", null, context.ClientAddress, context.ClientApplication);
                return refused;
            }

            //Limits of the user set by the Security Provider
            var refusal = context.User.GetMcpCallRefusal(toolName);
            if (refusal != null)
            {
                Audit.LogMcpAudit(AuditType.AIMcpRefused, context.User, context.Agent?.Name, toolName, Truncate(refusal, 250), null, context.ClientAddress, context.ClientApplication);
                return new AIToolHostResult() { Text = "Error: " + refusal };
            }

            var host = GetToolHost(context);
            var toolCall = new AIToolCall() { Id = Guid.NewGuid().ToString("N"), Name = toolName, Arguments = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments };
            var result = host.Execute(toolCall, cancelOperation);
            var isError = IsError(result);
            Audit.LogMcpAudit(isError ? AuditType.AIMcpCallError : AuditType.AIMcpCall, context.User, context.Agent.Name, toolName, isError ? Truncate(result.Text, 250) : null, (int)Math.Round((DateTime.Now - now).TotalSeconds), context.ClientAddress, context.ClientApplication);
            context.User.NotifyMcpCall(toolName, isError);
            return result;
        }

        /// <summary>
        /// Returns the progress label of a tool call, translated for the user, sent to the MCP client while the tool runs.
        /// </summary>
        public static string GetProgressLabel(McpRequestContext context, string toolName, string arguments)
        {
            var toolCall = new AIToolCall() { Name = toolName, Arguments = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments };
            return GetToolHost(context).GetProgressLabel(toolCall);
        }

        /// <summary>
        /// Returns the prompts of a request: the sample prompts of the agent for the user, translated in his culture.
        /// </summary>
        public static List<McpPrompt> GetPrompts(McpRequestContext context)
        {
            var result = new List<McpPrompt>();
            try
            {
                var culture = context.User.Profile?.Culture;
                var prompts = AIAgent.CreateWithoutProvider(context.Agent, context.User, culture).GetSamplePrompts();
                foreach (var prompt in prompts.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    var text = string.IsNullOrEmpty(culture)
                        ? Repository.Instance.RepositoryTranslate("AISamplePrompt", "*", prompt)
                        : Repository.Instance.RepositoryTranslate(culture, "AISamplePrompt", "*", prompt);
                    result.Add(new McpPrompt() { Name = "sample-" + (result.Count + 1), Text = text });
                }
            }
            catch (Exception ex)
            {
                Helpers.Helper.WriteLogException("McpGateway.GetPrompts", ex);
            }
            return result;
        }

        /// <summary>URI prefix of the report resources</summary>
        public const string ReportResourcePrefix = "seal://report/";

        /// <summary>URI prefix of the data source resources</summary>
        public const string SourceResourcePrefix = "seal://source/";

        /// <summary>Tool describing a report resource</summary>
        public const string ReportResourceTool = "report_get_detail";

        /// <summary>Tool describing a data source resource</summary>
        public const string SourceResourceTool = "datasource_get_detail";

        /// <summary>Maximum number of reports listed as resources</summary>
        public const int MaxReportResources = 500;

        static HashSet<string> getToolNames(McpRequestContext context)
        {
            return new HashSet<string>(GetToolHost(context).GetTools().Select(t => t.Name));
        }

        /// <summary>
        /// Returns the URI of a report resource from its path as returned by the tools (e.g. Reports\Sales\monthly.srex).
        /// </summary>
        public static string GetReportResourceUri(string path)
        {
            return ReportResourcePrefix + string.Join("/", path.Split('\\', '/').Where(s => s.Length > 0).Select(Uri.EscapeDataString));
        }

        /// <summary>
        /// Returns the resource templates of a request: reports and data sources, if the agent has the tools describing them.
        /// </summary>
        public static List<McpResourceTemplate> GetResourceTemplates(McpRequestContext context)
        {
            var result = new List<McpResourceTemplate>();
            var tools = getToolNames(context);
            if (tools.Contains(ReportResourceTool)) result.Add(new McpResourceTemplate() { UriTemplate = ReportResourcePrefix + "{+path}", Name = "Report", Description = "Detail of a report (models, columns, restrictions, views, outputs and schedules) from its path, e.g. Reports/Sales/monthly.srex" });
            if (tools.Contains(SourceResourceTool)) result.Add(new McpResourceTemplate() { UriTemplate = SourceResourcePrefix + "{name}", Name = "Data source", Description = "Detail of a data source (connections, tables, columns, joins and enumerated lists) from its name" });
            return result;
        }

        /// <summary>
        /// Returns the resources of a request: the reports of the folders of the user (at most <see cref="MaxReportResources"/>) and the data sources he can access, if the agent has the tools describing them.
        /// </summary>
        public static List<McpResource> GetResources(McpRequestContext context)
        {
            var result = new List<McpResource>();
            var tools = getToolNames(context);
            var user = context.User;
            try
            {
                if (tools.Contains(ReportResourceTool))
                {
                    var repository = Repository.Instance;
                    string personalFolder = user.PersonalFolderRight != PersonalFolderRight.None ? repository.GetPersonalFolder(user) : null;
                    foreach (var folder in user.AllFolders)
                    {
                        if (result.Count >= MaxReportResources) break;
                        //Same rules as the tool report_list: no hidden folder, no files-only folder, no personal recycle bin
                        if (folder.right == 0 || folder.files) continue;
                        if (folder.IsPersonal && folder.FinalPath.TrimStart(System.IO.Path.DirectorySeparatorChar).Split(System.IO.Path.DirectorySeparatorChar).FirstOrDefault() == "BIN") continue;
                        string physicalPath, displayPath;
                        if (folder.IsPersonal)
                        {
                            if (personalFolder == null) continue;
                            physicalPath = personalFolder + folder.FinalPath;
                            displayPath = "Personal" + folder.FinalPath;
                        }
                        else
                        {
                            physicalPath = repository.ReportsFolder + folder.path;
                            displayPath = "Reports" + folder.path;
                        }
                        if (!System.IO.Directory.Exists(physicalPath)) continue;
                        foreach (var file in System.IO.Directory.GetFiles(physicalPath, "*." + Repository.SealReportFileExtension).OrderBy(f => f))
                        {
                            if (result.Count >= MaxReportResources) break;
                            var path = displayPath.TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar + System.IO.Path.GetFileName(file);
                            result.Add(new McpResource() { Uri = GetReportResourceUri(path), Name = System.IO.Path.GetFileNameWithoutExtension(file), Description = "Report " + path });
                        }
                    }
                }
                if (tools.Contains(SourceResourceTool))
                {
                    foreach (var source in AIToolConfiguration.GetAvailableSources(user).OrderBy(s => s.Name))
                    {
                        result.Add(new McpResource() { Uri = SourceResourcePrefix + Uri.EscapeDataString(source.Name), Name = source.Name, Description = "Data source " + source.Name });
                    }
                }
            }
            catch (Exception ex)
            {
                Helpers.Helper.WriteLogException("McpGateway.GetResources", ex);
            }
            return result;
        }

        /// <summary>
        /// Reads a resource: executes the tool of the agent describing it (same security, limit of the calls and audit as a tool call).
        /// Returns null with the reason in <paramref name="error"/> if the URI is not a resource of the agent.
        /// </summary>
        public static AIToolHostResult ReadResource(McpRequestContext context, string uri, out string error, ICancelOperation cancelOperation = null)
        {
            error = null;
            string toolName = null, arguments = null;
            var tools = getToolNames(context);
            if (uri != null && uri.StartsWith(ReportResourcePrefix) && tools.Contains(ReportResourceTool))
            {
                var segments = uri.Substring(ReportResourcePrefix.Length).Split('/').Where(s => s.Length > 0).Select(Uri.UnescapeDataString);
                toolName = ReportResourceTool;
                arguments = Newtonsoft.Json.JsonConvert.SerializeObject(new { path = string.Join(System.IO.Path.DirectorySeparatorChar.ToString(), segments) });
            }
            else if (uri != null && uri.StartsWith(SourceResourcePrefix) && tools.Contains(SourceResourceTool))
            {
                toolName = SourceResourceTool;
                arguments = Newtonsoft.Json.JsonConvert.SerializeObject(new { name = Uri.UnescapeDataString(uri.Substring(SourceResourcePrefix.Length).TrimEnd('/')) });
            }
            if (toolName == null)
            {
                error = $"unknown resource '{uri}'";
                return null;
            }
            return ExecuteTool(context, toolName, arguments, cancelOperation);
        }

        /// <summary>True if the result of a tool is an error (text starting with 'Error:' or script failure)</summary>
        public static bool IsError(AIToolHostResult result)
        {
            return result.Error != null || (result.Text ?? "").TrimStart().StartsWith("Error:", StringComparison.OrdinalIgnoreCase);
        }

        static string Truncate(string text, int length) => text == null || text.Length <= length ? text : text.Substring(0, length);
    }
}
