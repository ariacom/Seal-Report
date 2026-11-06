using System;
using System.Collections.Generic;
using System.Linq;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using OpenAI.Chat;

namespace Seal.AI
{
    /// <summary>
    /// AI provider implementation for Azure OpenAI, using the Azure OpenAI SDK client
    /// with the model name as the deployment.
    /// </summary>
    public class AzureProvider : AIProvider
    {
        private AzureOpenAIClient _azureClient;
        private ChatClient _chatClient;

        /// <inheritdoc/>
        protected override void Initialize(string endpoint, string key, string model, float temperature, int maxTokens, float topP)
        {
            _apiKey = key;
            _endpoint = endpoint;
            _model = model;
            _temperature = temperature;
            _maxTokens = maxTokens;
            _topP = topP;

            var clientOptions = new AzureOpenAIClientOptions();
            clientOptions.AddPolicy(new ReasoningNonePolicy(() => NoReasoning), System.ClientModel.Primitives.PipelinePosition.PerCall);
            _azureClient = new AzureOpenAIClient(
                new Uri(endpoint),
                new System.ClientModel.ApiKeyCredential(key),
                clientOptions
            );

            _chatClient = _azureClient.GetChatClient(model);
        }

        private ChatCompletionOptions Options
        {
            get
            {
                var options = new ChatCompletionOptions();
                if (SendTemperature)
                    options.Temperature = _temperature;
                if (_maxTokens > 0)
                    options.MaxOutputTokenCount = _maxTokens;
                if (_topP < 1.0f)
                    options.TopP = _topP;
                return options;
            }
        }

        /// <summary>
        /// Sends the request; if the model refuses the temperature (e.g. GPT-6 accepts only its default value),
        /// sends it again without temperature, and the next requests to the model omit it.
        /// The error message returned by Azure is added to the exception.
        /// </summary>
        private ChatCompletion Complete(List<ChatMessage> messages, ChatCompletionOptions options)
        {
            try
            {
                return _chatClient.CompleteChat(messages, options);
            }
            catch (System.ClientModel.ClientResultException ex)
            {
                var body = ex.GetRawResponse()?.Content?.ToString();
                if (options.Temperature != null && HandleTemperatureRefused(ex.Status, body))
                {
                    options.Temperature = null;
                    return Complete(messages, options);
                }
                if (HandleReasoningRefused(ex.Status, body))
                    return Complete(messages, options); //sent with reasoning_effort 'none' by the ReasoningNonePolicy
                var message = GetErrorMessage(body);
                if (string.IsNullOrEmpty(message)) throw;
                throw new Exception($"Azure OpenAI: HTTP {ex.Status} {message}", ex);
            }
        }

        /// <inheritdoc/>
        public override string HandleChat(List<ChatMessage> messages)
        {
            ChatCompletion completion = Complete(messages, Options);
            SetLastUsage(completion.Usage?.InputTokenCount ?? 0, completion.Usage?.OutputTokenCount ?? 0, completion.Usage?.InputTokenDetails?.CachedTokenCount ?? 0);

            if (completion.FinishReason == ChatFinishReason.Stop)
            {
                var result = completion.Content[0].Text;
                messages.Add(new AssistantChatMessage(result));
                return result;
            }

            throw IncompleteReplyException(completion);
        }

        /// <inheritdoc/>
        public override string HandleChatWithTools(List<ChatMessage> messages, IList<AITool> tools, out IList<AIToolCall> toolCalls)
        {
            toolCalls = new List<AIToolCall>();

            var options = Options;
            foreach (var tool in tools)
            {
                var schema = BinaryData.FromString(
                    string.IsNullOrEmpty(tool.ParametersSchema) ? "{}" : tool.ParametersSchema);
                options.Tools.Add(ChatTool.CreateFunctionTool(tool.Name, tool.Description, schema));
            }

            ChatCompletion completion = Complete(messages, options);
            SetLastUsage(completion.Usage?.InputTokenCount ?? 0, completion.Usage?.OutputTokenCount ?? 0, completion.Usage?.InputTokenDetails?.CachedTokenCount ?? 0);

            if (completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                toolCalls = completion.ToolCalls
                    .Select(tc => new AIToolCall
                    {
                        Id = tc.Id,
                        Name = tc.FunctionName,
                        Arguments = tc.FunctionArguments.ToString()
                    })
                    .ToList();
                // SDK-native constructor preserves ToolCalls for the next turn's serialization.
                messages.Add(new AssistantChatMessage(completion));
                return string.Empty;
            }

            if (completion.FinishReason == ChatFinishReason.Stop)
            {
                var result = completion.Content[0].Text;
                messages.Add(new AssistantChatMessage(result));
                return result;
            }

            throw IncompleteReplyException(completion);
        }

