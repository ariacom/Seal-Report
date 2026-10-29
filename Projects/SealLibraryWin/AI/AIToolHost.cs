using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Seal.Model;

namespace Seal.AI
{
    /// <summary>
    /// Result of a tool call executed by an <see cref="AIToolHost"/>.
    /// </summary>
    public class AIToolHostResult
    {
        /// <summary>Text result to send back to the AI (an error starts with 'Error:')</summary>
        public string Text = string.Empty;

        /// <summary>Images returned with the text (only when <see cref="AIToolHost.AcceptsImages"/> is true)</summary>
        public List<AIToolResultImage> Images = new List<AIToolResultImage>();

        /// <summary>True if the call was the built-in <see cref="AIToolHost.LoadSkillToolName"/> tool</summary>
        public bool IsSkillLoad = false;

        /// <summary>True if the call unlocked new tools: the tool list must be sent again (see <see cref="AIToolHost.GetTools"/>)</summary>
        public bool ToolsChanged = false;

        /// <summary>Exception raised by the tool script, its message is in <see cref="Text"/></summary>
        public Exception Error = null;
    }

    /// <summary>
    /// Executes the tools of an AI agent configuration for a security context, independently of the AI provider and of the transport:
    /// the tools of the agent, the built-in <c>load_skill</c> tool and the tools unlocked by the skills loaded (progressive disclosure).
    /// Used by the tool loop of <see cref="AIAgent.Chat"/>. An instance holds the skills loaded by one conversation, it is not shared by several conversations.
    /// </summary>
    public class AIToolHost
    {
        /// <summary>
        /// Name of the built-in synthetic tool the AI calls to load a skill's instructions on demand.
        /// </summary>
        public const string LoadSkillToolName = "load_skill";

        /// <summary>
        /// Name of the pseudo-skill returning the complete instructions of the agent (see <see cref="AgentInstructions"/>)
        /// </summary>
        public const string InstructionsSkillName = "instructions";

        /// <summary>
        /// If set, the pseudo-skill <see cref="InstructionsSkillName"/> of <c>load_skill</c> returns these complete instructions of the agent:
        /// used by the MCP server, whose clients truncate long server instructions.
        /// </summary>
        public Func<string> AgentInstructions = null;

        /// <summary>
        /// Hard ceiling (in characters) for a single tool result appended to the conversation.
        /// A result larger than this (roughly 50k tokens) would risk exceeding the model's context
        /// window — and since the history is resent on every turn, it would brick the whole session.
        /// </summary>
        public const int MaxToolResultLength = 200000;

        /// <summary>The agent configuration defining the tools and the skills</summary>
        public AIAgentConfiguration Configuration { get; }

        /// <summary>Security context of the tool calls</summary>
        public SecurityUser SecurityContext = null;

        /// <summary>Culture used to translate the progress labels (null for the repository culture)</summary>
        public string Culture = null;

        /// <summary>True if the AI accepts images in a tool result</summary>
        public bool AcceptsImages = false;

        /// <summary>Log of the tool executions</summary>
        public ReportExecutionLog ExecutionLog = null;

        readonly List<AISkillConfiguration> _skills;
        readonly bool _hasTools;
        // Tools currently callable, keyed by name: the agent's base tools, then the tools of the skills loaded
        readonly Dictionary<string, AIToolConfiguration> _activeTools = new Dictionary<string, AIToolConfiguration>();
        readonly HashSet<string> _loadedSkills = new HashSet<string>();
        // Tools activated by ActivateAllTools without their skill loaded: tool name -> skill name
        readonly Dictionary<string, string> _toolSkills = new Dictionary<string, string>();

        /// <summary>
        /// Creates the host of the tools of <paramref name="configuration"/>: the tools of the agent are active, no skill is loaded.
        /// </summary>
        public AIToolHost(AIAgentConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            var tools = Configuration.GetToolConfigurations();
            foreach (var t in tools) _activeTools[t.Name] = t;
            _skills = Configuration.GetSkillConfigurations();
            _hasTools = tools.Count > 0 || _skills.Count > 0;
        }

        /// <summary>True if the agent has tools or skills (otherwise a plain chat without tool loop)</summary>
        public bool HasTools => _hasTools;

        /// <summary>The enabled skills of the agent</summary>
        public IReadOnlyList<AISkillConfiguration> Skills => _skills;

        /// <summary>The names of the skills loaded</summary>
        public IReadOnlyCollection<string> LoadedSkills => _loadedSkills;

