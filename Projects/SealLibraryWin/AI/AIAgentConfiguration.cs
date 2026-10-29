using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Seal.Model;

#if WINDOWS
using System.Drawing.Design;
using Seal.Forms;
using DynamicTypeDescriptor;
#endif

namespace Seal.AI
{
    /// <summary>
    /// Configuration for an AI agent: ties together one provider, an optional subset of tools,
    /// and a default system prompt. Stored in <see cref="Seal.AI.AIServerConfiguration.AIAgents"/>.
    /// </summary>
    public class AIAgentConfiguration : RootEditor
    {
        /// <summary>
        /// Display name shown in the property-grid dropdown for an empty default agent,
        /// meaning the first enabled agent is used.
        /// </summary>
        public const string FirstEnabledName = "<First Enabled>";

#if WINDOWS
        #region Editor
        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                foreach (var property in Properties) property.SetIsBrowsable(false);
                GetProperty("Name").SetIsBrowsable(true);
                GetProperty("Description").SetIsBrowsable(true);
                GetProperty("IsEnabled").SetIsBrowsable(true);
                GetProperty("Channel").SetIsBrowsable(true);
                GetProperty("UsageDisplay").SetIsBrowsable(true);
                GetProperty("ProviderGUID").SetIsBrowsable(true);
                GetProperty("ToolGUIDs").SetIsBrowsable(true);
                GetProperty("SkillGUIDs").SetIsBrowsable(true);
                GetProperty("MaxReportCodeLevel").SetIsBrowsable(true);
                GetProperty("MaxToolCalls").SetIsBrowsable(true);
                GetProperty("DefaultSystemPrompt").SetIsBrowsable(true);
                GetProperty("SystemPromptFile").SetIsBrowsable(true);
                GetProperty("SystemPromptScriptFile").SetIsBrowsable(true);
                GetProperty("SamplePromptsFile").SetIsBrowsable(true);
                GetProperty("SamplePromptsScriptFile").SetIsBrowsable(true);
                GetProperty("Temperature").SetIsBrowsable(true);
                GetProperty("MaxTokens").SetIsBrowsable(true);
                GetProperty("TopP").SetIsBrowsable(true);
                TypeDescriptor.Refresh(this);
            }
        }
        #endregion
#endif

        /// <summary>
        /// Unique identifier for this agent configuration.
        /// </summary>
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        string _name = "Agent";
        /// <summary>
        /// Display name of this agent configuration.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\tAgent name"), Description("Display name of this AI agent."), Id(1, 1)]
#endif
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        string _description = "";
        /// <summary>
        /// Short description of what this agent can do.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\tDescription"), Description("Short description of what this agent can do."), Id(2, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        /// <summary>
        /// When <c>false</c>, this agent is excluded from all operations without being deleted.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\tIs Enabled"), Description("When false, this agent is excluded from all operations."), Id(4, 1)]
        [DefaultValue(true)]
#endif
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// The channels the agent is served on: the web chat (with the provider of the agent) and/or the MCP server (with the AI model of the external client, if <see cref="AIServerConfiguration.McpServerEnabled"/> is true).
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\tChannel"), Description("The channels the agent is served on: 'Web' (chat of the Web Report Server, with the provider of the agent), 'MCP' (MCP server, with the AI model of the external client: the provider is not used) or 'Web and MCP'. The MCP server is active only if 'MCP server enabled' is set in the AI configuration. Set 'Web' if the data of the agent must not leave its provider."), Id(4, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(AIAgentChannel.WebAndMcp)]
#endif
        public AIAgentChannel Channel { get; set; } = AIAgentChannel.WebAndMcp;

        /// <summary>
        /// True if the agent is enabled and served on the web chat
        /// </summary>
        [XmlIgnore]
        public bool IsWebAgent => IsEnabled && Channel != AIAgentChannel.Mcp;

        /// <summary>
        /// True if the agent is enabled and served on the MCP server (the server itself must be enabled, see <see cref="AIServerConfiguration.McpServerEnabled"/>)
        /// </summary>
        [XmlIgnore]
        public bool IsMcpAgent => IsEnabled && Channel != AIAgentChannel.Web;

        /// <summary>
        /// Usage shown under each answer of the web chat: tokens and/or cost of the exchange, or nothing. The cost is the one of the AI provider, unless the Security Provider sets <see cref="SecurityUser.FormatAICost"/> (e.g. the amount billed to the user).
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\tUsage display"), Description("The usage shown under each answer of the web chat: the tokens and/or the cost of the exchange, or nothing. The cost is the one of the AI provider (costs of the provider configuration), unless the Security Provider script sets 'user.FormatAICost' to show another text (e.g. the amount billed to the user)."), Id(4, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(AIUsageDisplay.TokensAndCost)]
#endif
        public AIUsageDisplay UsageDisplay { get; set; } = AIUsageDisplay.TokensAndCost;

        /// <summary>
        /// GUID of the <see cref="AIProviderConfiguration"/> to use.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\tProvider"), Description("The AI provider to use for this agent (web chat). An agent served only on the MCP server does not need a provider: the AI model is the one of the external client."), Id(5, 1)]
        [TypeConverter(typeof(AIProviderConverter))]
