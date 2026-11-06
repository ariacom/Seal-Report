using System;
using System.Collections.Generic;
using OpenAI.Chat;

namespace Seal.AI
{
    /// <summary>
    /// Interface implemented by all AI chat providers (OpenAI, Azure OpenAI, Anthropic, Ollama)
    /// used by the AI Agents feature.
    /// </summary>
    public interface IAIProvider
    {
        /// <summary>
        /// Sends <paramref name="messages"/> to the AI and returns the agent reply.
        /// The reply is automatically appended to <paramref name="messages"/>.
        /// </summary>
        string HandleChat(List<ChatMessage> messages);

        /// <summary>
        /// Sends <paramref name="messages"/> together with available <paramref name="tools"/> to the AI.
        /// If the model decides to invoke one or more tools the descriptors are placed in
        /// <paramref name="toolCalls"/> and the method returns <see cref="string.Empty"/>;
        /// the agent message (with embedded tool-call data) is appended to <paramref name="messages"/>
        /// automatically so the conversation can be continued after the caller executes the tools.
        /// If the model produces a text reply the reply is appended to <paramref name="messages"/> and returned.
        /// </summary>
        string HandleChatWithTools(List<ChatMessage> messages, IList<AITool> tools, out IList<AIToolCall> toolCalls);

        /// <summary>
        /// Token usage of the last API call (input/output tokens, one call).
        /// Zeroes when the API did not report usage information.
        /// </summary>
        AIUsage LastUsage { get; }
    }

    /// <summary>
    /// Base class for AI chat providers: holds the common connection and generation parameters
    /// and the factory methods to create a provider from a configuration.
    /// </summary>
    public abstract class AIProvider : IAIProvider
    {
        /// <summary>Unique identifier for this provider instance.</summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        /// <summary>Resource endpoint URL of the provider API.</summary>
        protected string _endpoint;
        /// <summary>API key used to authenticate against the provider.</summary>
        protected string _apiKey;
        /// <summary>Name of the model to use (e.g. gpt-4o, claude-3-5-sonnet-20241022, llama3).</summary>
        protected string _model;
        /// <summary>Sampling temperature (0.0 = default, deterministic).</summary>
        protected float _temperature;
        /// <summary>Maximum number of tokens to generate (0 = provider default).</summary>
        protected int _maxTokens;
        /// <summary>Nucleus sampling probability (1.0 = default, disabled).</summary>
        protected float _topP;

        /// <summary>
        /// Configures the provider with the endpoint URL, API key, model name, and generation parameters.
        /// Called internally by <see cref="GetAIProvider"/> immediately after construction.
        /// </summary>
        protected abstract void Initialize(string endpoint, string key, string model, float temperature, int maxTokens, float topP);

        /// <summary>
        /// Sends <paramref name="messages"/> to the AI and returns the agent reply.
        /// The reply is automatically appended to <paramref name="messages"/>.
        /// </summary>
        public abstract string HandleChat(List<ChatMessage> messages);

        /// <inheritdoc cref="IAIProvider.HandleChatWithTools"/>
        public abstract string HandleChatWithTools(List<ChatMessage> messages, IList<AITool> tools, out IList<AIToolCall> toolCalls);

        /// <inheritdoc cref="IAIProvider.LastUsage"/>
        public AIUsage LastUsage { get; protected set; } = new AIUsage();

        /// <summary>
        /// True if the provider sends the images of a tool result (image parts of a <see cref="ToolChatMessage"/>) to the model.
        /// </summary>
        public virtual bool SupportsToolResultImages => false;

        /// <summary>
        /// Models (endpoint|model) that refused the temperature parameter (e.g. GPT-6 accepts only its default value):
        /// the next requests to these models are sent without temperature.
        /// </summary>
        static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _noTemperatureModels = new();

        /// <summary>
        /// True if the temperature must be sent to the model: false once the model refused it.
        /// </summary>
        protected bool SendTemperature => !_noTemperatureModels.ContainsKey(_endpoint + "|" + _model);

        /// <summary>
        /// If the error body of an HTTP 400 says that the model refuses the temperature parameter, remembers it for
        /// the model and returns true: the caller retries the request without temperature.
        /// </summary>
        protected bool HandleTemperatureRefused(int status, string errorBody)
        {
            if (status != 400 || !SendTemperature || string.IsNullOrEmpty(errorBody)) return false;
            if (!errorBody.Contains("\"temperature\"") && !errorBody.Contains("'temperature'")) return false;
            _noTemperatureModels[_endpoint + "|" + _model] = true;
            return true;
        }

        /// <summary>
        /// Models (endpoint|model) that refused function tools with their default reasoning on the chat completions API
        /// (e.g. GPT-6): the next requests to these models are sent with reasoning_effort 'none'.
        /// </summary>
        static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _noReasoningModels = new();

