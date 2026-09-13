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
        // Constants
        private const string THOUGHT_CATEGORY = "session_reflection";

        private readonly IChatClient _chatClient;
        private readonly IPromptBuilder _promptBuilder;
        private readonly IMemoryManager _memoryManager;
        private readonly ISessionManager _sessionManager;
        private readonly IThoughtService? _thoughtService;
        private readonly ISpeechToTextService? _speechToTextService;
        private readonly ITextToSpeechService? _textToSpeechService;

        private Session _currentSession;

        /// <summary>
        /// Event fired when a token is received from the model.
        /// </summary>
        public event Action<string>? OnTokenReceived;

        /// <summary>
        /// Event fired when audio is ready for playback (after synthesis).
        /// </summary>
        public event Action<byte[]>? OnAudioReady;

        public AlissaClient(
            IChatClient chatClient,
            IPromptBuilder promptBuilder,
            IMemoryManager memoryManager,
            ISessionManager sessionManager,
            IThoughtService? thoughtService = null,
            ISpeechToTextService? speechToTextService = null,
            ITextToSpeechService? textToSpeechService = null)
        {
            _chatClient = chatClient;
            _promptBuilder = promptBuilder;
            _memoryManager = memoryManager;
            _sessionManager = sessionManager;
            _thoughtService = thoughtService;
            _speechToTextService = speechToTextService;
            _textToSpeechService = textToSpeechService;

            _currentSession = _sessionManager.CreateSession();

            List<Message> cachedMessages = _memoryManager.LoadSessionCache();
            bool hasCachedMessages = cachedMessages.Any();
            if (hasCachedMessages)
            {
                for (int i = 0; i < cachedMessages.Count; i++)
                {
                    Message msg = cachedMessages[i];
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

            string systemPrompt = await BuildSystemPromptAsync(userInput);

            StringBuilder sb = new StringBuilder();
            StringBuilder emojiCollector = new StringBuilder();

            await foreach (string token in _chatClient.StreamAsync(systemPrompt, userInput))
            {
                EmojiUtils.ExtractEmojis(token, out string cleaned, out string emojis);

                bool hasCleanedContent = !string.IsNullOrEmpty(cleaned);
                if (hasCleanedContent)
                {
                    sb.Append(cleaned);
                    yield return cleaned;
                }

                bool hasEmojis = !string.IsNullOrEmpty(emojis);
                if (hasEmojis)
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

            bool hasThoughtService = _thoughtService != null;
            if (hasThoughtService)
            {
                _ = FireAndForgetThoughtGeneration(userInput);
            }

            bool hasTextToSpeech = _textToSpeechService != null;
            if (hasTextToSpeech)
            {
                _ = FireAndForgetAudioSynthesis(response);
            }
        }

        private async Task FireAndForgetAudioSynthesis(string response)
        {
            bool hasTextToSpeechService = _textToSpeechService != null;

            if (!hasTextToSpeechService)
            {
                return;
            }

            try
            {
                byte[] audioData = await _textToSpeechService!.SynthesizeAsync(response).ConfigureAwait(false);

                bool hasAudioData = audioData.Length > 0;
                if (hasAudioData)
                {
                    OnAudioReady?.Invoke(audioData);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, null, false);
            }
        }

        /// <summary>
        /// Transcribes audio to text and streams the response.
        /// Delegates to StreamAsync after converting audio to text.
        /// </summary>
        public async IAsyncEnumerable<string> StreamFromAudioAsync(byte[] audioBytes, string format)
        {
            bool speechToTextAvailable = _speechToTextService != null;
            {
                if (!speechToTextAvailable)
                {
                    throw new InvalidOperationException("Speech-to-text service is not configured");
                }
            }

            string userInput = await _speechToTextService!.TranscribeAsync(audioBytes, format).ConfigureAwait(false);

            await foreach (string token in StreamAsync(userInput))
            {
                yield return token;
            }
        }

        private async Task FireAndForgetThoughtGeneration(string userInput)
        {
            bool hasThoughtService = _thoughtService != null;

            if (!hasThoughtService)
            {
                return;
            }

            try
            {
                // Check if we captured native thinking from the model
                bool thinkingCaptured = false;
                string thinkingText = string.Empty;

                IThinkingCapable? thinkingClient = _chatClient as IThinkingCapable;
                bool hasThinkingCapability = thinkingClient != null;
                if (hasThinkingCapability)
                {
                    thinkingText = thinkingClient!.LastThinkingText;
                    thinkingCaptured = !string.IsNullOrEmpty(thinkingText);
                }

                // If we captured thinking, store it and skip redundant generation
                if (thinkingCaptured)
                {
                    await _thoughtService.StoreThoughtAsync(thinkingText, THOUGHT_CATEGORY);
                }
                else
                {
                    // Fall back to generating thought via separate model call
                    string thought = await _thoughtService.GenerateThoughtAsync(userInput, _currentSession.Messages);
                    bool hasThought = !string.IsNullOrEmpty(thought);

                    if (hasThought)
                    {
                        await _thoughtService.StoreThoughtAsync(thought, THOUGHT_CATEGORY);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, null, false);
            }
        }

        /// <summary>
        /// Gets the current session.
        /// </summary>
        public Session CurrentSession => _currentSession;

        /// <summary>
        /// Gets the prompt builder (for setting user context etc).
        /// </summary>
        public IPromptBuilder PromptBuilder => _promptBuilder;

        /// <summary>
        /// Gets the speech-to-text service (if enabled).
        /// </summary>
        public ISpeechToTextService? SpeechToTextService => _speechToTextService;

        private async Task<string> BuildSystemPromptAsync(string currentUserInput = "")
        {
            PromptBuilder? promptBuilderTyped = _promptBuilder as PromptBuilder;

            bool isPromptBuilder = promptBuilderTyped != null;
            if (isPromptBuilder)
            {
                string result = await promptBuilderTyped.BuildSystemPromptWithContextAsync(_currentSession.Messages, currentUserInput);
                return result;
            }

            return _promptBuilder.BuildSystemPrompt();
        }
    }
}