        /// <summary>
        /// Marks a skill as loaded and activates its tools, without returning its instructions (e.g. a skill loaded earlier in a resumed conversation).
        /// Returns true if new tools were activated.
        /// </summary>
        public bool ActivateSkill(string skillName)
        {
            var skill = _skills.FirstOrDefault(s => s.Name == skillName);
            if (skill == null || !_loadedSkills.Add(skill.Name)) return false;
            var changed = false;
            foreach (var t in skill.GetToolConfigurations())
            {
                if (!_activeTools.ContainsKey(t.Name))
                {
                    _activeTools[t.Name] = t;
                    changed = true;
                }
            }
            return changed;
        }

        /// <summary>
        /// Activates the tools of all the skills without loading the skills, for a client that cannot receive a new tool list during a conversation (MCP server without session):
        /// the description of a tool of a skill not loaded tells the AI to load the skill first (see <see cref="GetTools"/>).
        /// </summary>
        public void ActivateAllTools()
        {
            foreach (var skill in _skills)
            {
                if (_loadedSkills.Contains(skill.Name)) continue;
                foreach (var t in skill.GetToolConfigurations())
                {
                    if (_activeTools.ContainsKey(t.Name)) continue;
                    _activeTools[t.Name] = t;
                    _toolSkills[t.Name] = skill.Name;
                }
            }
        }

        /// <summary>
        /// Returns the tools to send to the AI: the active tools plus the built-in <c>load_skill</c> tool when the agent has skills.
        /// </summary>
        public List<AITool> GetTools()
        {
            var list = _activeTools.Values.Select(t =>
            {
                var tool = t.ToAITool();
                if (_toolSkills.TryGetValue(t.Name, out var skillName) && !_loadedSkills.Contains(skillName))
                    tool.Description = $"Requires the skill '{skillName}': call '{LoadSkillToolName}' with skill_name '{skillName}' once before the first call of this tool, and follow its instructions.\n" + tool.Description;
                return tool;
            }).ToList();
            if (_skills.Count > 0 || AgentInstructions != null) list.Add(BuildLoadSkillTool(_skills, AgentInstructions != null));
            return list;
        }

        /// <summary>
        /// Returns the progress label of a tool call shown to the user while it runs.
        /// </summary>
        public string GetProgressLabel(AIToolCall toolCall)
        {
            if (toolCall.Name == LoadSkillToolName)
            {
                var skillName = ExtractSkillName(toolCall.Arguments);
                if (skillName == InstructionsSkillName && AgentInstructions != null) return "Loading the instructions of the agent";
                return _skills.FirstOrDefault(s => s.Name == skillName)?.GetProgressLabel(Culture) ?? $"Loading skill '{skillName}'";
            }
            var toolConfig = _activeTools.TryGetValue(toolCall.Name, out var tc) ? tc : null;
            return toolConfig?.GetProgressLabel(toolCall, Culture) ?? $"Running '{toolCall.Name}'";
        }

        /// <summary>
        /// Executes a tool call: loads a skill for <c>load_skill</c>, otherwise runs the active tool with the security context of the host.
        /// A script failure or an inactive tool becomes an error text the AI can recover from, the text is truncated to <see cref="MaxToolResultLength"/>.
        /// </summary>
        public AIToolHostResult Execute(AIToolCall toolCall, ICancelOperation cancelOperation = null)
        {
            var result = new AIToolHostResult();

            // Built-in skill loader: return the skill instructions and unlock its tools.
            if (toolCall.Name == LoadSkillToolName)
            {
                result.IsSkillLoad = true;
                result.Text = loadSkill(toolCall, out result.ToolsChanged);
                return result;
            }

            toolCall.SecurityContext = SecurityContext;
            toolCall.AgentConfiguration = Configuration;
            toolCall.ExecutionLog = ExecutionLog;
            toolCall.CancelOperation = cancelOperation;
            toolCall.AcceptsImages = AcceptsImages;

            // The tool is not active yet. It almost always belongs to a skill that has not been
            // loaded: returning an empty result silently misleads the model (it reads it as "no
            // data" and answers wrong). Return an actionable error so it loads the skill and retries.
            if (!_activeTools.TryGetValue(toolCall.Name, out var toolConfig))
            {
                result.Text = $"Error: tool '{toolCall.Name}' is not available. If it belongs to a skill, call '{LoadSkillToolName}' to load that skill first, then retry.";
                return result;
            }

            try
            {
                result.Text = TruncateToolResult(toolConfig.Execute(toolCall) ?? string.Empty);
            }
            catch (Exception ex)
            {
                // Tool scripts are user-editable Razor: a crash there (e.g. GetProperty on a
                // missing JSON argument) must become a tool error the model can recover from,
                // not abort the whole chat exchange.
                var hint = ex is KeyNotFoundException ? " A required argument is probably missing: check the call arguments against the tool's parameters schema and retry." : "";
                result.Text = $"Error: tool '{toolCall.Name}' failed: {ex.Message}{hint}";
                result.Error = ex;
            }
            if (AcceptsImages) result.Images.AddRange(toolCall.ResultImages.Where(img => img.Data != null && img.Data.Length > 0));
            return result;
        }

