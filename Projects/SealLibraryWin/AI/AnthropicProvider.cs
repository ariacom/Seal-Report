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

            var body = new Dictionary<string, object>
            {
                ["model"] = _model,
                ["messages"] = SerializeMessages(messages),
                ["system"] = systemMessage,
                ["max_tokens"] = _maxTokens > 0 ? _maxTokens : 4096,  // required by Anthropic
            };
            // Send temperature OR top_p, never both: Sonnet 4.5+ returns 400 when both are specified.
            if (_topP != 1.0f) body["top_p"] = _topP;
            else body["temperature"] = _temperature;

            var response = SendRequest(body);
            var result = response?.Content?[0]?.Text
                ?? throw new Exception("Failed to get response from Anthropic");

            messages.Add(new AssistantChatMessage(result));
            return result;
        }

        /// <inheritdoc/>
        public override string HandleChatWithTools(List<ChatMessage> messages, IList<AITool> tools, out IList<AIToolCall> toolCalls)
        {
            toolCalls = new List<AIToolCall>();

            var systemMessage = messages
                .OfType<SystemChatMessage>()
                .FirstOrDefault()?.Content[0].Text ?? string.Empty;

            var body = new Dictionary<string, object>
            {
                ["model"] = _model,
                ["messages"] = SerializeMessages(messages),
                ["system"] = systemMessage,
                ["max_tokens"] = _maxTokens > 0 ? _maxTokens : 4096,
                ["tools"] = tools.Select(t => (object)new
                {
                    name = t.Name,
                    description = t.Description,
                    input_schema = string.IsNullOrEmpty(t.ParametersSchema)
                        ? JsonSerializer.Deserialize<JsonElement>("{\"type\":\"object\",\"properties\":{}}")
                        : JsonSerializer.Deserialize<JsonElement>(t.ParametersSchema)
                }).ToList()
            };
            // Send temperature OR top_p, never both: Sonnet 4.5+ returns 400 when both are specified.
            if (_topP != 1.0f) body["top_p"] = _topP;
            else body["temperature"] = _temperature;

            var response = SendRequest(body);

            if (response.StopReason == "tool_use")
            {
                var toolUseBlocks = response.Content
                    .Where(b => b.Type == "tool_use")
                    .ToList();

                var calls = toolUseBlocks
                    .Select(b => new AIToolCall
                    {
                        Id = b.Id,
                        Name = b.Name,
                        Arguments = b.Input.GetRawText()
                    })
                    .ToList();

                toolCalls = calls;

                // Capture any interstitial text the model produced alongside the tool calls.
                var textBlock = response.Content.FirstOrDefault(b => b.Type == "text");
                messages.Add(new AssistantToolCallsChatMessage(calls, textBlock?.Text));
                return string.Empty;
            }

            var result = response?.Content?[0]?.Text
                ?? throw new Exception("Failed to get response from Anthropic");

            messages.Add(new AssistantChatMessage(result));
            return result;
        }

        private AnthropicResponse SendRequest(Dictionary<string, object> body)
        {
            var json = JsonSerializer.Serialize(body);
            const int maxRetries = 5;

            for (int attempt = 0; ; attempt++)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("x-api-key", _apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");

                var response = _httpClient.SendAsync(request).Result;
                var content = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<AnthropicResponse>(content);

                // Retry on rate limit (429) and overload (529), honoring Retry-After.
                bool retryable = (int)response.StatusCode == 429 || (int)response.StatusCode == 529;
                if (retryable && attempt < maxRetries)
                {
                    System.Threading.Thread.Sleep(GetRetryDelay(response, attempt));
                    continue;
                }

                throw new Exception($"Anthropic API {(int)response.StatusCode} ({response.ReasonPhrase}): {content}");
            }
        }

        private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
        {
            // Honor the Retry-After header (seconds) when Anthropic provides it.
            if (response.Headers.TryGetValues("retry-after", out var values)
                && int.TryParse(values.FirstOrDefault(), out var seconds) && seconds > 0)
                return TimeSpan.FromSeconds(Math.Min(seconds, 60));

            // Otherwise exponential backoff capped at 30s: 2, 4, 8, 16, 30.
            return TimeSpan.FromSeconds(Math.Min(2 * Math.Pow(2, attempt), 30));
        }

        private static string GetRole(ChatMessage message) => message switch
        {
            UserChatMessage => "user",
            AssistantChatMessage => "assistant",
            _ => "user"
        };

        /// <summary>
        /// Serializes the conversation history into the format Anthropic expects:
        /// <list type="bullet">
        /// <item><see cref="SystemChatMessage"/> entries are skipped (handled separately as the top-level <c>system</c> field).</item>
        /// <item>Consecutive <see cref="ToolChatMessage"/> entries are grouped into a single user message
        /// with <c>tool_result</c> content blocks, as required by Anthropic's alternating-turn rule.</item>
        /// <item><see cref="AssistantToolCallsChatMessage"/> entries become agent messages with
        /// <c>tool_use</c> content blocks (plus an optional leading text block).</item>
        /// <item>All other messages become plain role/content objects.</item>
        /// </list>
        /// </summary>
        private static List<object> SerializeMessages(List<ChatMessage> messages)
        {
            var result = new List<object>();
            int i = 0;
            while (i < messages.Count)
            {
                var msg = messages[i];

                if (msg is SystemChatMessage)
                {
                    i++;
                    continue;
                }

                if (msg is ToolChatMessage)
                {
                    // Group consecutive tool results into one user message.
                    var toolResults = new List<object>();
                    while (i < messages.Count && messages[i] is ToolChatMessage toolMsg)
                    {
                        toolResults.Add(new
                        {
                            type = "tool_result",
                            tool_use_id = toolMsg.ToolCallId,
                            content = toolMsg.Content[0].Text
                        });
                        i++;
                    }
                    result.Add(new { role = "user", content = toolResults });
                    continue;
                }

                // Must be checked before AssistantChatMessage (it is a subclass).
                if (msg is AssistantToolCallsChatMessage atcm)
                {
                    var content = new List<object>();
                    if (!string.IsNullOrEmpty(atcm.TextContent))
                        content.Add(new { type = "text", text = atcm.TextContent });
                    foreach (var tc in atcm.AIToolCalls)
                    {
                        content.Add(new
                        {
                            type = "tool_use",
                            id = tc.Id,
                            name = tc.Name,
                            input = JsonSerializer.Deserialize<JsonElement>(tc.Arguments)
                        });
                    }
                    result.Add(new { role = "assistant", content });
                    i++;
                    continue;
                }

                result.Add(new
                {
                    role = GetRole(msg),
                    content = msg.Content[0].Text
                });
                i++;
            }
            return result;
        }

        private class AnthropicResponse
        {
            [JsonPropertyName("stop_reason")]
            public string StopReason { get; set; }

            [JsonPropertyName("content")]
            public ContentBlock[] Content { get; set; }

            public class ContentBlock
            {
                [JsonPropertyName("type")]
                public string Type { get; set; }

                // For text blocks
                [JsonPropertyName("text")]
                public string Text { get; set; }

                // For tool_use blocks
                [JsonPropertyName("id")]
                public string Id { get; set; }

                [JsonPropertyName("name")]
                public string Name { get; set; }

                [JsonPropertyName("input")]
                public JsonElement Input { get; set; }
            }
        }
    }
}
