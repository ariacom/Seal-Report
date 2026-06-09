using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Seal.Helpers;
using Seal.Model;

#if WINDOWS
using Seal.Forms;
using DynamicTypeDescriptor;
#endif

namespace Seal.AI
{
    public class AIProviderConfiguration : RootEditor
    {
        public const string AIProviderKeysKeyName = "AI Provider Keys";
        public const string AIProviderKeysKeyValue = "wwk93484*%%&%&kjtgé+c$àé";

        /// <summary>
        /// Sentinel value for an assistant's <see cref="AIAssistantConfiguration.ProviderGUID"/> meaning
        /// "use the server's Default Provider" (see <see cref="Seal.AI.AIServerConfiguration.DefaultProviderGUID"/>).
        /// Real providers use <see cref="Guid"/> values, so this can never collide.
        /// </summary>
        public const string DefaultProviderGUID = "1";

        /// <summary>
        /// Display name shown in the property-grid dropdown for the default-provider sentinel.
        /// </summary>
        public const string DefaultProviderName = "<Default Provider>";

        /// <summary>
        /// Display name shown in the property-grid dropdown for an empty default provider,
        /// meaning the first enabled provider is used.
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
                GetProperty("Type").SetIsBrowsable(true);
                GetProperty("Model").SetIsBrowsable(true);
                GetProperty("IsEnabled").SetIsBrowsable(true);
                GetProperty("EndPoint").SetIsBrowsable(true);
                GetProperty("ClearProviderKey").SetIsBrowsable(true);
                TypeDescriptor.Refresh(this);
            }
        }
        #endregion
#endif

        /// <summary>
        /// Unique identifier for this provider configuration.
        /// </summary>
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        string _name = "Provider";
        /// <summary>
        /// Display name of this provider configuration.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tProvider name"), Description("The name of the AI provider"), Id(1, 1)]
#endif
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// When <c>false</c>, this provider is excluded from all operations without being deleted.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Is Enabled"), Description("When false, this provider is excluded from all operations."), Id(3, 1)]
        [DefaultValue(true)]
#endif
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Type of the AI provider.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tProvider type"), Description("Type of the AI provider"), Id(2, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(ProviderType.OpenAI)]
#endif
        public ProviderType Type { get; set; } = ProviderType.OpenAI;

        /// <summary>
        /// Model name to use (e.g. gpt-4o, claude-3-5-sonnet-20241022, llama3).
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Model"), Description("The model name to use (e.g. gpt-4o, claude-3-5-sonnet-20241022, llama3)"), Id(3, 1)]
#endif
        public string Model { get; set; }

        /// <summary>
        /// The resource endpoint URL.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Provider End Point"), Description("The resource endpoint to use (e.g. https://api.anthropic.com/v1/messages or https://<resource>.openai.azure.com/)"), Id(4, 1)]
#endif
        public string EndPoint { get; set; }

        /// <summary>
        /// Encrypted API Key.
        /// </summary>
        public string ProviderKey { get; set; }

        /// <summary>
        /// Provider API Key in clear text.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Provider API Key"), PasswordPropertyText(true), Description("API Key of the AI provider."), Id(5, 1)]
#endif
        [XmlIgnore]
        public string ClearProviderKey
        {
            get
            {
                try
                {
                    return Repository.Instance.DecryptValue(ProviderKey, AIProviderKeysKeyName);
                }
                catch (Exception ex)
                {
                    Helper.WriteLogException("ClearProviderKey get", ex);
                    return ProviderKey;
                }
            }
            set
            {
                try
                {
                    ProviderKey = Repository.Instance.EncryptValue(value, AIProviderKeysKeyName);
                }
                catch (Exception ex)
                {
                    Helper.WriteLogException("ClearProviderKey set", ex);
                    TypeDescriptor.Refresh(this);
                    ProviderKey = value;
                }
            }
        }

    }
}