        /// <summary>
        /// Returns <paramref name="result"/> truncated to <see cref="MaxToolResultLength"/> characters,
        /// with an explicit marker so the model knows data is missing and can narrow its request.
        /// </summary>
        public static string TruncateToolResult(string result)
        {
            if (result == null || result.Length <= MaxToolResultLength) return result;
            return result.Substring(0, MaxToolResultLength) +
                $"\r\n[Result truncated: {result.Length - MaxToolResultLength} characters removed — the tool returned too much data for the conversation. Retry with a more specific request (filters, restrictions, fewer columns).]";
        }

        /// <summary>
        /// Extracts the <c>skill_name</c> argument from a <c>load_skill</c> tool call's JSON arguments.
        /// Returns <c>null</c> when it cannot be parsed.
        /// </summary>
        public static string ExtractSkillName(string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments)) return null;
            try
            {
                var obj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(arguments);
                return obj?["skill_name"]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Builds the synthetic <c>load_skill</c> tool. Its description embeds the live catalog of the
        /// agent's skills (name: description) and its single <c>skill_name</c> parameter is constrained to
        /// the available skill names.
        /// </summary>
        static AITool BuildLoadSkillTool(List<AISkillConfiguration> skillConfigs, bool withInstructions = false)
        {
            var catalog = string.Join("\n", skillConfigs.Select(s => $"- {s.Name}: {s.Description}"));
            if (withInstructions) catalog = $"- {InstructionsSkillName}: the complete instructions and rules of this agent (the server instructions can be truncated by the client). Load it once at the start of the conversation, before any other call.\n" + catalog;
            var description =
                "Load the detailed instructions for a skill before performing related work. " +
                "Call this as soon as a user request matches one of the skills below; the full instructions " +
                "(and any tools the skill needs) become available after loading. Available skills:\n" + catalog;
            var names = skillConfigs.Select(s => s.Name).ToList();
            if (withInstructions) names.Insert(0, InstructionsSkillName);

            var schema = JsonConvert.SerializeObject(new
            {
                type = "object",
                properties = new
                {
                    skill_name = new
                    {
                        type = "string",
                        description = "Name of the skill to load.",
                        @enum = names
                    }
                },
                required = new[] { "skill_name" }
            });

            return new AITool { Name = LoadSkillToolName, Description = description, ParametersSchema = schema, Access = Model.AIToolAccess.ReadOnly };
        }

        /// <summary>
        /// Resolves the skill requested by a <c>load_skill</c> tool call, returns its instructions as the
        /// tool result, and activates the skill's tools the first time it is loaded.
        /// Sets <paramref name="toolsChanged"/> to <c>true</c> when new tools were activated.
        /// </summary>
        string loadSkill(AIToolCall toolCall, out bool toolsChanged)
        {
            toolsChanged = false;
            var skillName = ExtractSkillName(toolCall.Arguments);
            if (skillName == InstructionsSkillName && AgentInstructions != null)
            {
                try
                {
                    var instructions = AgentInstructions();
                    return string.IsNullOrWhiteSpace(instructions) ? "The agent has no instructions configured." : instructions;
                }
                catch (Exception ex)
                {
                    return $"Unable to load the instructions of the agent: {ex.Message}";
                }
            }
            var skill = _skills.FirstOrDefault(s => s.Name == skillName);
            if (skill == null)
                return $"Unknown skill '{skillName}'. Available skills: {string.Join(", ", _skills.Select(s => s.Name))}.";

            toolsChanged = ActivateSkill(skill.Name);
            try
            {
                var instructions = skill.EffectiveInstructions;
                return string.IsNullOrWhiteSpace(instructions)
                    ? $"Skill '{skill.Name}' has no instructions configured."
                    : instructions;
            }
            catch (Exception ex)
            {
                return $"Unable to load skill '{skill.Name}': {ex.Message}";
            }
        }
    }
}
