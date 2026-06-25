using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ganss.Xss;
using Newtonsoft.Json;
using OpenAI.Chat;
using Seal.Model;

namespace Seal.AI
{
    /// <summary>
    /// Stateful AI agent that owns a conversation history and delegates to
    /// an <see cref="IAIProvider"/> for all model calls.
    /// Initialise once per session or per conversation; call <see cref="Chat"/>
    /// for each user turn.
    /// </summary>
    public class AIAgent
    {
        /// <summary>
        /// The configuration used to build this agent.
        /// </summary>
        public AIAgentConfiguration Configuration { get; }

        private readonly IAIProvider _provider;

        /// <summary>
        /// Full conversation history, including the system prompt (if any),
        /// all user turns, and all agent replies.
        /// Exposed so callers can inspect or serialise it.
        /// </summary>
        public List<ChatMessage> Messages { get; } = new List<ChatMessage>();

        // ----------------------------------------------------------------
        //  Construction
        // ----------------------------------------------------------------

        /// <summary>
        /// Creates an <see cref="AIAgent"/> from a named
        /// <see cref="AIAgentConfiguration"/>.
        /// Injects the configuration's <see cref="AIAgentConfiguration.EffectiveSystemPrompt"/>
        /// as the first message in the conversation when it is not empty.
        /// </summary>
        /// <param name="agentName">
        /// Name of the <see cref="AIAgentConfiguration"/> to look up.
        /// Pass <c>null</c> or empty to use the default configuration
        /// (set via <see cref="AIServerConfiguration.DefaultAgentGUID"/>).
        /// </param>
        public AIAgent(string agentName = null)
        {
            // Resolve configuration
            if (string.IsNullOrEmpty(agentName))
            {
                var defaultGuid = Repository.Instance.AIConfiguration.DefaultAgentGUID;
                var agents = Repository.Instance.AIConfiguration.AIAgents;
                Configuration = (string.IsNullOrEmpty(defaultGuid)
                        ? agents.Find(a => a.IsEnabled)
                        : agents.Find(a => a.GUID == defaultGuid && a.IsEnabled))
                    ?? throw new System.Exception(
                        "No default AI agent configuration found. " +
                        "Set Default Agent in AI Configuration.");
            }
            else
            {
                Configuration = Repository.Instance.AIConfiguration.AIAgents
                    .Find(a => a.Name == agentName && a.IsEnabled)
                    ?? throw new System.Exception(
                        $"AI agent configuration '{agentName}' not found or is disabled.");
            }

            _provider = Configuration.GetProvider()
                ?? throw new System.Exception(
                    $"AI agent '{Configuration.Name}' has no valid provider configuration.");

            // Seed conversation with the system prompt if one is defined
            if (!string.IsNullOrWhiteSpace(Configuration.EffectiveSystemPrompt))
                Messages.Add(new SystemChatMessage(Configuration.EffectiveSystemPrompt));
        }

        /// <summary>
        /// Creates an <see cref="AIAgent"/> directly from an existing
        /// <see cref="AIAgentConfiguration"/> instance.
        /// </summary>
        public AIAgent(AIAgentConfiguration configuration)
        {
            Configuration = configuration
                ?? throw new System.ArgumentNullException(nameof(configuration));

            _provider = Configuration.GetProvider()
                ?? throw new System.Exception(
                    $"AI agent '{Configuration.Name}' has no valid provider configuration.");

            if (!string.IsNullOrWhiteSpace(Configuration.EffectiveSystemPrompt))
                Messages.Add(new SystemChatMessage(Configuration.EffectiveSystemPrompt));
        }

        // ----------------------------------------------------------------
        //  Chat
        // ----------------------------------------------------------------

        /// <summary>
        /// Name of the built-in synthetic tool the AI calls to load a skill's instructions on demand.
        /// </summary>
        internal const string LoadSkillToolName = "load_skill";

