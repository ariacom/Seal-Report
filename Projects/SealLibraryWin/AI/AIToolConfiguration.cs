using System.Collections.Generic;
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
                GetProperty("ProgressLabel").SetIsBrowsable(true);
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
        /// Friendly, end-user-facing text shown in the chat "thinking" panel while this tool runs,
        /// instead of the raw tool name. Supports <c>{param}</c> placeholders that are replaced by the
        /// matching argument values of the current call (e.g. <c>Getting columns from table {table}</c>).
        /// Translatable via the repository <c>AIToolProgress</c> context. Leave empty to fall back to a
        /// generic label built from the tool name.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Progress text"), Description("Friendly text shown to the user in the chat 'thinking' panel while this tool runs, instead of the raw tool name. Supports {param} placeholders filled from the call arguments, e.g. 'Getting columns from table {table}'. Leave empty to show a generic label."), Id(3, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string ProgressLabel { get; set; }

        /// <summary>
        /// When <c>false</c>, this tool is excluded from all agent calls without being deleted.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Is Enabled"), Description("When false, this tool is excluded from all agent calls."), Id(4, 1)]
        [DefaultValue(true)]
#endif
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Effect of the tool, published to the MCP clients as tool annotations so that they can ask a confirmation before the tools that write or delete.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Access"), Description("Effect of the tool: 'Read only', 'Write' (creates or adds) or 'Destructive' (deletes, overwrites or modifies existing items). Published to the MCP clients as tool annotations so that they can ask a confirmation before the tools that write or delete. 'Not specified': the clients consider the tool as possibly destructive."), Id(5, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(AIToolAccess.Unspecified)]
#endif
        public AIToolAccess Access { get; set; } = AIToolAccess.Unspecified;

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
        /// Returns the data sources the current security context is allowed to access with the AI tools.
        /// When no security context is set, or the context defines no restriction, all repository sources are returned.
        /// Restrictions are defined through the DataSourceGUIDs of the user's security groups.
        /// </summary>
        public IEnumerable<MetaSource> GetAvailableSources()
        {
            return GetAvailableSources(CurrentToolCall?.SecurityContext);
        }

        /// <summary>
        /// Returns the data sources a security context is allowed to access with the AI tools (all the repository sources when <paramref name="user"/> is null).
        /// </summary>
        public static IEnumerable<MetaSource> GetAvailableSources(SecurityUser user)
        {
            var repository = Repository.Instance;
            if (user == null) return repository.Sources;
            //Private Data Sources listed explicitly by the groups of the user are added
            var privateSources = user.SecurityGroups.SelectMany(g => g.DataSourceGUIDs).Distinct()
                .Where(guid => !repository.Sources.Exists(s => s.GUID == guid))
                .Select(guid => repository.GetPrivateSource(guid))
                .Where(s => s != null);
            return repository.Sources.Concat(privateSources).Where(s => user.CanAccessSource(s));
        }

        /// <summary>
        /// Returns the code level the current tool call can write in a report: the level of the user (Scripts without security context), limited by the maximum level of the agent.
        /// </summary>
        public ReportCodeLevel GetReportCodeLevel()
        {
            var user = CurrentToolCall?.SecurityContext;
            var level = user == null ? ReportCodeLevel.Scripts : user.ReportCodeLevel;
            var agent = CurrentToolCall?.AgentConfiguration;
            if (agent != null && agent.MaxReportCodeLevel < level) level = agent.MaxReportCodeLevel;
            return level;
        }

        /// <summary>
        /// Checks the code (SQL, Razor scripts) of a report XML written by the tool against <see cref="GetReportCodeLevel"/>, see <see cref="ReportCodeGuard"/>.
        /// Returns null if the XML is accepted, otherwise the error to return to the agent.
        /// </summary>
        /// <param name="newXml">The report XML to write</param>
        /// <param name="existingXml">The XML of the report already saved (null for a new report): its content is accepted as is</param>
        /// <param name="path">The path of the report written, for the audit of a refusal</param>
        /// <param name="trustedValues">Texts generated by the tool itself (e.g. the SQL of an enumerated list built from a column), accepted at any level</param>
        public string CheckReportCode(string newXml, string existingXml, string path, IEnumerable<string> trustedValues)
        {
            var level = GetReportCodeLevel();
            var messages = ReportCodeGuard.Check(newXml, existingXml, level, trustedValues, CurrentToolCall?.SecurityContext);
            if (messages.Count == 0) return null;
            ReportCodeGuard.LogRefusal(CurrentToolCall?.SecurityContext, path, level, messages, $"AI tool '{Name}'" + (CurrentToolCall?.AgentConfiguration != null ? $" (agent '{CurrentToolCall.AgentConfiguration.Name}')" : ""));
            return $"Error: the report contains content above your authorization level ('{level}'): " + string.Join("; ", messages) +
                ". Remove this content (or keep it exactly as in the saved report) and call the tool again. Do not retry with the same content: if the request needs it, tell the user that an administrator must grant a higher 'Report code level'.";
        }

        /// <summary>
        /// Checks the code (SQL, Razor scripts) of a report XML written by the tool, see <see cref="CheckReportCode(string, string, string, IEnumerable{string})"/>.
        /// </summary>
        public string CheckReportCode(string newXml, string existingXml = null, string path = null)
        {
            return CheckReportCode(newXml, existingXml, path, null);
        }

        /// <summary>
        /// For the tools writing outside a report (e.g. in a data source): returns an error if an argument of the current tool call is a Razor script (a text starting with '@', compiled by Seal) and the code level is below Scripts, otherwise null.
        /// </summary>
        /// <param name="path">The path of the object written, for the audit of a refusal</param>
        public string CheckRazorArguments(string path = null)
        {
            var level = GetReportCodeLevel();
            if (level >= ReportCodeLevel.Scripts || string.IsNullOrEmpty(CurrentToolCall?.Arguments)) return null;

            var scripts = new List<string>();
            void check(System.Text.Json.JsonElement element, string name)
            {
                switch (element.ValueKind)
                {
                    case System.Text.Json.JsonValueKind.Object:
                        foreach (var property in element.EnumerateObject()) check(property.Value, property.Name);
                        break;
                    case System.Text.Json.JsonValueKind.Array:
                        foreach (var item in element.EnumerateArray()) check(item, name);
                        break;
                    case System.Text.Json.JsonValueKind.String:
                        var value = element.GetString();
                        if (value != null && value.Trim().StartsWith("@")) scripts.Add($"the argument '{name}' starts with '@' (Razor script) and requires the Scripts level");
                        break;
                }
            }
            try
            {
                using (var doc = System.Text.Json.JsonDocument.Parse(CurrentToolCall.Arguments)) check(doc.RootElement, "arguments");
            }
            catch { return null; } //invalid JSON: the tool reports it
            if (scripts.Count == 0) return null;

            scripts = scripts.Distinct().ToList();
            ReportCodeGuard.LogRefusal(CurrentToolCall.SecurityContext, path, level, scripts, $"AI tool '{Name}'" + (CurrentToolCall.AgentConfiguration != null ? $" (agent '{CurrentToolCall.AgentConfiguration.Name}')" : ""));
            return $"Error: the call contains content above your authorization level ('{level}'): " + string.Join("; ", scripts) +
                ". Remove the leading '@' (a SQL text or a name must not be a Razor script) and call the tool again. Do not retry with the same content: if the request needs a script, tell the user that an administrator must grant a higher 'Report code level'.";
        }

        /// <summary>
        /// Returns the repository the tool scripts use to load or create reports (the Private Data Sources used by a report are loaded on demand).
        /// </summary>
        public Repository GetRepository()
        {
            return Repository.Instance;
        }

        /// <summary>
        /// Resolves the data source to use when executing this tool call, restricted to the sources
        /// the current security context is allowed to access (see <see cref="GetAvailableSources"/>).
        /// If <paramref name="datasourceGuid"/> is provided, returns the allowed source with that GUID;
        /// otherwise returns the default allowed source (or the first allowed source).
        /// Returns <c>null</c> when no matching allowed source is found.
        /// </summary>
        public MetaSource GetExecSource(string datasourceGuid = null)
        {
            var available = GetAvailableSources();
            return string.IsNullOrEmpty(datasourceGuid)
                ? available.FirstOrDefault(i => i.IsDefault) ?? available.FirstOrDefault()
                : available.FirstOrDefault(s => s.GUID == datasourceGuid);
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
        /// Returns the output devices the current security context is allowed to use with the AI tools.
        /// When no security context is set, or the context defines no restriction, all repository devices are returned.
        /// Restrictions are defined through the OutputDeviceGUIDs of the user's security groups.
        /// </summary>
        public IEnumerable<OutputDevice> GetAvailableDevices()
        {
            var devices = Repository.Instance.Devices;
            var user = CurrentToolCall?.SecurityContext;
            return user == null ? devices : devices.Where(d => user.CanAccessDevice(d));
        }

        /// <summary>
        /// Builds the user-facing "thinking" label shown in the chat panel while this tool runs:
        /// the translated <see cref="ProgressLabel"/> with any <c>{param}</c> placeholders replaced by
        /// the matching argument values from <paramref name="toolCall"/>. Returns <c>null</c> when no
        /// label is configured so the caller can fall back to a generic label.
        /// </summary>
        public string GetProgressLabel(AIToolCall toolCall, string culture = null)
        {
            if (string.IsNullOrWhiteSpace(ProgressLabel)) return null;
            // Translate with the caller's culture (the web user's session locale) when supplied,
            // otherwise fall back to the repository's current culture.
            var label = string.IsNullOrEmpty(culture)
                ? Repository.Instance.RepositoryTranslate("AIToolProgress", "*", ProgressLabel)
                : Repository.Instance.RepositoryTranslate(culture, "AIToolProgress", "*", ProgressLabel);
            return SubstituteArguments(label, toolCall?.Arguments).Trim();
        }

        /// <summary>
        /// Replaces every <c>{name}</c> placeholder in <paramref name="template"/> with the value of the
        /// matching property in the call's JSON <paramref name="argumentsJson"/>. Unknown or missing
        /// arguments are replaced by an empty string.
        /// </summary>
        private static string SubstituteArguments(string template, string argumentsJson)
        {
            if (string.IsNullOrEmpty(template) || template.IndexOf('{') < 0) return template ?? string.Empty;
            Newtonsoft.Json.Linq.JObject args = null;
            if (!string.IsNullOrWhiteSpace(argumentsJson))
            {
                try { args = Newtonsoft.Json.Linq.JObject.Parse(argumentsJson); } catch { }
            }
            return System.Text.RegularExpressions.Regex.Replace(template, @"\{(\w+)\}", m =>
            {
                var val = args?[m.Groups[1].Value];
                return val != null ? val.ToString() : string.Empty;
            });
        }

        /// <summary>
        /// Returns an <see cref="AITool"/> instance built from this configuration.
        /// </summary>
        public AITool ToAITool() => new AITool
        {
            Name = Name,
            Description = Description,
            ParametersSchema = ParametersSchema,
            Access = Access
        };

        /// <summary>
        /// Runs the <see cref="ExecutionScriptFile"/> via the Razor engine for <paramref name="toolCall"/>
        /// and returns the <see cref="ExecResult"/> set by the script as the tool result string to send back to the AI.
        /// The script runs on a copy of this configuration having <see cref="CurrentToolCall"/> set to <paramref name="toolCall"/>:
        /// the configuration is shared by all the agents of the repository, concurrent calls of the same tool (several users, several calls of a reply)
        /// must not see the call or the result of each other.
        /// Returns <see cref="string.Empty"/> if no script is configured.
        /// </summary>
        public string Execute(AIToolCall toolCall)
        {
            var script = EffectiveExecutionScript;
            if (string.IsNullOrWhiteSpace(script)) return string.Empty;

            var execution = (AIToolConfiguration)MemberwiseClone();
            execution.CurrentToolCall = toolCall;
            execution.ExecResult = string.Empty;

            //File-backed: cache the compiled script by tool and file date, a modified file is compiled again
            //(the in-memory cache of a keyed script is never invalidated, the date must be part of the key)
            var path = Path.IsPathRooted(ExecutionScriptFile)
                ? ExecutionScriptFile
                : Path.Combine(Repository.Instance.AIScriptsFolder, ExecutionScriptFile);
            var lastWrite = File.GetLastWriteTime(path);
            RazorHelper.CompileExecute(script, execution, GetType().Name + "_" + GUID + "_" + lastWrite.Ticks, lastWrite);
            return execution.ExecResult;
        }
    }
}
