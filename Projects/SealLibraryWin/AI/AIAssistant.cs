using System.Collections.Generic;
using System.Linq;
using Ganss.Xss;
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
        /// Injects the configuration's <see cref="AIAssistantConfiguration.DefaultSystemPrompt"/>
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
                Configuration = Repository.Instance.Configuration.AIAssistants
                    .Find(a => a.IsDefault && a.IsEnabled)
                    ?? throw new System.Exception(
                        "No default AI assistant configuration found. " +
                        "Set IsDefault = true on one enabled assistant.");
            }
            else
            {
                Configuration = Repository.Instance.Configuration.AIAssistants
                    .Find(a => a.Name == assistantName && a.IsEnabled)
                    ?? throw new System.Exception(
                        $"AI assistant configuration '{assistantName}' not found or is disabled.");
            }

            _provider = Configuration.GetProvider()
                ?? throw new System.Exception(
                    $"AI assistant '{Configuration.Name}' has no valid provider configuration.");

            // Seed conversation with the system prompt if one is defined
            if (!string.IsNullOrWhiteSpace(Configuration.DefaultSystemPrompt))
                Messages.Add(new SystemChatMessage(Configuration.DefaultSystemPrompt));
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

            if (!string.IsNullOrWhiteSpace(Configuration.DefaultSystemPrompt))
                Messages.Add(new SystemChatMessage(Configuration.DefaultSystemPrompt));
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
        /// prompt that overrides the one from <see cref="AIAssistantConfiguration.DefaultSystemPrompt"/>.
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
            if (!string.IsNullOrWhiteSpace(Configuration.DefaultSystemPrompt))
                Messages.Add(new SystemChatMessage(Configuration.DefaultSystemPrompt));
        }

        /// <summary>
        /// Number of messages currently in the conversation (including the
        /// system prompt when present).
        /// </summary>
        public int MessageCount => Messages.Count;
    }
}
