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
                messages = messages.Select(SerializeMessage).ToList(),
                stream = false,
                options = BuildOptions()
            };

            var response = Post("/api/chat", request);
            var result = response?.Message?.Content
                ?? throw new Exception("Failed to get response from Ollama");

            messages.Add(new AssistantChatMessage(result));
            return result;
        }

        /// <inheritdoc/>
        public override string HandleChatWithTools(List<ChatMessage> messages, IList<AITool> tools, out IList<AIToolCall> toolCalls)
        {
            toolCalls = new List<AIToolCall>();

            var requestBody = new
            {
                model = _model,
                messages = messages.Select(SerializeMessage).ToList(),
                stream = false,
                options = BuildOptions(),
                tools = tools.Select(t => (object)new
                {
                    type = "function",
                    function = new
                    {
                        name = t.Name,
                        description = t.Description,
                        parameters = string.IsNullOrEmpty(t.ParametersSchema)
                            ? JsonSerializer.Deserialize<JsonElement>("{\"type\":\"object\",\"properties\":{}}")
                            : JsonSerializer.Deserialize<JsonElement>(t.ParametersSchema)
                    }
                }).ToList()
            };

            var response = Post("/api/chat", requestBody);

            if (response?.Message?.ToolCalls?.Length > 0)
            {
                var calls = response.Message.ToolCalls
                    .Select(tc => new AIToolCall
                    {
                        // Ollama may omit the id; generate one so callers can correlate results.
                        Id = tc.Id ?? Guid.NewGuid().ToString("N"),
                        Name = tc.Function?.Name,
                        // Ollama returns arguments as a JSON object (not a string); serialize back to string.
                        // Guard against a missing or undefined element.
                        Arguments = tc.Function != null && tc.Function.Arguments.ValueKind != JsonValueKind.Undefined
                            ? tc.Function.Arguments.GetRawText()
                            : "{}"
                    })
                    .ToList();

                toolCalls = calls;
                messages.Add(new AssistantToolCallsChatMessage(calls, response.Message.Content));
                return string.Empty;
            }

            var result = response?.Message?.Content
                ?? throw new Exception("Failed to get response from Ollama");

            messages.Add(new AssistantChatMessage(result));
            return result;
        }

        private OllamaResponse Post(string path, object body)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );
            var response = _httpClient.PostAsync(_endpoint + path, content).Result;
            response.EnsureSuccessStatusCode();
            return JsonSerializer.Deserialize<OllamaResponse>(
                response.Content.ReadAsStringAsync().Result);
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

        /// <summary>
        /// Serializes a <see cref="ChatMessage"/> to an anonymous object suitable for the
        /// Ollama REST API (OpenAI-compatible), handling plain messages, tool-call agent
        /// turns, and tool results.
        /// </summary>
        private static object SerializeMessage(ChatMessage m)
        {
            switch (m)
            {
                // Must be checked before AssistantChatMessage (it is a subclass).
                case AssistantToolCallsChatMessage atcm:
                    return new
                    {
                        role = "assistant",
                        content = atcm.TextContent,
                        tool_calls = atcm.AIToolCalls.Select(tc => (object)new
                        {
                            id = tc.Id,
                            type = "function",
                            function = new { name = tc.Name, arguments = tc.Arguments }
                        }).ToList()
                    };
                case SystemChatMessage:
                    return new { role = "system", content = m.Content[0].Text };
                case AssistantChatMessage:
                    return new { role = "assistant", content = m.Content[0].Text };
                case ToolChatMessage tool:
                    return new { role = "tool", tool_call_id = tool.ToolCallId, content = tool.Content[0].Text };
                default:
                    return new { role = "user", content = m.Content[0].Text };
            }
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

                [JsonPropertyName("tool_calls")]
                public OllamaToolCall[] ToolCalls { get; set; }
            }

            public class OllamaToolCall
            {
                // Ollama may omit the id field.
                [JsonPropertyName("id")]
                public string Id { get; set; }

                [JsonPropertyName("function")]
                public OllamaFunction Function { get; set; }
            }

            public class OllamaFunction
            {
                [JsonPropertyName("name")]
                public string Name { get; set; }

                // Ollama returns arguments as a JSON object, not a string.
                [JsonPropertyName("arguments")]
                public JsonElement Arguments { get; set; }
            }
        }
    }
}
