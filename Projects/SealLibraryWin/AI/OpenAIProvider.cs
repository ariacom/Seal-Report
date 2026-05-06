using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Chat;

namespace Seal.AI
{
    public class OpenAIProvider : AIProvider
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = System.Threading.Timeout.InfiniteTimeSpan
        };

        /// <summary>
        /// Initializes a new OpenAI provider.
        /// </summary>
        public OpenAIProvider() { }

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
            var body = new Dictionary<string, object>
            {
                ["model"] = _model,
                ["messages"] = messages.Select(m => new
                {
                    role = GetRole(m),
                    content = m.Content[0].Text
                }).ToList<object>(),
                ["temperature"] = _temperature,
                ["top_p"] = _topP
            };
            if (_maxTokens > 0)
                body["max_completion_tokens"] = _maxTokens;

            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = _httpClient.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();

            var responseBody = response.Content.ReadAsStringAsync().Result;
            var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(responseBody);

            var result = responseObject?.Choices?[0]?.Message?.Content
                ?? throw new Exception("Failed to get response from OpenAI");

            messages.Add(new AssistantChatMessage(result));
            return result;
        }

        private static string GetRole(ChatMessage message) => message switch
        {
            SystemChatMessage => "system",
            UserChatMessage => "user",
            AssistantChatMessage => "assistant",
            _ => "user"
        };

        private class OpenAIResponse
        {
            [JsonPropertyName("choices")]
            public Choice[] Choices { get; set; }

            public class Choice
            {
                [JsonPropertyName("message")]
                public Message Message { get; set; }
            }

            public class Message
            {
                [JsonPropertyName("content")]
                public string Content { get; set; }
            }
        }
    }
}
