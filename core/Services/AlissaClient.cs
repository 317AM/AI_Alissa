using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Main client for interacting with Alissa.
    /// Routes all communication through the chat client and manages sessions.
    /// Only public method for Alissa interaction.
    /// </summary>
    public class AlissaClient
    {
        private readonly IChatClient _chatClient;
        private readonly IPromptBuilder _promptBuilder;
        private readonly IMemoryManager _memoryManager;
        private readonly ISessionManager _sessionManager;

        private Session _currentSession;

        /// <summary>
        /// Event fired when a token is received from the model.
        /// </summary>
        public event Action<string>? OnTokenReceived;

        public AlissaClient(IChatClient chatClient, IPromptBuilder promptBuilder,
                             IMemoryManager memoryManager, ISessionManager sessionManager)
        {
            _chatClient = chatClient;
            _promptBuilder = promptBuilder;
            _memoryManager = memoryManager;
            _sessionManager = sessionManager;

            _currentSession = _sessionManager.CreateSession();

            var cachedMessages = _memoryManager.LoadSessionCache();
            if (cachedMessages.Any())
            {
                foreach (var msg in cachedMessages)
                {
                    _currentSession.Messages.Add(msg);
                }
            }
        }

        /// <summary>
        /// Sends a message to Alissa and returns the response as an async enumerable.
        /// This is the primary method for all user interaction.
        /// </summary>
        public async IAsyncEnumerable<string> StreamAsync(string userInput)
        {
            _currentSession.AddMessage(MessageRole.User, userInput);
            _memoryManager.SaveSessionCache(_currentSession.Messages);

            string systemPrompt = BuildSystemPrompt();

            var sb = new StringBuilder();
            var emojiCollector = new StringBuilder();

            await foreach (var token in _chatClient.StreamAsync(systemPrompt, userInput))
            {
                EmojiUtils.ExtractEmojis(token, out string cleaned, out string emojis);

                if (!string.IsNullOrEmpty(cleaned))
                {
                    sb.Append(cleaned);
                    yield return cleaned;
                }

                if (!string.IsNullOrEmpty(emojis))
                {
                    emojiCollector.Append(emojis);
                    OnTokenReceived?.Invoke(emojis);
                }
            }

            string response = sb.ToString();
            string collectedEmojis = emojiCollector.ToString();

            _currentSession.AddMessage(MessageRole.AI, response);
            _currentSession.LatestEmoji = collectedEmojis;
            _currentSession.CollectedEmojis += collectedEmojis;

            _sessionManager.SaveSession(_currentSession);
            _memoryManager.SaveSessionCache(_currentSession.Messages);
        }

        /// <summary>
        /// Gets the current session.
        /// </summary>
        public Session CurrentSession => _currentSession;

        private string BuildSystemPrompt()
        {
            var promptBuilderTyped = _promptBuilder as PromptBuilder;

            if (promptBuilderTyped != null)
            {
                return promptBuilderTyped.BuildSystemPromptWithContext(_currentSession.Messages);
            }

            return _promptBuilder.BuildSystemPrompt();
        }
    }
}