        /// <summary>
        /// Appends a user message to the conversation history, calls the AI using
        /// the tools scoped to this agent (via <see cref="AIAgentConfiguration.GetToolConfigurations"/>)
        /// plus a built-in <c>load_skill</c> tool when the agent has skills
        /// (via <see cref="AIAgentConfiguration.GetSkillConfigurations"/>),
        /// executes any tool calls the AI requests, and repeats until a plain reply is
        /// produced or <paramref name="maxIterations"/> is reached.
        /// Falls back to a plain <see cref="IAIProvider.HandleChat"/> call when no tools and no skills are configured.
        /// </summary>
        /// <remarks>
        /// Skills use progressive disclosure: only each skill's name and description are exposed up front
        /// (in the <c>load_skill</c> tool's catalog). When the AI calls <c>load_skill</c>, the skill's full
        /// instructions are returned as the tool result and the skill's own tools are merged into the active
        /// tool set for the rest of the conversation.
        /// </remarks>
        public string Chat(string userMessage, ICancelOperation cancelOperation = null, ReportExecutionLog log = null, ReportExecutionLog toolsLog = null, int maxIterations = 10, Action<string> progress = null)
        {
            var messageCountBefore = Messages.Count;
            Messages.Add(new UserChatMessage(userMessage));

            var toolConfigs = Configuration.GetToolConfigurations();
            var skillConfigs = Configuration.GetSkillConfigurations();

            if (log != null)
            {
                var pc = Configuration.GetProviderConfiguration();
                var systemPrompt = Messages.OfType<SystemChatMessage>().FirstOrDefault()?.Content[0].Text;
                log.LogMessage($"[AIAgent] Provider: {pc?.Name}, Model: {pc?.Model}, ToolGUIDs: {Configuration.ToolGUIDs?.Count ?? 0}, Skills: {skillConfigs.Count}, SystemPrompt length: {systemPrompt?.Length ?? 0}");
            }

            // No tools and no skills — plain chat, no tool loop.
            if (toolConfigs.Count == 0 && skillConfigs.Count == 0)
                return _provider.HandleChat(Messages);

            // Tools currently callable, keyed by name. Starts with the agent's base tools and grows as
            // skills are loaded. Pre-activate the tools of any skill already loaded earlier in this
            // conversation (e.g. a reloaded session) so unlocked tools survive across turns.
            var activeToolConfigs = new Dictionary<string, AIToolConfiguration>();
            foreach (var t in toolConfigs) activeToolConfigs[t.Name] = t;

            var loadedSkills = new HashSet<string>();
            foreach (var s in skillConfigs)
            {
                if (IsSkillAlreadyLoaded(s.Name) && loadedSkills.Add(s.Name))
                    foreach (var t in s.GetToolConfigurations()) activeToolConfigs[t.Name] = t;
            }

            List<AITool> BuildTools()
            {
                var list = activeToolConfigs.Values.Select(t => t.ToAITool()).ToList();
                if (skillConfigs.Count > 0) list.Add(BuildLoadSkillTool(skillConfigs));
                return list;
            }

            var tools = BuildTools();

            for (int i = 0; i < maxIterations; i++)
            {
                if (cancelOperation?.Cancel == true) break;

                var reply = _provider.HandleChatWithTools(Messages, tools, out var toolCalls);
                if (log != null) log.LogMessage($"****** Chat Reply {i}:\r\n{reply}");

                // AI returned a text response — loop is complete
                if (toolCalls == null || toolCalls.Count == 0)
                    return reply;

                bool toolsChanged = false;

                // Execute every requested tool and append the results to the conversation
                foreach (var toolCall in toolCalls)
                {
                    if (cancelOperation?.Cancel == true) break;

                    // Built-in skill loader: return the skill instructions and unlock its tools.
                    if (toolCall.Name == LoadSkillToolName)
                    {
                        var skillName = ExtractSkillName(toolCall.Arguments);
                        var skillCfg = skillConfigs.FirstOrDefault(s => s.Name == skillName);
                        progress?.Invoke(skillCfg?.GetProgressLabel(Culture) ?? $"Loading skill '{skillName}'");
                        var skillResult = LoadSkill(toolCall, skillConfigs, loadedSkills, activeToolConfigs, ref toolsChanged);
                        if (log != null) log.LogMessage($"****** Skill Load {i}\r\nArguments:\r\n{toolCall.Arguments}\r\nReply:\r\n{skillResult}");
                        Messages.Add(new ToolChatMessage(toolCall.Id, skillResult));
                        continue;
                    }

                    toolCall.SecurityContext = SecurityContext;
                    toolCall.ExecutionLog = toolsLog;
                    toolCall.CancelOperation = cancelOperation;

                    var toolConfig = activeToolConfigs.TryGetValue(toolCall.Name, out var tc) ? tc : null;
                    progress?.Invoke(toolConfig?.GetProgressLabel(toolCall, Culture) ?? $"Running '{toolCall.Name}'");
                    // The tool is not active yet. It almost always belongs to a skill that has not been
                    // loaded: returning an empty result silently misleads the model (it reads it as "no
                    // data" and answers wrong). Return an actionable error so it loads the skill and retries.
                    var result = toolConfig != null
                        ? (toolConfig.Execute(toolCall) ?? string.Empty)
                        : $"Error: tool '{toolCall.Name}' is not available. If it belongs to a skill, call '{LoadSkillToolName}' to load that skill first, then retry.";
                    if (log != null) log.LogMessage($"****** Tool Call {i} {toolCall.Name}\r\nArguments:\r\n{toolCall.Arguments}\r\nReply:\r\n{result}");
                    Messages.Add(new ToolChatMessage(toolCall.Id, result));
                }
                if (cancelOperation?.Cancel == true) break;

                // A skill unlocked new tools — rebuild the tool list for the next iteration.
                if (toolsChanged) tools = BuildTools();
                // Continue: send again with the tool results now in the history
            }

            // Safety net: max iterations reached — ask for a plain reply without tools
            if (cancelOperation?.Cancel == true)
            {
                Messages.RemoveRange(messageCountBefore, Messages.Count - messageCountBefore);
                return "";
            }
            return _provider.HandleChat(Messages);
        }

