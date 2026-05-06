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
    public class OllamaProvider : AIProvider
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = System.Threading.Timeout.InfiniteTimeSpan
        };

        /// <summary>
        /// Initializes a new Ollama provider.
        /// </summary>
        public OllamaProvider() { }

        /// <inheritdoc/>
        protected override void Initialize(string endpoint, string key, string model, float temperature, int maxTokens, float topP)
        {
            _endpoint = endpoint.TrimEnd('/');
            _model = model;
            _temperature = temperature;
            _maxTokens = maxTokens;
            _topP = topP;
        }

        /// <inheritdoc/>
        public override string HandleChat(List<ChatMessage> messages)
        {
            var request = new
            {
                model = _model,
                messages = messages.Select(m => new
                {
                    role = GetRole(m),
                    content = m.Content[0].Text
                }).ToList(),
                stream = false,
                options = BuildOptions()
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json"
            );

            var response = _httpClient.PostAsync(_endpoint + "/api/chat", content).Result;
            response.EnsureSuccessStatusCode();

            var responseBody = response.Content.ReadAsStringAsync().Result;
            var responseObject = JsonSerializer.Deserialize<OllamaResponse>(responseBody);

            var result = responseObject?.Message?.Content
                ?? throw new Exception("Failed to get response from Ollama");

            messages.Add(new AssistantChatMessage(result));
            return result;
        }

        private Dictionary<string, object> BuildOptions()
        {
            var opts = new Dictionary<string, object>
            {
                ["temperature"] = _temperature,
                ["top_p"] = _topP
            };
            if (_maxTokens > 0)
                opts["num_predict"] = _maxTokens;
            return opts;
        }

        private static string GetRole(ChatMessage message) => message switch
        {
            SystemChatMessage => "system",
            UserChatMessage => "user",
            AssistantChatMessage => "assistant",
            _ => "user"
        };

        private class OllamaResponse
        {
            [JsonPropertyName("message")]
            public OllamaMessage Message { get; set; }

            public class OllamaMessage
            {
                [JsonPropertyName("content")]
                public string Content { get; set; }
            }
        }
    }
}
