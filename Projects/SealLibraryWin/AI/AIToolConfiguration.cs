using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Seal.Helpers;
using Seal.Model;
using System;


#if WINDOWS
using System.Drawing.Design;
using Seal.Forms;
using DynamicTypeDescriptor;
#endif

namespace Seal.AI
{
    /// <summary>
    /// Configuration for a single AI tool (function) available to AI providers.
    /// Stored in <see cref="Seal.AI.AIServerConfiguration.AITools"/> and edited
    /// through the Server Manager.
    /// </summary>
    public class AIToolConfiguration : RootEditor
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
                GetProperty("ParametersSchema").SetIsBrowsable(true);
                GetProperty("ExecutionScriptFile").SetIsBrowsable(true);
                TypeDescriptor.Refresh(this);
            }
        }
        #endregion
#endif

        /// <summary>
        /// Unique identifier for this tool configuration.
        /// </summary>
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        string _name = "tool_name";
        /// <summary>
        /// Unique function name sent to the AI model (e.g. <c>get_report_data</c>).
        /// Must contain only letters, digits, and underscores.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tTool name"), Description("Unique function name sent to the AI model (e.g. get_report_data). Must contain only letters, digits and underscores."), Id(1, 1)]
#endif
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Human-readable description of what the tool does, shown to the AI model to help
        /// it decide when to call the tool.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Description"), Description("Human-readable description of what the tool does. Shown to the AI model to help it decide when to call the tool."), Id(2, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string Description { get; set; }

        /// <summary>
        /// When <c>false</c>, this tool is excluded from all agent calls without being deleted.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Is Enabled"), Description("When false, this tool is excluded from all agent calls."), Id(4, 1)]
        [DefaultValue(true)]
#endif
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// JSON Schema string describing the function's parameters, sent to the AI model.
        /// Example: <c>{"type":"object","properties":{"city":{"type":"string"}},"required":["city"]}</c>
        /// Leave empty for tools that take no parameters.
        /// </summary>
#if WINDOWS
        [Category("Parameters"), DisplayName("Parameters Schema"), Description("JSON Schema describing the function parameters sent to the AI model.\r\nExample: {\"type\":\"object\",\"properties\":{\"city\":{\"type\":\"string\"}},\"required\":[\"city\"]}\r\nLeave empty for tools that take no parameters."), Id(5, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string ParametersSchema { get; set; }

        /// <summary>
        /// File name of a Razor/C# script file, relative to <c>Settings\AI\Scripts</c>.
        /// </summary>
#if WINDOWS
        [Category("Execution"), DisplayName("Execution Script File"), Description("File name of the execution script (e.g. my_tool.cshtml), located in Settings\\AI\\Scripts. Click the editor button to edit the script file content with syntax check (F8)."), Id(7, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string ExecutionScriptFile { get; set; }

        /// <summary>
        /// Reads <see cref="ExecutionScriptFile"/> from <c>Settings\AI\Scripts</c>.
        /// Returns an empty string when no script file is configured.
        /// </summary>
        [XmlIgnore]
        public string EffectiveExecutionScript
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ExecutionScriptFile))
                {
                    var path = Path.IsPathRooted(ExecutionScriptFile)
                        ? ExecutionScriptFile
                        : Path.Combine(Repository.Instance.AIScriptsFolder, ExecutionScriptFile);
                    return File.ReadAllText(path);
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Set by <see cref="Execute"/> immediately before the script runs so that the script can
        /// access the current call's arguments, id, and name via <c>@Model.CurrentToolCall</c>.
        /// </summary>
        [XmlIgnore]
        public AIToolCall CurrentToolCall { get; set; }

        /// <summary>
        /// The script must assign the tool result string to this property.
        /// <see cref="Execute"/> returns this value after the Razor engine has run.
        /// </summary>
        [XmlIgnore]
        public string ExecResult { get; set; }

        /// <summary>
        /// Resolves the data source to use when executing this tool call.
        /// If <paramref name="datasourceGuid"/> is provided, returns the source with that GUID;
        /// otherwise returns the default source.
        /// Returns <c>null</c> when no matching source is found.
        /// </summary>
        public MetaSource GetExecSource(string datasourceGuid = null)
        {
            return string.IsNullOrEmpty(datasourceGuid)
                ? Repository.Instance.Sources.Where(i => i.IsDefault).FirstOrDefault()
                : Repository.Instance.Sources.FirstOrDefault(s => s.GUID == datasourceGuid);
        }

        /// <summary>
        /// Resolves the connection to use for the given <paramref name="source"/>.
        /// If <paramref name="connectionGuid"/> is provided, returns the connection with that GUID;
        /// otherwise returns the source's current default connection.
        /// Returns <c>null</c> when <paramref name="source"/> is <c>null</c> or no matching connection is found.
        /// </summary>
        public MetaConnection GetExecConnection(MetaSource source, string connectionGuid = null)
        {
            if (source == null) return null;
            return string.IsNullOrEmpty(connectionGuid)
                ? source.Connection
                : source.Connections.FirstOrDefault(c => c.GUID == connectionGuid);
        }

        /// <summary>
        /// Returns an <see cref="AITool"/> instance built from this configuration.
        /// </summary>
        public AITool ToAITool() => new AITool
        {
            Name = Name,
            Description = Description,
            ParametersSchema = ParametersSchema
        };

        /// <summary>
        /// Sets <see cref="CurrentToolCall"/> to <paramref name="toolCall"/>, clears
        /// <see cref="ExecResult"/>, runs the <see cref="ExecutionScriptFile"/> via the Razor engine,
        /// and returns <see cref="ExecResult"/> as the tool result string to send back to the AI.
        /// Returns <see cref="string.Empty"/> if no script is configured.
        /// </summary>
        public string Execute(AIToolCall toolCall)
        {
            CurrentToolCall = toolCall;
            ExecResult = string.Empty;
            var script = EffectiveExecutionScript;
            if (string.IsNullOrWhiteSpace(script)) return string.Empty;

            //File-backed: cache the compiled script by file path and invalidate on file change
            var path = Path.IsPathRooted(ExecutionScriptFile)
                ? ExecutionScriptFile
                : Path.Combine(Repository.Instance.AIScriptsFolder, ExecutionScriptFile);
            RazorHelper.CompileExecute(script, this, GetType().Name + "_" + GUID, File.GetLastWriteTime(path));
            return ExecResult;
        }
    }
}
