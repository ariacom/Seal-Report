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
    /// Stateful AI assistant that owns a conversation history and delegates to
    /// an <see cref="IAIProvider"/> for all model calls.
    /// Initialise once per session or per conversation; call <see cref="Chat"/>
    /// for each user turn.
    /// </summary>
    public class AIAssistant
    {
        /// <summary>
        /// The configuration used to build this assistant.
        /// </summary>
        public AIAssistantConfiguration Configuration { get; }

        private readonly IAIProvider _provider;

        /// <summary>
        /// Full conversation history, including the system prompt (if any),
        /// all user turns, and all assistant replies.
        /// Exposed so callers can inspect or serialise it.
        /// </summary>
        public List<ChatMessage> Messages { get; } = new List<ChatMessage>();

        // ----------------------------------------------------------------
        //  Construction
        // ----------------------------------------------------------------

        /// <summary>
        /// Creates an <see cref="AIAssistant"/> from a named
        /// <see cref="AIAssistantConfiguration"/>.
        /// Injects the configuration's <see cref="AIAssistantConfiguration.EffectiveSystemPrompt"/>
        /// as the first message in the conversation when it is not empty.
        /// </summary>
        /// <param name="assistantName">
        /// Name of the <see cref="AIAssistantConfiguration"/> to look up.
        /// Pass <c>null</c> or empty to use the default configuration
        /// (the one with <see cref="AIAssistantConfiguration.IsDefault"/> set).
        /// </param>
        public AIAssistant(string assistantName = null)
        {
            // Resolve configuration
            if (string.IsNullOrEmpty(assistantName))
            {
                Configuration = Repository.Instance.AIConfiguration.AIAssistants
                    .Find(a => a.IsDefault && a.IsEnabled)
                    ?? throw new System.Exception(
                        "No default AI assistant configuration found. " +
                        "Set IsDefault = true on one enabled assistant.");
            }
            else
            {
                Configuration = Repository.Instance.AIConfiguration.AIAssistants
                    .Find(a => a.Name == assistantName && a.IsEnabled)
                    ?? throw new System.Exception(
                        $"AI assistant configuration '{assistantName}' not found or is disabled.");
            }

            _provider = Configuration.GetProvider()
                ?? throw new System.Exception(
                    $"AI assistant '{Configuration.Name}' has no valid provider configuration.");

            // Seed conversation with the system prompt if one is defined
            if (!string.IsNullOrWhiteSpace(Configuration.EffectiveSystemPrompt))
                Messages.Add(new SystemChatMessage(Configuration.EffectiveSystemPrompt));
        }

        /// <summary>
        /// Creates an <see cref="AIAssistant"/> directly from an existing
        /// <see cref="AIAssistantConfiguration"/> instance.
        /// </summary>
        public AIAssistant(AIAssistantConfiguration configuration)
        {
            Configuration = configuration
                ?? throw new System.ArgumentNullException(nameof(configuration));

            _provider = Configuration.GetProvider()
                ?? throw new System.Exception(
                    $"AI assistant '{Configuration.Name}' has no valid provider configuration.");

            if (!string.IsNullOrWhiteSpace(Configuration.EffectiveSystemPrompt))
                Messages.Add(new SystemChatMessage(Configuration.EffectiveSystemPrompt));
        }

        // ----------------------------------------------------------------
        //  Chat
        // ----------------------------------------------------------------

        /// <summary>
        /// Appends a user message to the conversation history, calls the AI using
        /// the tools scoped to this assistant (via <see cref="AIAssistantConfiguration.GetToolConfigurations"/>),
        /// executes any tool calls the AI requests, and repeats until a plain reply is
        /// produced or <paramref name="maxIterations"/> is reached.
        /// Falls back to a plain <see cref="IAIProvider.HandleChat"/> call when no tools are configured.
        /// </summary>
        public string Chat(string userMessage, ICancelOperation cancelOperation = null, ReportExecutionLog log = null, int maxIterations = 10)
        {
            var messageCountBefore = Messages.Count;
            Messages.Add(new UserChatMessage(userMessage));

            if (log != null)
            {
                var pc = Configuration.GetProviderConfiguration();
                var systemPrompt = Messages.OfType<SystemChatMessage>().FirstOrDefault()?.Content[0].Text;
                log.LogMessage($"[AIAssistant] Provider: {pc?.Name}, Model: {pc?.Model}, ToolGUIDs: {Configuration.ToolGUIDs?.Count ?? 0}, SystemPrompt length: {systemPrompt?.Length ?? 0}");
            }

            var toolConfigs = Configuration.GetToolConfigurations();
            if (toolConfigs.Count == 0)
                return _provider.HandleChat(Messages);

            var tools = toolConfigs.Select(t => t.ToAITool()).ToList();

            for (int i = 0; i < maxIterations; i++)
            {
                if (cancelOperation?.Cancel == true) break;

                var reply = _provider.HandleChatWithTools(Messages, tools, out var toolCalls);
                if (log != null) log.LogMessage($"****** Chat Reply {i}:\r\n{reply}");

                // AI returned a text response — loop is complete
                if (toolCalls == null || toolCalls.Count == 0)
                    return reply;

                // Execute every requested tool and append the results to the conversation
                foreach (var toolCall in toolCalls)
                {
                    if (cancelOperation?.Cancel == true) break;

                    toolCall.SecurityContext = SecurityContext;
                    toolCall.CancelOperation = cancelOperation;

                    var toolConfig = toolConfigs.FirstOrDefault(t => t.IsEnabled && t.Name == toolCall.Name);
                    var result = toolConfig?.Execute(toolCall) ?? string.Empty;
                    if (log != null) log.LogMessage($"****** Tool Call {i} {toolCall.Name}\r\nArguments:\r\n{toolCall.Arguments}\r\nReply:\r\n{result}");
                    Messages.Add(new ToolChatMessage(toolCall.Id, result));
                }
                if (cancelOperation?.Cancel == true) break;
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
        /// Same as <see cref="Chat"/> but returns the reply sanitized as safe HTML.
        /// </summary>
        public string ChatHtml(string userMessage, ICancelOperation cancelOperation = null, ReportExecutionLog log = null, int maxIterations = 10)
        {
            var raw = Chat(userMessage, cancelOperation, log, maxIterations);
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

        // ----------------------------------------------------------------
        //  Conversation management
        // ----------------------------------------------------------------

        /// <summary>
        /// Replaces the leading system message in <see cref="Messages"/> with
        /// <paramref name="prompt"/>.  If <paramref name="prompt"/> is null or
        /// whitespace the existing system message (if any) is simply removed.
        /// Call this before <see cref="Chat"/> to inject a parameter-driven system
        /// prompt that overrides the one from <see cref="AIAssistantConfiguration.EffectiveSystemPrompt"/>.
        /// </summary>
        public void OverrideSystemPrompt(string prompt)
        {
            if (Messages.Count > 0 && Messages[0] is SystemChatMessage)
                Messages.RemoveAt(0);

            if (!string.IsNullOrWhiteSpace(prompt))
                Messages.Insert(0, new SystemChatMessage(prompt));
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
        /// Current security user of the assistant
        /// </summary>
        public SecurityUser SecurityContext = null;

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
                // Tool-call assistant messages may have empty text but must still be saved
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
                            // Reconstruct as AssistantToolCallsChatMessage so that the AI
                            // provider accepts the following ToolChatMessage responses.
                            var aiToolCalls = entry.ToolCalls
                                .Select(tc => new AIToolCall { Id = tc.Id, Name = tc.FunctionName, Arguments = tc.Arguments })
                                .ToList();
                            msg = new AssistantToolCallsChatMessage(aiToolCalls, entry.Content);
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
        /// Loads a conversation from a JSON file and returns both the assistant
        /// and the embedded session metadata.
        /// The assistant is built from the configuration whose Name matches the
        /// <c>Name</c> info key; falls back to the default configuration when not found.
        /// </summary>
        /// <param name="path">Full file-system path to the file.</param>
        /// <returns>A tuple of the restored assistant and the raw session file.</returns>
        public static (AIAssistant assistant, ChatSessionFile session) LoadFromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Chat session file not found.", path);

            var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            var session = JsonConvert.DeserializeObject<ChatSessionFile>(json)
                          ?? throw new InvalidOperationException("Invalid json file: could not deserialise.");

            // Resolve assistant configuration by name (fall back to default)
            var assistantName = session.GetInfo("Name");
            AIAssistant assistant;
            try { assistant = new AIAssistant(assistantName); }
            catch { assistant = new AIAssistant(); }

            // Replace the seeded system prompt with the saved history
            assistant.LoadFromSessionFile(session);
            return (assistant, session);
        }
    }
}