#endif
        public string ProviderGUID { get; set; }

        /// <summary>
        /// GUIDs of the <see cref="AIToolConfiguration"/> instances available to this agent.
        /// An empty list means no tools are available.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\tTools"), Description("The tools available to this agent. An empty list disables all tools."), Id(6, 1)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
#endif
        [XmlArray("ToolGUIDs")]
        [XmlArrayItem("GUID")]
        public List<string> ToolGUIDs { get; set; } = new List<string>();

        /// <summary>
        /// GUIDs of the <see cref="AISkillConfiguration"/> instances available to this agent.
        /// An empty list means no skills are available.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\tSkills"), Description("The skills available to this agent. An empty list disables all skills."), Id(7, 1)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
#endif
        [XmlArray("SkillGUIDs")]
        [XmlArrayItem("GUID")]
        public List<string> SkillGUIDs { get; set; } = new List<string>();

        /// <summary>
        /// The highest code level the tools of this agent can write in a report. For a tool call, the level applied is the lowest between the level of the user (security groups) and this value.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\tMaximum report code level"), Description("The highest code level the tools of this agent can write in a report: 'Metadata only' (no SQL, no script), 'Metadata and SQL' or 'Metadata, SQL and scripts'. For a tool call, the level applied is the lowest between the level of the user (Report code level of his security groups) and this value."), Id(13, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(ReportCodeLevel.Scripts)]
#endif
        public ReportCodeLevel MaxReportCodeLevel { get; set; } = ReportCodeLevel.Scripts;

        /// <summary>
        /// Default value of <see cref="MaxToolCalls"/>
        /// </summary>
        public const int DefaultMaxToolCalls = 40;

        /// <summary>
        /// Maximum number of tool calls the agent can make to answer one user message. When it is reached, the agent answers with what it has done so far.
        /// </summary>
#if WINDOWS
        [Category("Parameters"), DisplayName("Max tool calls per message"), Description("Maximum number of tool calls the agent can make to answer one user message (0 for the default value, 40). When it is reached, the agent answers with what it has done so far."), Id(4, 2)]
        [DefaultValue(DefaultMaxToolCalls)]