        /// <summary>
        /// Builds the synthetic <c>load_skill</c> tool. Its description embeds the live catalog of the
        /// agent's skills (name: description) and its single <c>skill_name</c> parameter is constrained to
        /// the available skill names.
        /// </summary>
        private static AITool BuildLoadSkillTool(List<AISkillConfiguration> skillConfigs)
        {
            var catalog = string.Join("\n", skillConfigs.Select(s => $"- {s.Name}: {s.Description}"));
            var description =
                "Load the detailed instructions for a skill before performing related work. " +
                "Call this as soon as a user request matches one of the skills below; the full instructions " +
                "(and any tools the skill needs) become available after loading. Available skills:\n" + catalog;

            var schema = JsonConvert.SerializeObject(new
            {
                type = "object",
                properties = new
                {
                    skill_name = new
                    {
                        type = "string",
                        description = "Name of the skill to load.",
                        @enum = skillConfigs.Select(s => s.Name).ToList()
                    }
                },
                required = new[] { "skill_name" }
            });

            return new AITool { Name = LoadSkillToolName, Description = description, ParametersSchema = schema };
        }

        /// <summary>
        /// Resolves the skill requested by a <c>load_skill</c> tool call, returns its instructions as the
        /// tool result, and merges the skill's tools into <paramref name="activeToolConfigs"/> the first
        /// time it is loaded. Sets <paramref name="toolsChanged"/> to <c>true</c> when new tools were added.
        /// </summary>
        private string LoadSkill(AIToolCall toolCall, List<AISkillConfiguration> skillConfigs, HashSet<string> loadedSkills, Dictionary<string, AIToolConfiguration> activeToolConfigs, ref bool toolsChanged)
        {
            var skillName = ExtractSkillName(toolCall.Arguments);
            var skill = skillConfigs.FirstOrDefault(s => s.Name == skillName);
            if (skill == null)
                return $"Unknown skill '{skillName}'. Available skills: {string.Join(", ", skillConfigs.Select(s => s.Name))}.";

            // Unlock the skill's tools the first time it is loaded.
            if (loadedSkills.Add(skill.Name))
            {
                foreach (var t in skill.GetToolConfigurations())
                {
                    if (!activeToolConfigs.ContainsKey(t.Name))
                    {
                        activeToolConfigs[t.Name] = t;
                        toolsChanged = true;
                    }
                }
            }

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

        /// <summary>
        /// Extracts the <c>skill_name</c> argument from a <c>load_skill</c> tool call's JSON arguments.
        /// Returns <c>null</c> when it cannot be parsed.
        /// </summary>
        private static string ExtractSkillName(string arguments)
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
        /// Returns <c>true</c> when a <c>load_skill</c> call for <paramref name="skillName"/> already appears
        /// in the conversation history, so its tools can be pre-activated on a resumed session.
        /// </summary>
        private bool IsSkillAlreadyLoaded(string skillName)
        {
            foreach (var m in Messages)
            {
                // AssistantToolCallsChatMessage (raw-HTTP providers) extends AssistantChatMessage — check first.
                if (m is AssistantToolCallsChatMessage atcm)
                {
                    if (atcm.AIToolCalls.Any(tc => tc.Name == LoadSkillToolName && ExtractSkillName(tc.Arguments) == skillName))
                        return true;
                }
                else if (m is AssistantChatMessage asst && asst.ToolCalls != null)
                {
                    if (asst.ToolCalls.Any(tc => tc.FunctionName == LoadSkillToolName && ExtractSkillName(tc.FunctionArguments?.ToString()) == skillName))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Same as <see cref="Chat"/> but returns the reply sanitized as safe HTML.
        /// </summary>
        public string ChatHtml(string userMessage, ICancelOperation cancelOperation = null, ReportExecutionLog log = null, ReportExecutionLog toolsLog = null, int maxIterations = 10)
        {
            var raw = Chat(userMessage, cancelOperation, log, toolsLog, maxIterations);
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedAttributes.Add("class");
            return sanitizer.Sanitize(raw);
        }

        /// <summary>
        /// Sends a plain user message to the AI without any tools and returns
        /// the reply. Useful when you want a simple single-turn exchange that
        /// bypasses the agentic tool loop.
        /// </summary>
        public string ChatSimple(string userMessage)
        {
            Messages.Add(new UserChatMessage(userMessage));
            return _provider.HandleChat(Messages);
        }

        /// <summary>
        /// Asks the AI to summarise the current conversation as a short, friendly
        /// title (a few words). Uses the same provider/model as the conversation,
        /// without tools, and works on a copy of <see cref="Messages"/> so the
        /// persisted history is never modified. Returns <c>null</c> when no
        /// conversation exists yet or on any failure, so callers can fall back to
        /// their default naming.
        /// </summary>
        public string GenerateTitle()
        {
            try
            {
                // Need at least one real exchange to summarise.
                if (!Messages.OfType<UserChatMessage>().Any()) return null;

                var temp = new List<ChatMessage>(Messages)
                {
                    new UserChatMessage(
                        "Summarize this conversation as a short title of 3 to 6 words. " +
                        "Reply with the title only: plain text, no quotes, no trailing punctuation.")
                };

                var title = _provider.HandleChat(temp);
                if (string.IsNullOrWhiteSpace(title)) return null;

                // Keep the first line only and strip surrounding quotes.
                title = title.Split('\n')[0].Trim().Trim('"', '\'', '`').Trim();
                if (title.Length > 60) title = title.Substring(0, 60).Trim();

                return string.IsNullOrWhiteSpace(title) ? null : title;
            }
            catch
            {
                return null;
            }
        }

        // ----------------------------------------------------------------
        //  Conversation management
        // ----------------------------------------------------------------

        /// <summary>
        /// Replaces the leading system message in <see cref="Messages"/> with
        /// <paramref name="prompt"/>.  If <paramref name="prompt"/> is null or
        /// whitespace the existing system message (if any) is simply removed.
        /// Call this before <see cref="Chat"/> to inject a parameter-driven system
        /// prompt that overrides the one from <see cref="AIAgentConfiguration.EffectiveSystemPrompt"/>.
        /// </summary>
        public void OverrideSystemPrompt(string prompt)
        {
            if (Messages.Count > 0 && Messages[0] is SystemChatMessage)
                Messages.RemoveAt(0);

            if (!string.IsNullOrWhiteSpace(prompt))
                Messages.Insert(0, new SystemChatMessage(prompt));
        }

        /// <summary>
        /// Rewinds the conversation to just before the user turn at
        /// <paramref name="userMessageIndex"/> (0-based, counting only user messages).
        /// Removes that user message and every message that followed it — including the
        /// assistant replies, tool calls and tool results — so the conversation returns to
        /// the state it had before that message was sent.
        /// Returns the text of the removed user message (so the caller can re-populate the
        /// input box), or <c>null</c> when the index is out of range.
        /// </summary>
        public string RewindToUserMessage(int userMessageIndex)
        {
            if (userMessageIndex < 0) return null;

            int count = 0;
            for (int i = 0; i < Messages.Count; i++)
            {
                if (Messages[i] is UserChatMessage usr)
                {
                    if (count == userMessageIndex)
                    {
                        var text = usr.Content.Count > 0 ? usr.Content[0].Text : string.Empty;
                        Messages.RemoveRange(i, Messages.Count - i);
                        return text;
                    }
                    count++;
                }
            }
            return null;
        }

        /// <summary>
        /// Resets the conversation history, re-seeding it with the system
        /// prompt when one is configured.
        /// </summary>
        public void Clear()
        {
            Messages.Clear();
            if (!string.IsNullOrWhiteSpace(Configuration.EffectiveSystemPrompt))
                Messages.Add(new SystemChatMessage(Configuration.EffectiveSystemPrompt));
        }

        /// <summary>
        /// Number of messages currently in the conversation (including the
        /// system prompt when present).
        /// </summary>
        public int MessageCount => Messages.Count;

        /// <summary>
        /// Current security user of the agent
        /// </summary>
        public SecurityUser SecurityContext = null;

        /// <summary>
        /// Two-letter ISO culture code (e.g. "fr") used to translate the progress ("thinking") labels
        /// in the caller's locale. Set it per request from the session culture before calling
        /// <see cref="Chat"/>; leave null to use the repository's current culture.
        /// </summary>
        public string Culture = null;

        // ----------------------------------------------------------------
        //  Serialisation  (JSON format)
        // ----------------------------------------------------------------

        /// <summary>
        /// Serialises the current conversation to a <see cref="ChatSessionFile"/> object.
        /// </summary>
        /// <param name="infos">
        /// Key/value pairs that describe the session (Type, Name, Description, Instance …).
        /// Pass <c>null</c> to produce an empty Infos list.
        /// </param>
        public ChatSessionFile ToSessionFile(List<StringPair> infos = null)
        {
            var session = new ChatSessionFile
            {
                Infos = infos ?? new List<StringPair>()
            };

            foreach (var msg in Messages)
            {
                // AssistantToolCallsChatMessage extends AssistantChatMessage and must be
                // handled first (before the base type) via an explicit is-check.
                if (msg is AssistantToolCallsChatMessage atcm)
                {
                    var toolCalls = atcm.AIToolCalls
                        .Select(tc => new ChatSessionToolCall { Id = tc.Id, FunctionName = tc.Name, Arguments = tc.Arguments })
                        .ToList();
                    session.Messages.Add(new ChatSessionMessage
                    {
                        Type      = "AssistantChatMessage",
                        Content   = atcm.TextContent ?? string.Empty,
                        ToolCalls = toolCalls
                    });
                    continue;
                }

                string type;
                string content;
                string toolCallId = null;
                List<ChatSessionToolCall> sessionToolCalls = null;

                switch (msg)
                {
                    case SystemChatMessage sys:
                        type    = "SystemChatMessage";
                        content = sys.Content.Count > 0 ? sys.Content[0].Text : string.Empty;
                        break;
                    case UserChatMessage usr:
                        type    = "UserChatMessage";
                        content = usr.Content.Count > 0 ? usr.Content[0].Text : string.Empty;
                        break;
                    case AssistantChatMessage asst:
                        type    = "AssistantChatMessage";
                        content = asst.Content.Count > 0 ? asst.Content[0].Text : string.Empty;
                        // Azure SDK-native path: ToolCalls is populated on the base class
                        if (asst.ToolCalls != null && asst.ToolCalls.Count > 0)
                        {
                            sessionToolCalls = asst.ToolCalls
                                .Select(tc => new ChatSessionToolCall
                                {
                                    Id           = tc.Id,
                                    FunctionName = tc.FunctionName,
                                    Arguments    = tc.FunctionArguments.ToString()
                                })
                                .ToList();
                        }
                        break;
                    case ToolChatMessage tool:
                        type       = "ToolChatMessage";
                        content    = tool.Content.Count > 0 ? tool.Content[0].Text : string.Empty;
                        toolCallId = tool.ToolCallId;
                        break;
                    default:
                        continue; // skip unknown message types
                }

                // Save the entry when it has text, tool calls, or is a tool result.
                // Tool-call agent messages may have empty text but must still be saved
                // so that the paired ToolChatMessage entries are not orphaned on reload.
                bool hasToolCalls = sessionToolCalls != null && sessionToolCalls.Count > 0;
                if (!string.IsNullOrEmpty(content) || hasToolCalls || type == "ToolChatMessage")
                {
                    session.Messages.Add(new ChatSessionMessage
                    {
                        Type       = type,
                        Content    = content,
                        ToolCallId = toolCallId,
                        ToolCalls  = sessionToolCalls
                    });
                }
            }

            return session;
        }

        /// <summary>
        /// Saves the current conversation to a JSON file.
        /// </summary>
        /// <param name="path">Full file-system path (including file name and extension).</param>
        /// <param name="infos">Session metadata to embed in the file.</param>
        public void SaveToFile(string path, List<StringPair> infos = null)
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var session = ToSessionFile(infos);
            var json = JsonConvert.SerializeObject(session, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// Restores the conversation history from a <see cref="ChatSessionFile"/>.
        /// The current <see cref="Messages"/> list is cleared first.
        /// </summary>
        public void LoadFromSessionFile(ChatSessionFile session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            Messages.Clear();
            foreach (var entry in session.Messages)
            {
                ChatMessage msg;
                switch (entry.Type)
                {
                    case "SystemChatMessage":
                        msg = new SystemChatMessage(entry.Content ?? string.Empty);
                        break;
                    case "UserChatMessage":
                        msg = new UserChatMessage(entry.Content ?? string.Empty);
                        break;
                    case "AssistantChatMessage":
                        if (entry.ToolCalls != null && entry.ToolCalls.Count > 0)
                        {
                            // Reconstruct using SDK-native ChatToolCall objects so that both
                            // the Azure SDK provider (CompleteChat) and the OpenAI custom-HTTP
                            // provider (SerializeMessage) can serialise tool_calls correctly.
                            var sdkToolCalls = entry.ToolCalls
                                .Select(tc => ChatToolCall.CreateFunctionToolCall(
                                    tc.Id, tc.FunctionName, BinaryData.FromString(tc.Arguments ?? "{}")))
                                .ToList();
                            msg = new AssistantChatMessage(sdkToolCalls);
                        }
                        else
                        {
                            msg = new AssistantChatMessage(entry.Content ?? string.Empty);
                        }
                        break;
                    case "ToolChatMessage":
                        // Only valid when paired with a preceding AssistantToolCallsChatMessage.
                        msg = !string.IsNullOrEmpty(entry.ToolCallId)
                            ? new ToolChatMessage(entry.ToolCallId, entry.Content ?? string.Empty)
                            : null;
                        break;
                    default:
                        msg = null;
                        break;
                }
                if (msg != null) Messages.Add(msg);
            }
        }

        /// <summary>
        /// Loads a conversation from a JSON file and returns both the agent
        /// and the embedded session metadata.
        /// The agent is built from the configuration whose Name matches the
        /// <c>Name</c> info key; falls back to the default configuration when not found.
        /// </summary>
        /// <param name="path">Full file-system path to the file.</param>
        /// <returns>A tuple of the restored agent and the raw session file.</returns>
        public static (AIAgent agent, ChatSessionFile session) LoadFromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Chat session file not found.", path);

            var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            var session = JsonConvert.DeserializeObject<ChatSessionFile>(json)
                          ?? throw new InvalidOperationException("Invalid json file: could not deserialise.");

            // Resolve agent configuration by name (fall back to default)
            var agentName = session.GetInfo("Name");
            AIAgent agent;
            try { agent = new AIAgent(agentName); }
            catch { agent = new AIAgent(); }

            // Replace the seeded system prompt with the saved history
            agent.LoadFromSessionFile(session);
            return (agent, session);
        }
    }
}
