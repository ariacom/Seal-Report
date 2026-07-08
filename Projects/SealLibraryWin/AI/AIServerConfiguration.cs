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
            if (ExternalAIProviders) Helper.Serialize(providersPath, AIProviders);
            else if (File.Exists(providersPath)) File.Delete(providersPath);
        }
    }
}
