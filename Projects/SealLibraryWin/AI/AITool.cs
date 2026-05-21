using System.Collections.Generic;
using OpenAI.Chat;

namespace Seal.AI
{
    /// <summary>
    /// Describes a tool (function) that an AI provider can invoke.
    /// </summary>
    public class AITool
    {
        /// <summary>Function name – must be unique within a request.</summary>
        public string Name { get; set; }

        /// <summary>Human-readable description of what the tool does.</summary>
        public string Description { get; set; }

        /// <summary>
        /// JSON Schema string describing the function's parameters.
        /// Example: <c>{"type":"object","properties":{"city":{"type":"string"}},"required":["city"]}</c>
        /// Pass <c>null</c> or an empty string for tools that take no parameters.
        /// </summary>
        public string ParametersSchema { get; set; }
    }

    /// <summary>
    /// Represents a single tool-call request returned by the AI.
    /// </summary>
    public class AIToolCall
    {
        /// <summary>
        /// Provider-issued call identifier. Pass it back via
        /// <see cref="ToolChatMessage"/> when submitting the tool result.
        /// </summary>
        public string Id { get; set; }

        /// <summary>Name of the tool the AI wants to invoke.</summary>
        public string Name { get; set; }

        /// <summary>JSON string containing the arguments the AI supplied.</summary>
        public string Arguments { get; set; }

    }

    /// <summary>
    /// Internal <see cref="AssistantChatMessage"/> carrier used by raw-HTTP providers
    /// (OpenAI, Anthropic, Ollama) to store tool-call data in the conversation history.
    /// The Azure provider uses the SDK-native <c>new AssistantChatMessage(ChatCompletion)</c>
    /// constructor instead.
    /// </summary>
    internal sealed class AssistantToolCallsChatMessage : AssistantChatMessage
    {
        /// <summary>The tool calls the AI requested in this turn.</summary>
        public IReadOnlyList<AIToolCall> AIToolCalls { get; }

        /// <summary>Optional interstitial text the AI produced alongside the tool calls.</summary>
        public string TextContent { get; }

        public AssistantToolCallsChatMessage(IReadOnlyList<AIToolCall> toolCalls, string textContent = null)
            : base(string.Empty)
        {
            AIToolCalls = toolCalls;
            TextContent = textContent;
        }
    }
}