        /// <summary>
        /// Exception for a completion that ended neither with a reply nor with tool calls
        /// (content filter, max output tokens...), instead of silently returning an empty reply.
        /// </summary>
        private static Exception IncompleteReplyException(ChatCompletion completion)
        {
            if (completion.FinishReason == ChatFinishReason.ContentFilter)
                return new Exception($"Azure OpenAI: the reply was blocked by the Azure content filter{FilteredCategories(completion)}. Rephrase the request or review the content filter settings of the deployment.");
            if (completion.FinishReason == ChatFinishReason.Length)
                return new Exception("Azure OpenAI: the reply was truncated because the maximum number of output tokens was reached. Increase the Max Tokens of the provider.");
            return new Exception($"Azure OpenAI: the model returned no reply (finish reason: {completion.FinishReason}).");
        }

        /// <summary>
        /// Adds "reasoning_effort": "none" to the request body when the model accepts the function tools only without reasoning
        /// (e.g. GPT-6 on the chat completions API): the version of the SDK has no option for it.
        /// </summary>
        private class ReasoningNonePolicy : System.ClientModel.Primitives.PipelinePolicy
        {
            readonly Func<bool> _apply;

            public ReasoningNonePolicy(Func<bool> apply) { _apply = apply; }

            public override void Process(System.ClientModel.Primitives.PipelineMessage message, IReadOnlyList<System.ClientModel.Primitives.PipelinePolicy> pipeline, int currentIndex)
            {
                Patch(message);
                ProcessNext(message, pipeline, currentIndex);
            }

            public override System.Threading.Tasks.ValueTask ProcessAsync(System.ClientModel.Primitives.PipelineMessage message, IReadOnlyList<System.ClientModel.Primitives.PipelinePolicy> pipeline, int currentIndex)
            {
                Patch(message);
                return ProcessNextAsync(message, pipeline, currentIndex);
            }

            void Patch(System.ClientModel.Primitives.PipelineMessage message)
            {
                if (!_apply() || message.Request.Content == null) return;
                using var stream = new System.IO.MemoryStream();
                message.Request.Content.WriteTo(stream);
                if (System.Text.Json.Nodes.JsonNode.Parse(stream.ToArray()) is not System.Text.Json.Nodes.JsonObject body || body.ContainsKey("reasoning_effort")) return;
                body["reasoning_effort"] = "none";
                message.Request.Content = System.ClientModel.BinaryContent.Create(BinaryData.FromString(body.ToJsonString()));
            }
        }

        /// <summary>
        /// Returns the content filter categories flagged as filtered for the completion (e.g. " (filtered: Violence severity Medium)"), or an empty string.
        /// </summary>
        private static string FilteredCategories(ChatCompletion completion)
        {
            try
            {
#pragma warning disable AOAI001
                var result = completion.GetResponseContentFilterResult();
#pragma warning restore AOAI001
                if (result == null) return string.Empty;
                // Each category result exposes a Filtered flag (and a Severity for the harm categories)
                var filtered = result.GetType().GetProperties()
                    .Select(p => (p.Name, Value: p.GetIndexParameters().Length == 0 ? p.GetValue(result) : null))
                    .Where(c => c.Value != null && c.Value.GetType().GetProperty("Filtered")?.GetValue(c.Value) is true)
                    .Select(c => c.Name + (c.Value.GetType().GetProperty("Severity")?.GetValue(c.Value) is object severity ? $" severity {severity}" : ""))
                    .ToList();
                return filtered.Count > 0 ? $" (filtered: {string.Join(", ", filtered)})" : " (no category reported as filtered)";
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
