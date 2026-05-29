using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Seal.Model;

#if WINDOWS
using System.ComponentModel;
using System.Drawing.Design;
using Seal.Forms;
using DynamicTypeDescriptor;
#endif

namespace Seal.AI
{
    /// <summary>
    /// Configuration for an AI assistant: ties together one provider, an optional subset of tools,
    /// and a default system prompt. Stored in <see cref="Seal.AI.AIServerConfiguration.AIAssistants"/>.
    /// </summary>
    public class AIAssistantConfiguration : RootEditor
    {
        /// <summary>Hardcoded GUID sentinel for "use the server default assistant".</summary>
        public const string DefaultAssistantGUIDValue = "1";
        public const string DefaultAssistant = "<Default Assistant>";
        public const string NoAssistant = "<No Assistant>";

#if WINDOWS
        #region Editor
        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                foreach (var property in Properties) property.SetIsBrowsable(false);
                GetProperty("Name").SetIsBrowsable(true);
                GetProperty("IsEnabled").SetIsBrowsable(true);
                GetProperty("ProviderGUID").SetIsBrowsable(true);
                GetProperty("ToolGUIDs").SetIsBrowsable(true);
                GetProperty("DefaultSystemPrompt").SetIsBrowsable(true);
                GetProperty("SystemPromptFile").SetIsBrowsable(true);
                GetProperty("SamplePromptsFile").SetIsBrowsable(true);
                GetProperty("Temperature").SetIsBrowsable(true);
                GetProperty("MaxTokens").SetIsBrowsable(true);
                GetProperty("TopP").SetIsBrowsable(true);
                TypeDescriptor.Refresh(this);
            }
        }
        #endregion
#endif

        /// <summary>
        /// Unique identifier for this assistant configuration.
        /// </summary>
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        string _name = "Assistant";
        /// <summary>
        /// Display name of this assistant configuration.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\tAssistant name"), Description("Display name of this AI assistant."), Id(1, 1)]
#endif
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// When <c>false</c>, this assistant is excluded from all operations without being deleted.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\tIs Enabled"), Description("When false, this assistant is excluded from all operations."), Id(4, 1)]
        [DefaultValue(true)]
#endif
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// GUID of the <see cref="AIProviderConfiguration"/> to use.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\tProvider"), Description("The AI provider to use for this assistant."), Id(5, 1)]
        [TypeConverter(typeof(AIProviderConverter))]
#endif
        public string ProviderGUID { get; set; }

        /// <summary>
        /// GUIDs of the <see cref="AIToolConfiguration"/> instances available to this assistant.
        /// An empty list means all enabled tools are available.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\tTools"), Description("The tools available to this assistant. Leave empty to make all enabled tools available."), Id(6, 1)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
#endif
        [XmlArray("ToolGUIDs")]
        [XmlArrayItem("GUID")]
        public List<string> ToolGUIDs { get; set; } = new List<string>();

        /// <summary>
        /// Inline system prompt injected at the start of every conversation.
        /// Ignored when <see cref="SystemPromptFile"/> is set.
        /// Leave empty to use no system prompt.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tDefault System Prompt"), Description("Inline system prompt. Ignored when System Prompt File is set. Leave empty for none."), Id(7, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string DefaultSystemPrompt { get; set; }

        /// <summary>
        /// File name of a text file containing the system prompt, relative to <c>Settings\AI\Prompts</c>.
        /// When set, takes precedence over <see cref="DefaultSystemPrompt"/>.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tSystem Prompt File"), Description("File name of the system prompt (e.g. my-prompt.md), located in Settings\\AI\\Prompts. When set, overrides Default System Prompt."), Id(8, 1)]
#endif
        public string SystemPromptFile { get; set; }

        /// <summary>
        /// File name of a text file containing sample user prompts, relative to <c>Settings\AI\Prompts\Samples</c>.
        /// One prompt per line; blank lines and lines starting with <c>#</c> are ignored.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Sample Prompts File"), Description("File name of the sample prompts list (e.g. sample-prompts.md), located in Settings\\AI\\Prompts\\Samples. One prompt per line; lines starting with # are treated as comments."), Id(9, 1)]
#endif
        public string SamplePromptsFile { get; set; }

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
            foreach (var line in File.ReadAllLines(path))
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
            if (string.IsNullOrEmpty(ProviderGUID)) return null;
            return Repository.Instance.AIConfiguration.AIProviders
                .FirstOrDefault(p => p.GUID == ProviderGUID);
        }

        /// <summary>
        /// Returns the list of enabled <see cref="AIToolConfiguration"/> instances available to this assistant.
        /// When <see cref="ToolGUIDs"/> is empty, all enabled tools are returned.
        /// </summary>
        public List<AIToolConfiguration> GetToolConfigurations()
        {
            var allEnabled = Repository.Instance.AIConfiguration.AITools.Where(t => t.IsEnabled);
            if (ToolGUIDs == null || ToolGUIDs.Count == 0)
                return allEnabled.ToList();
            return allEnabled.Where(t => ToolGUIDs.Contains(t.GUID)).ToList();
        }

        /// <summary>
        /// Creates and returns the <see cref="IAIProvider"/> for this assistant's provider configuration.
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
