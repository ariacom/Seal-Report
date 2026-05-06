using System;
using System.Collections.Generic;
using Ganss.Xss;
using OpenAI.Chat;
using Seal.Model;

namespace Seal.AI
{
    public class AIClient
    {

        /// <summary>
        /// Creates a new <see cref="AIClient"/> bound to the given provider configuration name.
        /// </summary>
        public AIClient(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                var config = Repository.Instance.Configuration.AIProviders.Find(c => c.IsDefault);
                if (config == null)
                    throw new Exception("No default AI provider configuration found. Set IsDefault = true on one configuration.");
                ProviderName = config.Name;
            }
            else
            {
                ProviderName = providerName;
            }
        }

        /// <summary>
        /// Name of the <see cref="AIProviderConfiguration"/> to use, looked up from
        /// <see cref="Repository.AIProviderConfigurations"/> at initialisation time.
        /// </summary>
        public string ProviderName { get; set; }

        private IAIProvider _provider;

        /// <summary>
        /// Resolves <see cref="ProviderName"/> against <see cref="Repository.AIProviderConfigurations"/>,
        /// creates and initialises the matching provider, and returns it.
        /// Subsequent calls return the cached instance unless <paramref name="forceReinit"/> is <c>true</c>.
        /// </summary>
        /// <exception cref="Exception">
        /// Thrown when no configuration with the given <see cref="ProviderName"/> is found.
        /// </exception>
        public IAIProvider GetProvider(bool forceReinit = false)
        {
            if (_provider == null || forceReinit)
            {
                var config = Repository.Instance.Configuration.AIProviders
                    .Find(c => c.Name == ProviderName);

                if (config == null)
                    throw new Exception($"AI provider configuration '{ProviderName}' not found.");

                _provider = AIProvider.GetAIProvider(config.Type, config.EndPoint, config.ClearProviderKey, config.Model, config.Temperature, config.MaxTokens, config.TopP);
            }
            return _provider;
        }

        /// <summary>
        /// Sends the message list to the AI and returns the assistant reply.
        /// The reply is automatically appended to <paramref name="messages"/>.
        /// </summary>
        public string HandleChat(List<ChatMessage> messages)
        {
            return GetProvider().HandleChat(messages);
        }

        /// <summary>
        /// Sends the message list to the AI and returns the assistant reply as Html.
        /// The reply is automatically appended to <paramref name="messages"/>.
        /// </summary>
        public string HandleChatHtmlSanitized(List<ChatMessage> messages)
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedAttributes.Add("class");
            return sanitizer.Sanitize(HandleChat(messages));
        }
    }
}
