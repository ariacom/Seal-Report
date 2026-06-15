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
    /// Configuration for a single AI skill: a packaged set of instructions (loaded on demand)
    /// plus an optional set of tools that become available once the skill is loaded.
    /// Stored in <see cref="Seal.AI.AIServerConfiguration.AISkills"/> and edited through the Server Manager.
    /// </summary>
    /// <remarks>
    /// Unlike an <see cref="AIToolConfiguration"/> (a callable function), a skill carries no
    /// parameter schema and runs no script. Only its <see cref="Name"/> and <see cref="Description"/>
    /// are exposed to the AI model up front (via the built-in <c>load_skill</c> tool catalog).
    /// The full <see cref="EffectiveInstructions"/> body is returned to the model only when it
    /// decides the skill is relevant and calls <c>load_skill</c> — progressive disclosure.
    /// </remarks>
    public class AISkillConfiguration : RootEditor
    {
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
                GetProperty("InstructionsFile").SetIsBrowsable(true);
                GetProperty("ToolGUIDs").SetIsBrowsable(true);
                TypeDescriptor.Refresh(this);
            }
        }
        #endregion
#endif

        /// <summary>
        /// Unique identifier for this skill configuration.
        /// </summary>
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        string _name = "skill-name";
        /// <summary>
        /// Unique skill name presented to the AI model and used as the <c>skill_name</c> argument
        /// of the built-in <c>load_skill</c> tool. Use kebab-case (e.g. <c>build-report-from-sql</c>).
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tSkill name"), Description("Unique skill name presented to the AI model and used as the load_skill argument (e.g. build-report-from-sql). Use kebab-case."), Id(1, 1)]
#endif
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Short description of what the skill does and when to use it. Always visible to the AI
        /// model in the <c>load_skill</c> catalog, so it must be enough for the model to decide
        /// whether to load the skill.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Description"), Description("Short description of what the skill does and when to use it. Always shown to the AI model so it can decide whether to load the skill."), Id(2, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string Description { get; set; }

        /// <summary>
        /// When <c>false</c>, this skill is excluded from all agents without being deleted.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Is Enabled"), Description("When false, this skill is excluded from all agents."), Id(4, 1)]
        [DefaultValue(true)]
#endif
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// File name of the instructions file, relative to <c>Settings\AI\Skills</c>.
        /// The full text is returned to the AI model when the skill is loaded.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Instructions File"), Description("File name of the skill instructions (e.g. build-report-from-sql.md), located in Settings\\AI\\Skills. The full text is returned to the AI model when the skill is loaded. Click the editor button to edit the file content."), Id(5, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string InstructionsFile { get; set; }

        /// <summary>
        /// GUIDs of the <see cref="AIToolConfiguration"/> instances that become available to the
        /// agent once this skill is loaded. Leave empty for an instruction-only skill.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Tools"), Description("The tools that become available to the agent once this skill is loaded. Leave empty for an instruction-only skill."), Id(6, 1)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
#endif
        [XmlArray("ToolGUIDs")]
        [XmlArrayItem("GUID")]
        public List<string> ToolGUIDs { get; set; } = new List<string>();

        /// <summary>
        /// Reads <see cref="InstructionsFile"/> from <c>Settings\AI\Skills</c> as plain text.
        /// Returns an empty string when no instructions file is configured.
        /// </summary>
        [XmlIgnore]
        public string EffectiveInstructions
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(InstructionsFile))
                {
                    var path = Path.IsPathRooted(InstructionsFile)
                        ? InstructionsFile
                        : Path.Combine(Repository.Instance.AISkillsFolder, InstructionsFile);
                    if (!File.Exists(path)) throw new Exception($"Instructions File '{path}' not found.");
                    return File.ReadAllText(path);
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns the list of enabled <see cref="AIToolConfiguration"/> instances activated by this skill.
        /// </summary>
        public List<AIToolConfiguration> GetToolConfigurations()
        {
            var allEnabled = Repository.Instance.AIConfiguration.AITools.Where(t => t.IsEnabled);
            if (ToolGUIDs == null || ToolGUIDs.Count == 0)
                return new List<AIToolConfiguration>();
            return allEnabled.Where(t => ToolGUIDs.Contains(t.GUID)).ToList();
        }
    }
}
