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
    public class AlissaClient
    {
        private readonly IChatClient _chatClient;
        private readonly IPromptBuilder _promptBuilder;
        private readonly IMemoryManager _memoryManager;
        private readonly ISessionManager _sessionManager;

        private Session _currentSession;

        public AlissaClient(IChatClient chatClient, IPromptBuilder promptBuilder,
                             IMemoryManager memoryManager, ISessionManager sessionManager)
        {
            _chatClient = chatClient;
            _promptBuilder = promptBuilder;
            _memoryManager = memoryManager;
            _sessionManager = sessionManager;

            _currentSession = _sessionManager.CreateSession();
            
            // Load previous session cache if available
            var cachedMessages = _memoryManager.LoadSessionCache();
            if (cachedMessages.Any())
            {
                foreach (var msg in cachedMessages)
                {
                    _currentSession.Messages.Add(msg);
                }
            }
        }

        public async Task<string> SendAsync(string userInput)
        {
            _currentSession.AddMessage(MessageRole.User, userInput);

            // Save session cache after each user message
            _memoryManager.SaveSessionCache(_currentSession.Messages);

            // Build system prompt with recent conversation context
            string systemPrompt = BuildSystemPromptWithContext();

            var sb = new StringBuilder();
            var emojiCollector = new StringBuilder();

            await foreach (var token in _chatClient.StreamAsync(systemPrompt, userInput))
            {
                EmojiUtils.ExtractEmojis(token, out string cleaned, out string emojis);
                if (!string.IsNullOrEmpty(cleaned)) sb.Append(cleaned);
                if (!string.IsNullOrEmpty(emojis)) emojiCollector.Append(emojis);
            }

            string response = sb.ToString();
            string collectedEmojis = emojiCollector.ToString();

            _currentSession.AddMessage(MessageRole.AI, response);
            _sessionManager.SaveSession(_currentSession);

            // Save updated session cache after AI response
            _memoryManager.SaveSessionCache(_currentSession.Messages);

            return response;
        }

        private string BuildSystemPromptWithContext()
        {
            // Get recent messages (last 10) for context
            var recentMessages = _currentSession.Messages
                .Skip(Math.Max(0, _currentSession.Messages.Count - 10))
                .ToList();

            // Use typed method if available, fallback to base method
            var promptBuilderTyped = _promptBuilder as PromptBuilder;
            if (promptBuilderTyped != null)
            {
                return promptBuilderTyped.BuildSystemPromptWithContext(recentMessages);
            }

            return _promptBuilder.BuildSystemPrompt();
        }

        public Session CurrentSession => _currentSession;
    }
}