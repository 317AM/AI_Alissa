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
            if (string.IsNullOrWhiteSpace(conversationText))
            {
                return "No conversation content to summarize.";
            }

            string summaryPrompt = $"Summarize the following conversation in approximately {desiredLength} lines. " +
                "Focus on key topics, decisions, and important information. " +
                "Be concise and extract the essence:\n\n";

            string fullPrompt = summaryPrompt + conversationText;
            string systemPrompt = _promptBuilder.BuildSystemPrompt();

            var sb = new System.Text.StringBuilder();

            await foreach (var token in _chatClient.StreamAsync(systemPrompt, fullPrompt))
            {
                sb.Append(token);
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Generates key highlights from a conversation.
        /// </summary>
        /// <param name="conversationText">Full conversation text</param>
        /// <param name="highlightCount">Number of highlights to generate</param>
        /// <returns>List of key highlights</returns>
        public async Task<List<string>> GenerateHighlightsAsync(string conversationText, int highlightCount = 5)
        {
            if (string.IsNullOrWhiteSpace(conversationText))
            {
                return new List<string>();
            }

            string highlightPrompt = $"Extract {highlightCount} key highlights or important points from the following conversation. " +
                "List them as bullet points:\n\n";

            string fullPrompt = highlightPrompt + conversationText;
            string systemPrompt = _promptBuilder.BuildSystemPrompt();

            var sb = new System.Text.StringBuilder();

            await foreach (var token in _chatClient.StreamAsync(systemPrompt, fullPrompt))
            {
                sb.Append(token);
            }

            return ParseHighlights(sb.ToString(), highlightCount);
        }

        /// <summary>
        /// Extracts topics from a conversation.
        /// </summary>
        /// <param name="conversationText">Full conversation text</param>
        /// <returns>List of identified topics</returns>
        public async Task<List<string>> GenerateTopicsAsync(string conversationText)
        {
            if (string.IsNullOrWhiteSpace(conversationText))
            {
                return new List<string>();
            }

            string topicPrompt = "Identify the main topics discussed in the following conversation. " +
                "List them as comma-separated words:\n\n";

            string fullPrompt = topicPrompt + conversationText;
            string systemPrompt = _promptBuilder.BuildSystemPrompt();

            var sb = new System.Text.StringBuilder();

            await foreach (var token in _chatClient.StreamAsync(systemPrompt, fullPrompt))
            {
                sb.Append(token);
            }

            return ParseTopics(sb.ToString());
        }

        private static List<string> ParseHighlights(string highlightsText, int expectedCount)
        {
            var highlights = new List<string>();

            if (string.IsNullOrWhiteSpace(highlightsText))
            {
                return highlights;
            }

            var lines = highlightsText.Split(new[] { "\n", "-", "•", "*" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                string cleaned = line.Trim();
                if (!string.IsNullOrWhiteSpace(cleaned) && cleaned.Length > 5)
                {
                    highlights.Add(cleaned);

                    if (highlights.Count >= expectedCount)
                    {
                        break;
                    }
                }
            }

            return highlights;
        }

        private static List<string> ParseTopics(string topicsText)
        {
            var topics = new List<string>();

            if (string.IsNullOrWhiteSpace(topicsText))
            {
                return topics;
            }

            var parts = topicsText.Split(new[] { ",", ";", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                string topic = part.Trim();
                if (!string.IsNullOrWhiteSpace(topic) && topic.Length > 2)
                {
                    topics.Add(topic);
                }
            }

            return topics;
        }
    }
}
