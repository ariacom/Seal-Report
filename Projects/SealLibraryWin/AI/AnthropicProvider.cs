using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Chat;

namespace Seal.AI
{
    public class AnthropicProvider : AIProvider
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = System.Threading.Timeout.InfiniteTimeSpan
        };

        /// <summary>
        /// Initializes a new Anthropic provider.
        /// </summary>
        public AnthropicProvider() { }

        /// <inheritdoc/>
        protected override void Initialize(string endpoint, string key, string model, float temperature, int maxTokens, float topP)
        {
            _apiKey = key;
            _endpoint = endpoint;
            _model = model;
            _temperature = temperature;
            _maxTokens = maxTokens;
            _topP = topP;
        }

        /// <inheritdoc/>
        public override string HandleChat(List<ChatMessage> messages)
        {
            var systemMessage = messages
                .OfType<SystemChatMessage>()
                .FirstOrDefault()?.Content[0].Text ?? string.Empty;

            var chatMessages = messages
                .Where(m => m is not SystemChatMessage)
                .Select(m => new
                {
                    role = GetRole(m),
                    content = m.Content[0].Text
                });

            var body = new Dictionary<string, object>
            {
                ["model"] = _model,
                ["messages"] = chatMessages,
                ["system"] = systemMessage,
                ["max_tokens"] = _maxTokens > 0 ? _maxTokens : 4096,  // required by Anthropic
                ["temperature"] = _temperature,
                ["top_p"] = _topP
            };

            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var response = _httpClient.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();

            var responseBody = response.Content.ReadAsStringAsync().Result;
            var responseObject = JsonSerializer.Deserialize<AnthropicResponse>(responseBody);

            var result = responseObject?.Content?[0]?.Text
                ?? throw new Exception("Failed to get response from Anthropic");

            messages.Add(new AssistantChatMessage(result));
            return result;
        }

        private static string GetRole(ChatMessage message) => message switch
        {
            UserChatMessage => "user",
            AssistantChatMessage => "assistant",
            _ => "user"
        };

        private class AnthropicResponse
        {
            [JsonPropertyName("content")]
            public ContentBlock[] Content { get; set; }

            public class ContentBlock
            {
                [JsonPropertyName("type")]
                public string Type { get; set; }

                [JsonPropertyName("text")]
                public string Text { get; set; }
            }
        }
    }
}