#endif
        public int MaxToolCalls { get; set; } = DefaultMaxToolCalls;

        /// <summary>
        /// Inline system prompt injected at the start of every conversation.
        /// Ignored when <see cref="SystemPromptFile"/> is set.
        /// Leave empty to use no system prompt.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tDefault System Prompt"), Description("Inline system prompt. Ignored when System Prompt File is set. Leave empty for none."), Id(8, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string DefaultSystemPrompt { get; set; }

        /// <summary>
        /// File name of a text file containing the system prompt, relative to <c>Settings\AI\Prompts</c>.
        /// When set, takes precedence over <see cref="DefaultSystemPrompt"/>.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tSystem Prompt File"), Description("File name of the system prompt (e.g. my-prompt.md), located in Settings\\AI\\Prompts. When set, overrides Default System Prompt. Click the editor button to edit the file content."), Id(9, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string SystemPromptFile { get; set; }

        /// <summary>
        /// File name of a Razor script, relative to <c>Settings\AI\Scripts</c>, executed before each chat turn with the
        /// <see cref="AIAgent"/> as Model (its <see cref="AIAgent.SecurityContext"/> gives the current user, null for a
        /// caller without user). The text it returns is appended to the system prompt: a context computed per user and
        /// per turn (e.g. the data sets, folders or rights of the user), so one agent serves every user.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tSystem Prompt Script File"), Description("File name of a Razor script (e.g. my-context.cshtml), located in Settings\\AI\\Scripts, executed before each chat turn with the AI Agent as Model (agent.SecurityContext = current user, may be null). The text returned by the script is appended to the system prompt: use it for a context that depends on the user. Click the editor button to edit the script file content."), Id(10, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string SystemPromptScriptFile { get; set; }

        /// <summary>
        /// File name of a text file containing sample user prompts, relative to <c>Settings\AI\Prompts\Samples</c>.
        /// One prompt per line; blank lines and lines starting with <c>#</c> are ignored.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Sample Prompts File"), Description("File name of the sample prompts list (e.g. sample-prompts.md), located in Settings\\AI\\Prompts\\Samples. One prompt per line; lines starting with # are treated as comments. Click the editor button to edit the file content."), Id(11, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string SamplePromptsFile { get; set; }

        /// <summary>
        /// File name of a Razor script, relative to <c>Settings\AI\Scripts</c>, executed when the sample prompts are requested with the
        /// <see cref="AIAgent"/> as Model (its <see cref="AIAgent.SecurityContext"/> gives the current user, may be null). Its output gives
        /// one prompt per line (blank lines and lines starting with <c>#</c> are ignored): sample prompts computed per user.
        /// When the script fails or renders nothing, the prompts of <see cref="SamplePromptsFile"/> are used (see <see cref="AIAgent.GetSamplePrompts"/>).
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Sample Prompts Script File"), Description("File name of a Razor script (e.g. my-samples.cshtml), located in Settings\\AI\\Scripts, executed when the sample prompts are displayed with the AI Agent as Model (agent.SecurityContext = current user, may be null). The script renders one prompt per line (lines starting with # are ignored): use it for sample prompts that depend on the user. If the script fails or renders nothing, the Sample Prompts File is used. Click the editor button to edit the script file content."), Id(12, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string SamplePromptsScriptFile { get; set; }

        /// <summary>
        /// Returns the effective system prompt at runtime.
        /// Reads <see cref="SystemPromptFile"/> from <c>Settings\AI\Prompts</c> when set;
        /// otherwise returns <see cref="DefaultSystemPrompt"/>.
        /// </summary>
        [XmlIgnore]
        public string EffectiveSystemPrompt
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(SystemPromptFile))
                {
                    var path = Path.IsPathRooted(SystemPromptFile)
                        ? SystemPromptFile
                        : Path.Combine(Repository.Instance.AIPromptsFolder, SystemPromptFile);
                    if (!File.Exists(path)) throw new Exception($"System Prompt File '{path}' not found.");
                    return File.ReadAllText(path);
                }
                return DefaultSystemPrompt;
            }
        }

        /// <summary>
        /// Reads and returns the list of sample user prompts from <see cref="SamplePromptsFile"/>.
        /// Returns an empty list when the file is not set or does not exist.
        /// Blank lines and lines starting with <c>#</c> are skipped.
        /// </summary>
        public List<string> GetSamplePrompts()
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(SamplePromptsFile)) return result;
            var path = Path.IsPathRooted(SamplePromptsFile)
                ? SamplePromptsFile
                : Path.Combine(Repository.Instance.AISamplePromptsFolder, SamplePromptsFile);
            if (!File.Exists(path)) return result;
            return ParseSamplePrompts(File.ReadAllLines(path));
        }

        /// <summary>
        /// Returns the sample prompts of the given lines: blank lines and lines starting with <c>#</c> are skipped.
        /// </summary>
        public static List<string> ParseSamplePrompts(IEnumerable<string> lines)
        {
            var result = new List<string>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                    result.Add(trimmed);
            }
            return result;
        }

        /// <summary>
        /// Sampling temperature (0.0 = deterministic, 1.0 = creative).
        /// </summary>