        /// <summary>
        /// True if the request must be sent with reasoning_effort 'none': the model refused the tools with its default reasoning.
        /// </summary>
        protected bool NoReasoning => _noReasoningModels.ContainsKey(_endpoint + "|" + _model);

        /// <summary>
        /// If the error body of an HTTP 400 says that the model accepts the function tools only with reasoning_effort 'none',
        /// remembers it for the model and returns true: the caller retries the request with reasoning_effort 'none'.
        /// </summary>
        protected bool HandleReasoningRefused(int status, string errorBody)
        {
            if (status != 400 || NoReasoning || string.IsNullOrEmpty(errorBody)) return false;
            if (!errorBody.Contains("reasoning_effort") || !errorBody.Contains("'none'")) return false;
            _noReasoningModels[_endpoint + "|" + _model] = true;
            return true;
        }

        /// <summary>
        /// Returns the message of an OpenAI-like JSON error body ({"error":{"message":...}}), else the body itself (truncated).
        /// </summary>
        protected static string GetErrorMessage(string errorBody)
        {
            if (string.IsNullOrWhiteSpace(errorBody)) return null;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(errorBody);
                if (doc.RootElement.TryGetProperty("error", out var error))
                {
                    if (error.ValueKind == System.Text.Json.JsonValueKind.Object && error.TryGetProperty("message", out var message)) return message.GetString();
                    if (error.ValueKind == System.Text.Json.JsonValueKind.String) return error.GetString();
                }
                if (doc.RootElement.TryGetProperty("message", out var msg) && msg.ValueKind == System.Text.Json.JsonValueKind.String) return msg.GetString();
            }
            catch { }
            return errorBody.Length > 500 ? errorBody.Substring(0, 500) : errorBody;
        }

        /// <summary>
        /// Records the token usage of the API call that just completed.
        /// </summary>
        protected void SetLastUsage(int inputTokens, int outputTokens, int cachedInputTokens = 0)
        {
            LastUsage = new AIUsage { InputTokens = inputTokens, OutputTokens = outputTokens, CachedInputTokens = Math.Min(cachedInputTokens, inputTokens), Calls = 1 };
        }

        /// <summary>
        /// Resolves a provider configuration by name, creates it, and returns it.
        /// Pass <c>null</c> or empty to use the default provider
        /// (set via <see cref="AIServerConfiguration.DefaultProviderGUID"/>).
        /// </summary>
        /// <exception cref="Exception">
        /// Thrown when no matching or no default configuration is found.
        /// </exception>
        public static IAIProvider GetProvider(string providerName = null)
        {
            AIProviderConfiguration config;
            if (string.IsNullOrEmpty(providerName))
            {
                var defaultGuid = Seal.Model.Repository.Instance.AIConfiguration.DefaultProviderGUID;
                var providers = Seal.Model.Repository.Instance.AIConfiguration.AIProviders;
                config = (string.IsNullOrEmpty(defaultGuid)
                        ? providers.Find(p => p.IsEnabled)
                        : providers.Find(p => p.GUID == defaultGuid))
                    ?? throw new Exception("No default AI provider configuration found. Set Default Provider in AI Configuration.");
            }
            else
            {
                config = Seal.Model.Repository.Instance.AIConfiguration.AIProviders.Find(p => p.Name == providerName)
                    ?? throw new Exception($"AI provider configuration '{providerName}' not found.");
            }
            return GetAIProvider(config.Type, config.EndPoint, config.ClearProviderKey, config.Model);
        }

        /// <summary>
        /// Creates, initialises, and returns a provider for the given <paramref name="providerType"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when both <paramref name="temperature"/> and <paramref name="topP"/> are set to non-default values.
        /// Set one or the other, not both.
        /// </exception>
        public static IAIProvider GetAIProvider(ProviderType providerType, string endpoint, string key, string model, float temperature = 0.0f, int maxTokens = 0, float topP = 1.0f)
        {
            if (temperature != 0.0f && topP != 1.0f)
                throw new ArgumentException("Set either Temperature or TopP, not both. Leave one at its default (Temperature = 0.0 or TopP = 1.0).");

            AIProvider provider = providerType switch
            {
                ProviderType.Azure => new AzureProvider(),
                ProviderType.OpenAI => new OpenAIProvider(),
                ProviderType.Anthropic => new AnthropicProvider(),
                ProviderType.Ollama => new OllamaProvider(),
                _ => throw new ArgumentException($"Unknown provider type: {providerType}")
            };
            provider.Initialize(endpoint, key, model, temperature, maxTokens, topP);
            return provider;
        }
    }

    /// <summary>
    /// Types of AI providers supported.
    /// </summary>
    public enum ProviderType
    {
        /// <summary>Azure OpenAI service.</summary>
        Azure,
        /// <summary>Ollama server (local models).</summary>
        Ollama,
        /// <summary>Anthropic API (Claude models).</summary>
        Anthropic,
        /// <summary>OpenAI API.</summary>
        OpenAI
    }
}
