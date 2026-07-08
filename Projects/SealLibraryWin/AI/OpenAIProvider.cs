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
                ["messages"] = messages.Select(SerializeMessage).ToList<object>(),
                ["temperature"] = _temperature,
                ["top_p"] = _topP
            };
            if (_maxTokens > 0)
                body["max_completion_tokens"] = _maxTokens;

            var request = BuildRequest(body);
            var response = _httpClient.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();

            var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(
                response.Content.ReadAsStringAsync().Result);
            SetLastUsage(responseObject?.Usage?.PromptTokens ?? 0, responseObject?.Usage?.CompletionTokens ?? 0);

            var result = responseObject?.Choices?[0]?.Message?.Content
                ?? throw new Exception("Failed to get response from OpenAI");

            messages.Add(new AssistantChatMessage(result));
            return result;
        }

        /// <inheritdoc/>
        public override string HandleChatWithTools(List<ChatMessage> messages, IList<AITool> tools, out IList<AIToolCall> toolCalls)
        {
            toolCalls = new List<AIToolCall>();

            var body = new Dictionary<string, object>
            {
                ["model"] = _model,
                ["messages"] = messages.Select(SerializeMessage).ToList<object>(),
                ["temperature"] = _temperature,
                ["top_p"] = _topP,
                ["tools"] = tools.Select(t => (object)new
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
            if (_maxTokens > 0)
                body["max_completion_tokens"] = _maxTokens;

            var request = BuildRequest(body);
            var response = _httpClient.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();

            var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(
                response.Content.ReadAsStringAsync().Result);
            SetLastUsage(responseObject?.Usage?.PromptTokens ?? 0, responseObject?.Usage?.CompletionTokens ?? 0);

            var choice = responseObject?.Choices?[0]
                ?? throw new Exception("Failed to get response from OpenAI");

            if (choice.FinishReason == "tool_calls" && choice.Message?.ToolCalls?.Length > 0)
            {
                var calls = choice.Message.ToolCalls
                    .Select(tc => new AIToolCall
                    {
                        Id = tc.Id,
                        Name = tc.Function?.Name,
                        Arguments = tc.Function?.Arguments
                    })
                    .ToList();

                toolCalls = calls;
                messages.Add(new AssistantToolCallsChatMessage(calls, choice.Message.Content));
                return string.Empty;
            }

            var result = choice.Message?.Content
                ?? throw new Exception("Failed to get response from OpenAI");

            messages.Add(new AssistantChatMessage(result));
            return result;
        }

        private HttpRequestMessage BuildRequest(Dictionary<string, object> body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            return request;
        }

        /// <summary>
        /// Serializes a <see cref="ChatMessage"/> to an anonymous object suitable for the
        /// OpenAI REST API, handling plain messages, tool-call agent turns, and tool results.
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
                // SDK-native agent message with tool calls (e.g. loaded from a saved session).
                case AssistantChatMessage asst when asst.ToolCalls?.Count > 0:
                    return new
                    {
                        role = "assistant",
                        content = asst.Content.Count > 0 ? asst.Content[0].Text : (string)null,
                        tool_calls = asst.ToolCalls.Select(tc => (object)new
                        {
                            id = tc.Id,
                            type = "function",
                            function = new { name = tc.FunctionName, arguments = tc.FunctionArguments.ToString() }
                        }).ToList()
                    };
                case AssistantChatMessage:
                    return new { role = "assistant", content = m.Content[0].Text };
                case ToolChatMessage tool:
                    return new { role = "tool", tool_call_id = tool.ToolCallId, content = tool.Content[0].Text };
                default:
                    return new { role = "user", content = m.Content[0].Text };
            }
        }

        private class OpenAIResponse
        {
            [JsonPropertyName("choices")]
            public Choice[] Choices { get; set; }

            [JsonPropertyName("usage")]
            public UsageInfo Usage { get; set; }

            public class UsageInfo
            {
                [JsonPropertyName("prompt_tokens")]
                public int PromptTokens { get; set; }

                [JsonPropertyName("completion_tokens")]
                public int CompletionTokens { get; set; }
            }

            public class Choice
            {
                [JsonPropertyName("message")]
                public Message Message { get; set; }

                [JsonPropertyName("finish_reason")]
                public string FinishReason { get; set; }
            }

            public class Message
            {
                [JsonPropertyName("content")]
                public string Content { get; set; }

                [JsonPropertyName("tool_calls")]
                public ToolCall[] ToolCalls { get; set; }
            }

            public class ToolCall
            {
                [JsonPropertyName("id")]
                public string Id { get; set; }

                [JsonPropertyName("function")]
                public FunctionCall Function { get; set; }
            }

            public class FunctionCall
            {
                [JsonPropertyName("name")]
                public string Name { get; set; }

                [JsonPropertyName("arguments")]
                public string Arguments { get; set; }
            }
        }
    }
}
