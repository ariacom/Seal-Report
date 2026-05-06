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
    }

    public abstract class AIProvider : IAIProvider
    {
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
