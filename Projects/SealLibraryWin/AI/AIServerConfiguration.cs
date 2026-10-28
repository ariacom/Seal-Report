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

                GetProperty("McpTokenMaxDays").SetIsReadOnly(!McpServerEnabled);
                GetProperty("McpTokenIdleDays").SetIsReadOnly(!McpServerEnabled);
                GetProperty("McpMaxCallsPerMinute").SetIsReadOnly(!McpServerEnabled);

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
