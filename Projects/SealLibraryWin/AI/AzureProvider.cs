using System;
using System.Collections.Generic;
using System.Linq;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace Seal.AI
{
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

            _azureClient = new AzureOpenAIClient(
                new Uri(endpoint),
                new System.ClientModel.ApiKeyCredential(key)
            );

            _chatClient = _azureClient.GetChatClient(model);
        }

        private ChatCompletionOptions Options
        {
            get
            {
                var options = new ChatCompletionOptions
                {
                    Temperature = _temperature
                };
                if (_maxTokens > 0)
                    options.MaxOutputTokenCount = _maxTokens;
                if (_topP < 1.0f)
                    options.TopP = _topP;
                return options;
            }
        }

        /// <inheritdoc/>
        public override string HandleChat(List<ChatMessage> messages)
        {
            ChatCompletion completion = _chatClient.CompleteChat(messages, Options);

            if (completion.FinishReason == ChatFinishReason.Stop)
            {
                var result = completion.Content[0].Text;
                messages.Add(new AssistantChatMessage(result));
                return result;
            }

            return string.Empty;
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

            ChatCompletion completion = _chatClient.CompleteChat(messages, options);

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

            return string.Empty;
        }
    }
}
