//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.ComponentModel;
using System.IO;
using System.Xml;
using Seal.Helpers;
using Seal.Model;

#if WINDOWS
using System.Drawing.Design;
using DynamicTypeDescriptor;
using Seal.Forms;
using System.ComponentModel.Design;
#endif

namespace Seal.AI
{
    /// <summary>
    /// Dedicated configuration for AI Providers, Tools and Agents.
    /// Serialized to Settings\AI\AIConfiguration.xml in the repository.
    /// </summary>
    public class AIServerConfiguration : RootComponent
    {
        /// <summary>
        /// Current file path
        /// </summary>
        [XmlIgnore]
        public string FilePath;

        /// <summary>
        /// Current repository
        /// </summary>
        [XmlIgnore]
        public Repository Repository;

        /// <summary>
        /// Last modification date time
        /// </summary>
        [XmlIgnore]
        public DateTime LastModification;

        /// <summary>
        /// Last modification date time of the dedicated AI Providers file (if ExternalAIProviders is set)
        /// </summary>
        [XmlIgnore]
        public DateTime LastProvidersModification;

#if WINDOWS
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                foreach (var property in Properties) property.SetIsBrowsable(false);

                GetProperty("ExternalAIProviders").SetIsBrowsable(true);
                GetProperty("AIProviders").SetIsBrowsable(true);
                GetProperty("AITools").SetIsBrowsable(true);
                GetProperty("AISkills").SetIsBrowsable(true);
                GetProperty("AIAgents").SetIsBrowsable(true);
                GetProperty("DefaultProviderGUID").SetIsBrowsable(true);
                GetProperty("DefaultAgentGUID").SetIsBrowsable(true);
                GetProperty("KnowledgeFolders").SetIsBrowsable(true);
                GetProperty("McpServerEnabled").SetIsBrowsable(true);
                GetProperty("McpTokenMaxDays").SetIsBrowsable(true);
                GetProperty("McpTokenIdleDays").SetIsBrowsable(true);
                GetProperty("McpMaxCallsPerMinute").SetIsBrowsable(true);
                GetProperty("McpOAuthEnabled").SetIsBrowsable(true);
                GetProperty("McpPublicUrl").SetIsBrowsable(true);
                GetProperty("McpOAuthRedirectUris").SetIsBrowsable(true);
                GetProperty("McpOAuthDynamicRegistration").SetIsBrowsable(true);
                GetProperty("McpOAuthAccessTokenMinutes").SetIsBrowsable(true);
                GetProperty("McpOAuthGrantMaxDays").SetIsBrowsable(true);
                GetProperty("McpOAuthRequireStrongLogin").SetIsBrowsable(true);
                GetProperty("McpLogToolCalls").SetIsBrowsable(true);
                GetProperty("McpLogResultMaxLength").SetIsBrowsable(true);

                GetProperty("McpTokenMaxDays").SetIsReadOnly(!McpServerEnabled);
                GetProperty("McpTokenIdleDays").SetIsReadOnly(!McpServerEnabled);
                GetProperty("McpMaxCallsPerMinute").SetIsReadOnly(!McpServerEnabled);
                GetProperty("McpOAuthEnabled").SetIsReadOnly(!McpServerEnabled);
                GetProperty("McpLogToolCalls").SetIsReadOnly(!McpServerEnabled);
                GetProperty("McpLogResultMaxLength").SetIsReadOnly(!McpServerEnabled || !McpLogToolCalls);
                foreach (var name in new[] { "McpPublicUrl", "McpOAuthRedirectUris", "McpOAuthDynamicRegistration", "McpOAuthAccessTokenMinutes", "McpOAuthGrantMaxDays", "McpOAuthRequireStrongLogin" }) GetProperty(name).SetIsReadOnly(!McpServerEnabled || !McpOAuthEnabled);

                TypeDescriptor.Refresh(this);
            }
        }

        #endregion
#endif

        /// <summary>
        /// Returns the path of the dedicated AI Providers file beside the AIConfiguration.xml file
        /// </summary>
        public static string GetAIProvidersFilePath(string path)
        {
            return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_AIProviders.xml");
        }

        bool _externalAIProviders = false;
        /// <summary>
        /// If true, the AI Providers are saved in a dedicated XML file located beside the AIConfiguration.xml file. This may be useful for deployment.
        /// </summary>
#if WINDOWS
        [DisplayName("Store AI Providers in a dedicated file"), Description("If true, the AI Providers are saved in a dedicated XML file located beside the AIConfiguration.xml file. This may be useful for deployment."), Category("AI Configuration"), Id(1, 1)]
        [DefaultValue(false)]
