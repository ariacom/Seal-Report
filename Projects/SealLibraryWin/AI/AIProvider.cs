using System;
using System.Collections.Generic;
using OpenAI.Chat;

namespace Seal.AI
{
    public interface IAIProvider
    {
        /// <summary>
        /// Sends <paramref name="messages"/> to the AI and returns the assistant reply.
        /// The reply is automatically appended to <paramref name="messages"/>.
        /// </summary>
        string HandleChat(List<ChatMessage> messages);

        /// <summary>
        /// Sends <paramref name="messages"/> together with available <paramref name="tools"/> to the AI.
        /// If the model decides to invoke one or more tools the descriptors are placed in
        /// <paramref name="toolCalls"/> and the method returns <see cref="string.Empty"/>;
        /// the assistant message (with embedded tool-call data) is appended to <paramref name="messages"/>
        /// automatically so the conversation can be continued after the caller executes the tools.
        /// If the model produces a text reply the reply is appended to <paramref name="messages"/> and returned.
        /// </summary>
        string HandleChatWithTools(List<ChatMessage> messages, IList<AITool> tools, out IList<AIToolCall> toolCalls);
    }

    public abstract class AIProvider : IAIProvider
    {
        /// <summary>Unique identifier for this provider instance.</summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        protected string _endpoint;
        protected string _apiKey;
        protected string _model;
        protected float _temperature;
        protected int _maxTokens;
        protected float _topP;

        /// <summary>
        /// Configures the provider with the endpoint URL, API key, model name, and generation parameters.
        /// Called internally by <see cref="GetAIProvider"/> immediately after construction.
        /// </summary>
        protected abstract void Initialize(string endpoint, string key, string model, float temperature, int maxTokens, float topP);

        /// <summary>
        /// Sends <paramref name="messages"/> to the AI and returns the assistant reply.
        /// The reply is automatically appended to <paramref name="messages"/>.
        /// </summary>
        public abstract string HandleChat(List<ChatMessage> messages);

        /// <inheritdoc cref="IAIProvider.HandleChatWithTools"/>
        public abstract string HandleChatWithTools(List<ChatMessage> messages, IList<AITool> tools, out IList<AIToolCall> toolCalls);

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

    public enum ProviderType
    {
        Azure,
        Ollama,
        Anthropic,
        OpenAI
    }
}
