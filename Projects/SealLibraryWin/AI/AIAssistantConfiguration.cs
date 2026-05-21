using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// and a default system prompt. Stored in <see cref="Seal.Model.SealServerConfiguration.AIAssistants"/>.
    /// </summary>
    public class AIAssistantConfiguration : RootEditor
    {
#if WINDOWS
        #region Editor
        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                foreach (var property in Properties) property.SetIsBrowsable(false);
                GetProperty("Name").SetIsBrowsable(true);
                GetProperty("IsDefault").SetIsBrowsable(true);
                GetProperty("IsEnabled").SetIsBrowsable(true);
                GetProperty("ProviderGUID").SetIsBrowsable(true);
                GetProperty("ToolGUIDs").SetIsBrowsable(true);
                GetProperty("DefaultSystemPrompt").SetIsBrowsable(true);
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
        [Category("Definition"), DisplayName("\tAssistant name"), Description("Display name of this AI assistant."), Id(1, 1)]
#endif
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// When <c>true</c>, this assistant is used by default when no assistant is specified.
        /// Only one assistant should have this flag set.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Is Default"), Description("When true, this assistant is used by default when no assistant is specified. Only one assistant should have this flag set."), Id(2, 1)]
        [DefaultValue(false)]
#endif
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// When <c>false</c>, this assistant is excluded from all operations without being deleted.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Is Enabled"), Description("When false, this assistant is excluded from all operations."), Id(4, 1)]
        [DefaultValue(true)]
#endif
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// GUID of the <see cref="AIProviderConfiguration"/> to use.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Provider"), Description("The AI provider to use for this assistant."), Id(5, 1)]
        [TypeConverter(typeof(AIProviderConverter))]
#endif
        public string ProviderGUID { get; set; }

        /// <summary>
        /// GUIDs of the <see cref="AIToolConfiguration"/> instances available to this assistant.
        /// An empty list means all enabled tools are available.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Tools"), Description("The tools available to this assistant. Leave empty to make all enabled tools available."), Id(6, 1)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
#endif
        [XmlArray("ToolGUIDs")]
        [XmlArrayItem("GUID")]
        public List<string> ToolGUIDs { get; set; } = new List<string>();

        /// <summary>
        /// System prompt injected at the start of every conversation for this assistant.
        /// Leave empty to use no system prompt.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Default System Prompt"), Description("System prompt injected at the start of every conversation. Leave empty for none."), Id(7, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string DefaultSystemPrompt { get; set; }

        /// <summary>
        /// Resolves and returns the <see cref="AIProviderConfiguration"/> identified by <see cref="ProviderGUID"/>.
        /// Returns <c>null</c> when <see cref="ProviderGUID"/> is empty or no matching configuration is found.
        /// </summary>
        public AIProviderConfiguration GetProviderConfiguration()
        {
            if (string.IsNullOrEmpty(ProviderGUID)) return null;
            return Repository.Instance.Configuration.AIProviders
                .FirstOrDefault(p => p.GUID == ProviderGUID);
        }

        /// <summary>
        /// Returns the list of enabled <see cref="AIToolConfiguration"/> instances available to this assistant.
        /// When <see cref="ToolGUIDs"/> is empty, all enabled tools are returned.
        /// </summary>
        public List<AIToolConfiguration> GetToolConfigurations()
        {
            var allEnabled = Repository.Instance.Configuration.AITools.Where(t => t.IsEnabled);
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
            return AIProvider.GetAIProvider(providerConfig.Type, providerConfig.EndPoint, providerConfig.ClearProviderKey, providerConfig.Model, providerConfig.Temperature, providerConfig.MaxTokens, providerConfig.TopP);
        }
    }
}