#endif
        public bool ExternalAIProviders
        {
            get { return _externalAIProviders; }
            set { _externalAIProviders = value; }
        }

        List<AIProviderConfiguration> _AIProviders = new List<AIProviderConfiguration>();
        /// <summary>
        /// AI Providers available to configure AIClient.
        /// </summary>
#if WINDOWS
        [DisplayName("AI providers"), Description("AI Providers available to configure AIClient."), Category("AI Configuration"), Id(2, 1)]
        [DefaultValue(false)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<AIProviderConfiguration> AIProviders
        {
            get { return _AIProviders; }
            set { _AIProviders = value; }
        }

        /// <summary>
        /// Do not serialize AIProviders inline when they are stored in a dedicated file
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeAIProviders() { return !ExternalAIProviders; }

        List<AIToolConfiguration> _AITools = new List<AIToolConfiguration>();
        /// <summary>
        /// AI Tools (functions) available to AI providers. Each tool has an optional Razor execution script
        /// that is run when the AI model decides to call the tool.
        /// </summary>
#if WINDOWS
        [DisplayName("AI tools"), Description("AI Tools (functions) available to AI providers. Each tool carries an optional Razor execution script run when the AI invokes the tool."), Category("AI Configuration"), Id(3, 1)]
        [DefaultValue(false)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<AIToolConfiguration> AITools
        {
            get { return _AITools; }
            set { _AITools = value; }
        }

        List<AISkillConfiguration> _AISkills = new List<AISkillConfiguration>();
        /// <summary>
        /// AI Skills available to AI agents. Each skill bundles an instructions file (loaded on demand)
        /// and an optional set of tools that become available once the skill is loaded.
        /// </summary>
#if WINDOWS
        [DisplayName("AI skills"), Description("AI Skills available to AI agents. Each skill bundles an instructions file (loaded on demand) and an optional set of tools that become available once the skill is loaded."), Category("AI Configuration"), Id(4, 1)]
        [DefaultValue(false)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<AISkillConfiguration> AISkills
        {
            get { return _AISkills; }
            set { _AISkills = value; }
        }

        string _defaultProviderGUID;
        /// <summary>
        /// GUID of the default AI provider. Used when no provider name is specified.
        /// </summary>
#if WINDOWS
        [DisplayName("Default Provider"), Description("The default AI provider used when no provider name is specified."), Category("AI Configuration"), Id(6, 1)]
        [TypeConverter(typeof(AIProviderConverter))]
#endif
        public string DefaultProviderGUID
        {
            get { return _defaultProviderGUID; }
            set { _defaultProviderGUID = value; }
        }

        string _defaultAgentGUID;
        /// <summary>
        /// GUID of the default AI agent. Used when no agent name is specified.
        /// </summary>
#if WINDOWS
        [DisplayName("Default Agent"), Description("The default AI agent used when no agent name is specified."), Category("AI Configuration"), Id(7, 1)]
        [TypeConverter(typeof(AIAgentConverter))]
#endif
        public string DefaultAgentGUID
        {
            get { return _defaultAgentGUID; }
            set { _defaultAgentGUID = value; }
        }

        /// <summary>
        /// Default value of <see cref="KnowledgeFolders"/>
        /// </summary>
        public const string DefaultKnowledgeFolders = "Views\r\nReports\\Samples\r\nSettings\\AI\\Skills";

        string _knowledgeFolders = DefaultKnowledgeFolders;
        /// <summary>
        /// Folders of the repository the AI tools seal_read and seal_search can read (one relative path per line, e.g. Views or Reports\Samples), to learn Seal from the view templates, the sample reports and the skills.
        /// The folders Settings (except Settings\AI\Skills), Sources, Security and Logs are never readable, even if they are listed.
        /// </summary>
#if WINDOWS
        [DisplayName("Knowledge folders"), Description("Folders of the repository the AI tools seal_read and seal_search can read (one relative path per line, e.g. Views or Reports\\Samples), to learn Seal from the view templates, the sample reports and the skills. The folders Settings (except Settings\\AI\\Skills), Sources, Security and Logs are never readable, even if they are listed."), Category("AI Configuration"), Id(8, 1)]
        [DefaultValue(DefaultKnowledgeFolders)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string KnowledgeFolders
        {
            get { return _knowledgeFolders; }
            set { _knowledgeFolders = value; }
        }

        /// <summary>
        /// Returns the list of the <see cref="KnowledgeFolders"/>, the default folders if none is set
        /// </summary>
        public List<string> GetKnowledgeFolders()
        {
            var result = new List<string>();
            foreach (var line in (string.IsNullOrWhiteSpace(KnowledgeFolders) ? DefaultKnowledgeFolders : KnowledgeFolders).Split(new char[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrWhiteSpace(line)) result.Add(line.Trim());
            }
            return result;
        }

        /// <summary>
        /// Web Report Server: if true, the AI agents having the channel MCP are served to the external AI clients (Claude, Copilot, Cursor...) with the Model Context Protocol at the URL /mcp/{agent},
        /// the users authenticate with a personal access token created in their profile.
        /// </summary>
#if WINDOWS
        [DisplayName("MCP server enabled"), Description("Danger Zone: if true, the Web Report Server serves the AI agents having the channel 'MCP' or 'Web and MCP' to the external AI clients (Claude, Copilot, Cursor...) with the Model Context Protocol at the URL /mcp/{agent}. The users authenticate with a personal access token created in their profile and use the tools and skills of the agents assigned to their security group. The data returned by the tools are then sent to the AI model chosen by the user, not to the provider of the agent: set the channel of an agent to 'Web' if its data must not leave its provider. The Security Provider must authenticate a user from his name without password (scheduled outputs: user.ForSchedule)."), Category("MCP Server"), Id(1, 2)]
        [DefaultValue(false)]
#endif
        public bool McpServerEnabled { get; set; } = false;

        /// <summary>
        /// Web Report Server: maximum validity of a personal access token of the MCP server, in days (0 for no limit)
        /// </summary>
#if WINDOWS
        [DisplayName("MCP token maximum validity"), Description("Maximum validity in days of a personal access token of the MCP server: the user chooses the validity when he creates a token, within this maximum. 0 for no limit (tokens without expiration are then allowed)."), Category("MCP Server"), Id(2, 2)]
        [DefaultValue(365)]
#endif
        public int McpTokenMaxDays { get; set; } = 365;

        /// <summary>
        /// Web Report Server: a personal access token of the MCP server not used for this number of days is refused (0 for no limit)
        /// </summary>
#if WINDOWS
        [DisplayName("MCP token maximum inactivity"), Description("A personal access token of the MCP server not used for this number of days is refused, even before its expiration. 0 for no limit."), Category("MCP Server"), Id(3, 2)]
        [DefaultValue(0)]
#endif
        public int McpTokenIdleDays { get; set; } = 0;

        /// <summary>
        /// Web Report Server: maximum number of MCP tool calls per minute and per user (0 for no limit)
        /// </summary>
#if WINDOWS
        [DisplayName("MCP maximum calls per minute"), Description("Maximum number of MCP tool calls per minute and per user, the next calls are refused until the minute is over. 0 for no limit."), Category("MCP Server"), Id(4, 2)]
        [DefaultValue(60)]
#endif
        public int McpMaxCallsPerMinute { get; set; } = 60;

        /// <summary>
        /// Web Report Server: if true, the MCP server is also an OAuth 2.1 authorization server: the AI clients (claude.ai, Claude Desktop, Claude Code...) connect to an agent with its URL only, the user logs in to the Web Report Server and gives his consent
        /// </summary>
#if WINDOWS
        [DisplayName("MCP OAuth enabled"), Description("Danger Zone: if true, the MCP server is also an OAuth 2.1 authorization server: an AI client (claude.ai, Claude Desktop, Claude Code...) connects to an agent with the URL of its MCP server only. The user logs in to the Web Report Server in his browser and authorizes the client for the agent, the client then receives an access token valid for this agent only (renewed with a refresh token). The authorizations are listed and revoked in the profile of the user. The MCP server must be enabled and served in HTTPS."), Category("MCP Server"), Id(5, 2)]
        [DefaultValue(false)]
#endif
        public bool McpOAuthEnabled { get; set; } = false;

        /// <summary>
        /// Web Report Server: public base URL of the Web Report Server (e.g. https://reports.mycompany.com/seal) used for the OAuth issuer and the URLs of the MCP servers. Empty to use the URL of the request.
        /// </summary>
#if WINDOWS
        [DisplayName("MCP public URL"), Description("Public base URL of the Web Report Server used by the OAuth authorization server for its issuer and for the URLs of the MCP servers (e.g. https://reports.mycompany.com/seal). It must be the URL the users enter in their AI client. Empty to use the URL of the request, set it if the server is behind a proxy."), Category("MCP Server"), Id(6, 2)]
        [DefaultValue("")]
#endif
        public string McpPublicUrl { get; set; } = "";

        /// <summary>
        /// Default value of <see cref="McpOAuthRedirectUris"/>: claude.ai, Claude Desktop and mobile, Claude Code (loopback)
        /// </summary>
        public const string DefaultMcpOAuthRedirectUris = "https://claude.ai/api/mcp/auth_callback\r\nhttps://claude.com/api/mcp/auth_callback\r\nhttp://localhost/callback\r\nhttp://127.0.0.1/callback";

        /// <summary>
        /// Web Report Server: redirect URIs of the OAuth clients allowed by the MCP server (one per line, the port of a loopback URI is ignored, '*' for any HTTPS or loopback URI)
        /// </summary>
#if WINDOWS
        [DisplayName("MCP OAuth redirect URIs"), Description("Redirect URIs of the AI clients allowed to connect with OAuth (one per line). By default: claude.ai, Claude Desktop and mobile (https://claude.ai/api/mcp/auth_callback, https://claude.com/api/mcp/auth_callback) and Claude Code (http://localhost/callback, http://127.0.0.1/callback). The port of a loopback URI (localhost, 127.0.0.1) is ignored. Add the redirect URI of another AI client to allow it, '*' allows any HTTPS or loopback URI."), Category("MCP Server"), Id(7, 2)]
        [DefaultValue(DefaultMcpOAuthRedirectUris)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string McpOAuthRedirectUris { get; set; } = DefaultMcpOAuthRedirectUris;

        /// <summary>
        /// Web Report Server: if true, the OAuth clients can register dynamically (RFC 7591), otherwise only the clients described by a Client ID Metadata Document are accepted
        /// </summary>
#if WINDOWS
        [DisplayName("MCP OAuth dynamic client registration"), Description("If true, the AI clients can register dynamically (RFC 7591). Otherwise only the clients identified by a Client ID Metadata Document (an HTTPS URL, e.g. claude.ai and Claude Code) are accepted. In both cases the redirect URIs must be allowed."), Category("MCP Server"), Id(8, 2)]
        [DefaultValue(true)]
#endif
        public bool McpOAuthDynamicRegistration { get; set; } = true;

        /// <summary>
        /// Web Report Server: validity in minutes of an OAuth access token of the MCP server
        /// </summary>
#if WINDOWS
        [DisplayName("MCP OAuth access token validity"), Description("Validity in minutes of an OAuth access token: the AI client renews it with its refresh token."), Category("MCP Server"), Id(9, 2)]
        [DefaultValue(60)]
#endif
        public int McpOAuthAccessTokenMinutes { get; set; } = 60;

        /// <summary>
        /// Web Report Server: maximum duration in days of an OAuth authorization, the user must then log in and authorize the client again (0 for no limit)
        /// </summary>
#if WINDOWS
        [DisplayName("MCP OAuth authorization maximum duration"), Description("Maximum duration in days of an OAuth authorization given by a user to an AI client: the user must then log in and authorize the client again, the policies of the login (password, Two-Factor Authentication, single sign-on) are applied again. 0 for no limit (the 'MCP token maximum validity' still applies). The 'MCP token maximum inactivity' also applies to the authorizations."), Category("MCP Server"), Id(10, 2)]
        [DefaultValue(30)]
#endif
        public int McpOAuthGrantMaxDays { get; set; } = 30;

        /// <summary>
        /// Web Report Server: if true, a user can authorize an OAuth client only if he has logged in with the Two-Factor Authentication or with the OpenID Connect single sign-on
        /// </summary>
#if WINDOWS
        [DisplayName("MCP OAuth requires a strong login"), Description("If true, a user can authorize an AI client only if he has logged in to the Web Report Server with the Two-Factor Authentication or with the OpenID Connect single sign-on, not with a user name and a password only."), Category("MCP Server"), Id(11, 2)]
        [DefaultValue(false)]
#endif
        public bool McpOAuthRequireStrongLogin { get; set; } = false;

        /// <summary>
        /// Web Report Server: if true, the arguments and the result of each MCP tool call are written in the daily event log file
        /// </summary>
#if WINDOWS
        [DisplayName("MCP log of the tool calls"), Description("Danger Zone: if true, the arguments and the result of each MCP tool call are written in the daily event log file of the 'Logs' Repository sub-folder, to analyze the conversations of the external AI clients. The log contains the searches and the SQL of the users and is kept for the 'Log days to keep' of the server. The question of the user, the final answer and the model stay in the AI client: they are never logged."), Category("MCP Server"), Id(12, 2)]
        [DefaultValue(false)]
#endif
        public bool McpLogToolCalls { get; set; } = false;

        /// <summary>
        /// Web Report Server: maximum number of characters of a tool result in the log of the MCP tool calls (0 for the complete result)
        /// </summary>
#if WINDOWS
        [DisplayName("MCP log maximum result length"), Description("Maximum number of characters of a tool result written in the log of the MCP tool calls, the arguments are always complete. 0 for the complete result."), Category("MCP Server"), Id(13, 2)]
        [DefaultValue(4000)]
#endif
        public int McpLogResultMaxLength { get; set; } = 4000;

        List<AIAgentConfiguration> _AIAgents = new List<AIAgentConfiguration>();
        /// <summary>
        /// AI Agents combining a provider, an optional set of tools, and a default system prompt.
        /// </summary>
#if WINDOWS
        [DisplayName("AI agents"), Description("AI Agents: each agent ties together a provider, an optional subset of tools, and a default system prompt."), Category("AI Configuration"), Id(5, 1)]
        [DefaultValue(false)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<AIAgentConfiguration> AIAgents
        {
            get { return _AIAgents; }
            set { _AIAgents = value; }
        }

        /// <summary>
        /// Load AI configuration from a file
        /// </summary>
        static public AIServerConfiguration LoadFromFile(string path, bool ignoreException)
        {
            AIServerConfiguration result = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AIServerConfiguration));
                using (XmlReader xr = XmlReader.Create(path))
                {
                    result = (AIServerConfiguration)serializer.Deserialize(xr);
                }
                result.FilePath = path;
                result.LastModification = File.GetLastWriteTime(path);

                var providersPath = GetAIProvidersFilePath(path);
                if (result.ExternalAIProviders)
                {
                    if (File.Exists(providersPath))
                    {
                        var providersSerializer = new XmlSerializer(typeof(List<AIProviderConfiguration>));
                        using (XmlReader xr = XmlReader.Create(providersPath))
                        {
                            result.AIProviders = (List<AIProviderConfiguration>)providersSerializer.Deserialize(xr);
                        }
                        result.LastProvidersModification = File.GetLastWriteTime(providersPath);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ignoreException) throw new Exception(string.Format("Unable to read the AI configuration file '{0}'.\r\n{1}", path, ex.Message));
            }
            return result;
        }

        /// <summary>
        /// True if the configuration file (or the dedicated AI Providers file) has been modified or created since the load
        /// </summary>
        public bool MustReload(string path)
        {
            //Default configuration built in memory: reload as soon as the file exists
            if (FilePath == null) return File.Exists(path);
            //File deleted: keep the current configuration
            if (!File.Exists(FilePath)) return false;
            if (File.GetLastWriteTime(FilePath) != LastModification) return true;
            if (ExternalAIProviders)
            {
                var providersPath = GetAIProvidersFilePath(FilePath);
                if (File.Exists(providersPath) && File.GetLastWriteTime(providersPath) != LastProvidersModification) return true;
            }
            return false;
        }

        /// <summary>
        /// Save to current file
        /// </summary>
        public void SaveToFile()
        {
            SaveToFile(FilePath);
        }

        /// <summary>
        /// Save to a given file path
        /// </summary>
        public void SaveToFile(string path)
        {
            if (LastModification != DateTime.MinValue && File.Exists(path))
            {
                DateTime lastDateTime = File.GetLastWriteTime(path);
                if (LastModification != lastDateTime)
                {
                    throw new Exception("Unable to save the AI Configuration file. The file has been modified by another user.");
                }
            }

            var xmlOverrides = new XmlAttributeOverrides();
            XmlAttributes attrs = new XmlAttributes();
            attrs.XmlIgnore = true;
            xmlOverrides.Add(typeof(RootComponent), "Name", attrs);
            xmlOverrides.Add(typeof(RootComponent), "GUID", attrs);

            // Ensure directory exists
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            Helper.Serialize(path, this, new XmlSerializer(typeof(AIServerConfiguration), xmlOverrides));
            FilePath = path;
            LastModification = File.GetLastWriteTime(path);

            var providersPath = GetAIProvidersFilePath(path);
            if (ExternalAIProviders)
            {
                Helper.Serialize(providersPath, AIProviders);
                LastProvidersModification = File.GetLastWriteTime(providersPath);
            }
            else if (File.Exists(providersPath)) File.Delete(providersPath);
        }
    }
}
