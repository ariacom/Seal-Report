using System;
using System.Collections.Generic;
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
    }
}