#if WINDOWS
        [Category("Parameters"), DisplayName("Temperature"), Description("Sampling temperature: 0.0 is deterministic, 1.0 is most creative"), Id(1, 2)]
        [DefaultValue(0.0f)]
#endif
        public float Temperature { get; set; } = 0.0f;

        /// <summary>
        /// Maximum number of tokens to generate in a single reply.
        /// Set to 0 to use the provider's default (parameter is not sent).
        /// </summary>
#if WINDOWS
        [Category("Parameters"), DisplayName("Max Tokens"), Description("Maximum number of tokens to generate. Set to 0 to use the provider default."), Id(2, 2)]
        [DefaultValue(0)]
#endif
        public int MaxTokens { get; set; } = 0;

        /// <summary>
        /// Nucleus sampling threshold (0.0–1.0). Use instead of Temperature.
        /// </summary>
#if WINDOWS
        [Category("Parameters"), DisplayName("Top P"), Description("Nucleus sampling: only tokens within the top P probability mass are considered"), Id(3, 2)]
        [DefaultValue(1.0f)]
#endif
        public float TopP { get; set; } = 1.0f;

        /// <summary>
        /// Resolves and returns the <see cref="AIProviderConfiguration"/> identified by <see cref="ProviderGUID"/>.
        /// Returns <c>null</c> when <see cref="ProviderGUID"/> is empty or no matching configuration is found.
        /// </summary>
        public AIProviderConfiguration GetProviderConfiguration()
        {
            var guid = ProviderGUID;
            if (string.IsNullOrEmpty(guid)) return null;
            if (guid == AIProviderConfiguration.DefaultProviderGUID)
            {
                guid = Repository.Instance.AIConfiguration.DefaultProviderGUID;
                if (string.IsNullOrEmpty(guid))
                    return Repository.Instance.AIConfiguration.AIProviders
                        .FirstOrDefault(p => p.IsEnabled);
            }
            return Repository.Instance.AIConfiguration.AIProviders
                .FirstOrDefault(p => p.GUID == guid);
        }

        /// <summary>
        /// Returns the list of enabled <see cref="AIToolConfiguration"/> instances available to this agent.
        /// When <see cref="ToolGUIDs"/> is empty, no tools are returned.
        /// </summary>
        public List<AIToolConfiguration> GetToolConfigurations()
        {
            if (ToolGUIDs == null || ToolGUIDs.Count == 0)
                return new List<AIToolConfiguration>();
            return Repository.Instance.AIConfiguration.AITools
                .Where(t => t.IsEnabled && ToolGUIDs.Contains(t.GUID)).ToList();
        }

        /// <summary>
        /// Returns the list of enabled <see cref="AISkillConfiguration"/> instances available to this agent.
        /// When <see cref="SkillGUIDs"/> is empty, no skills are returned.
        /// </summary>
        public List<AISkillConfiguration> GetSkillConfigurations()
        {
            if (SkillGUIDs == null || SkillGUIDs.Count == 0)
                return new List<AISkillConfiguration>();
            return Repository.Instance.AIConfiguration.AISkills
                .Where(s => s.IsEnabled && SkillGUIDs.Contains(s.GUID)).ToList();
        }

        /// <summary>
        /// Creates and returns the <see cref="IAIProvider"/> for this agent's provider configuration.
        /// Returns <c>null</c> when no matching provider configuration is found.
        /// </summary>
        public IAIProvider GetProvider()
        {
            var providerConfig = GetProviderConfiguration();
            if (providerConfig == null) return null;
            return AIProvider.GetAIProvider(providerConfig.Type, providerConfig.EndPoint, providerConfig.ClearProviderKey, providerConfig.Model, Temperature, MaxTokens, TopP);
        }
    }
}
