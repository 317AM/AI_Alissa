using Alissa.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Generates conversation summaries WITHOUT using AlissaClient.
    /// This service is isolated to prevent session cache pollution.
    /// Summaries are generated directly through the chat client for clean separation.
    /// </summary>
    public class SummaryGenerationService
    {
        // Constants
        private const string EMPTY_CONTENT_MSG = "No conversation content to summarize.";
        private const string SUMMARY_PROMPT_FORMAT = "Summarize the following conversation in approximately {0} lines. Focus on key topics, decisions, and important information. Be concise and extract the essence:\n\n";
        private const string HIGHLIGHT_PROMPT_FORMAT = "Extract {0} key highlights or important points from the following conversation. List them as bullet points:\n\n";
        private const string TOPIC_PROMPT = "Identify the main topics discussed in the following conversation. List them as comma-separated words:\n\n";
        private const string HIGHLIGHT_SEPARATORS = "\n-•*";
        private const string TOPIC_SEPARATORS = ",;\n";
        private const int MIN_HIGHLIGHT_LENGTH = 5;
        private const int MIN_TOPIC_LENGTH = 2;

        private readonly IChatClient _chatClient;
        private readonly IPromptBuilder _promptBuilder;

        public SummaryGenerationService(IChatClient chatClient, IPromptBuilder promptBuilder)
        {
            _chatClient = chatClient;
            _promptBuilder = promptBuilder;
        }

        /// <summary>
        /// Generates a summary of a conversation WITHOUT polluting any session or memory caches.
        /// This is a pure generation function with no side effects.
        /// </summary>
        /// <param name="conversationText">Full conversation text to summarize</param>
        /// <param name="desiredLength">Approximate length in lines</param>
        /// <returns>Generated summary</returns>
        public async Task<string> GenerateSummaryAsync(string conversationText, int desiredLength = 5)
        {
            bool isEmpty = string.IsNullOrWhiteSpace(conversationText);
            if (isEmpty)
            {
                return EMPTY_CONTENT_MSG;
            }

            string summaryPrompt = string.Format(SUMMARY_PROMPT_FORMAT, desiredLength);
            string fullPrompt = summaryPrompt + conversationText;
            string systemPrompt = _promptBuilder.BuildSystemPrompt();

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            await foreach (string token in _chatClient.StreamAsync(systemPrompt, fullPrompt))
            {
                sb.Append(token);
            }

            string result = sb.ToString().Trim();
            return result;
        }

        /// <summary>
        /// Generates key highlights from a conversation.
        /// </summary>
        /// <param name="conversationText">Full conversation text</param>
        /// <param name="highlightCount">Number of highlights to generate</param>
        /// <returns>List of key highlights</returns>
        public async Task<List<string>> GenerateHighlightsAsync(string conversationText, int highlightCount = 5)
        {
            bool isEmpty = string.IsNullOrWhiteSpace(conversationText);
            if (isEmpty)
            {
                return new List<string>();
            }

            string highlightPrompt = string.Format(HIGHLIGHT_PROMPT_FORMAT, highlightCount);
            string fullPrompt = highlightPrompt + conversationText;
            string systemPrompt = _promptBuilder.BuildSystemPrompt();

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            await foreach (string token in _chatClient.StreamAsync(systemPrompt, fullPrompt))
            {
                sb.Append(token);
            }

            List<string> result = ParseHighlights(sb.ToString(), highlightCount);
            return result;
        }

        /// <summary>
        /// Extracts topics from a conversation.
        /// </summary>
        /// <param name="conversationText">Full conversation text</param>
        /// <returns>List of identified topics</returns>
        public async Task<List<string>> GenerateTopicsAsync(string conversationText)
        {
            bool isEmpty = string.IsNullOrWhiteSpace(conversationText);
            if (isEmpty)
            {
                return new List<string>();
            }

            string topicPrompt = TOPIC_PROMPT;
            string fullPrompt = topicPrompt + conversationText;
            string systemPrompt = _promptBuilder.BuildSystemPrompt();

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            await foreach (string token in _chatClient.StreamAsync(systemPrompt, fullPrompt))
            {
                sb.Append(token);
            }

            List<string> result = ParseTopics(sb.ToString());
            return result;
        }

        private static List<string> ParseHighlights(string highlightsText, int expectedCount)
        {
            List<string> highlights = new List<string>();

            bool isEmpty = string.IsNullOrWhiteSpace(highlightsText);
            if (isEmpty)
            {
                return highlights;
            }

            char[] separatorArray = HIGHLIGHT_SEPARATORS.ToCharArray();
            string[] lines = highlightsText.Split(separatorArray, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                string cleaned = lines[i].Trim();
                bool isValid = !string.IsNullOrWhiteSpace(cleaned) && cleaned.Length > MIN_HIGHLIGHT_LENGTH;
                if (isValid)
                {
                    highlights.Add(cleaned);

                    bool isEnough = highlights.Count >= expectedCount;
                    if (isEnough)
                    {
                        break;
                    }
                }
            }

            return highlights;
        }

        private static List<string> ParseTopics(string topicsText)
        {
            List<string> topics = new List<string>();

            bool isEmpty = string.IsNullOrWhiteSpace(topicsText);
            if (isEmpty)
            {
                return topics;
            }

            char[] separatorArray = TOPIC_SEPARATORS.ToCharArray();
            string[] parts = topicsText.Split(separatorArray, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i++)
            {
                string topic = parts[i].Trim();
                bool isValid = !string.IsNullOrWhiteSpace(topic) && topic.Length > MIN_TOPIC_LENGTH;
                if (isValid)
                {
                    topics.Add(topic);
                }
            }

            return topics;
        }
    }
}
