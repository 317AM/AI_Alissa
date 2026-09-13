using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Service for managing Alissa's internal thoughts and reasoning.
    /// Stores and retrieves thoughts separately from conversation.
    /// </summary>
    public class ThoughtService : IThoughtService
    {
        // Constants
        private const string THOUGHTS_SUBDIR = "memory";
        private const string THOUGHTS_DIR = "thoughts";
        private const string JSON_EXTENSION = ".json";
        private const string JSON_PATTERN = "*.json";
        private const string THOUGHT_SYSTEM_PROMPT = "You are analyzing this conversation. Generate a brief internal thought about what the user is asking, what they might need, and how to best help. Be concise (1-2 sentences). This is internal reasoning only.";
        private const int MAX_RELEVANT_THOUGHTS = 5;

        private readonly string _basePath;
        private readonly IChatClient _chatClient;
        private readonly string _thoughtsPath;
        private List<ThoughtEntry> _cachedThoughts;

        public ThoughtService(string basePath, IChatClient chatClient)
        {
            _basePath = basePath;
            _chatClient = chatClient;
            _thoughtsPath = Path.Combine(basePath, THOUGHTS_SUBDIR, THOUGHTS_DIR);
            _cachedThoughts = new List<ThoughtEntry>();

            Directory.CreateDirectory(_thoughtsPath);
            LoadCachedThoughts();
        }

        public async Task<string> GenerateThoughtAsync(string userMessage, List<Message> conversationContext)
        {
            string systemPrompt = THOUGHT_SYSTEM_PROMPT;

            List<string> thoughts = new List<string>();

            await foreach (string token in _chatClient.StreamAsync(systemPrompt, userMessage))
            {
                thoughts.Add(token);
            }

            string generatedThought = string.Concat(thoughts);
            return generatedThought;
        }

        public async Task StoreThoughtAsync(string thought, string category, string[]? relatedTopics = null)
        {
            ThoughtEntry entry = new ThoughtEntry
            {
                Content = thought,
                Category = category,
                CreatedUtc = DateTime.UtcNow,
                RelatedTopics = relatedTopics ?? Array.Empty<string>()
            };

            _cachedThoughts.Add(entry);
            await PersistThoughtAsync(entry);
        }

        public async Task<List<string>> GetRelevantThoughtsAsync(string topic)
        {
            string[] queryKeywords = ExtractKeywords(topic);

            List<string> filtered = _cachedThoughts
                .Select(t => new
                {
                    Content = t.Content,
                    Score = CalculateRelevanceScore(queryKeywords, t.RelatedTopics),
                    CreatedUtc = t.CreatedUtc
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.CreatedUtc)
                .Take(MAX_RELEVANT_THOUGHTS)
                .Select(x => x.Content)
                .ToList();

            List<string> result = await Task.FromResult(filtered);
            return result;
        }

        public async Task<List<string>> GetSessionThoughtsAsync(string sessionId)
        {
            List<string> sessionThoughts = _cachedThoughts
                .Where(t => t.SessionId == sessionId)
                .OrderByDescending(t => t.CreatedUtc)
                .Select(t => t.Content)
                .ToList();

            List<string> result = await Task.FromResult(sessionThoughts);
            return result;
        }

        private void LoadCachedThoughts()
        {
            string[] thoughtFiles = Directory.GetFiles(_thoughtsPath, JSON_PATTERN);

            for (int i = 0; i < thoughtFiles.Length; i++)
            {
                string file = thoughtFiles[i];
                try
                {
                    string json = File.ReadAllText(file);
                    ThoughtEntry? thought = JsonSerializer.Deserialize<ThoughtEntry>(json);

                    bool isValid = thought != null;
                    if (isValid)
                    {
                        _cachedThoughts.Add(thought);
                    }
                }
                catch
                {
                    // Silently skip corrupted files
                }
            }
        }

        private async Task PersistThoughtAsync(ThoughtEntry entry)
        {
            string filePath = Path.Combine(_thoughtsPath, entry.Id + JSON_EXTENSION);
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(entry, options);

            await File.WriteAllTextAsync(filePath, json);
        }

        private static string[] ExtractKeywords(string text)
        {
            List<string> keywords = new List<string>();

            string[] words = text.ToLower().Split(new[] { ' ', ',', '.', ';', '\n', '\r', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);

            int count = 0;
            for (int i = 0; i < words.Length && count < 5; i++)
            {
                string word = words[i];
                bool isLongEnough = word.Length > 3;
                bool isNotStopword = !IsStopword(word);

                if (isLongEnough && isNotStopword)
                {
                    keywords.Add(word);
                    count++;
                }
            }

            return keywords.Distinct().ToArray();
        }

        private static bool IsStopword(string word)
        {
            string[] stopwords = new[] { "the", "and", "or", "to", "a", "an", "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may", "might", "must", "can", "this", "that", "these", "those", "i", "you", "he", "she", "it", "we", "they", "what", "which", "who", "when", "where", "why", "how", "for", "in", "on", "at", "by", "from", "of", "with", "as", "about", "into", "through", "during", "before", "after", "above", "below", "up", "down", "out", "off", "over", "under", "again", "further", "then", "once", "here", "there", "if", "so", "than", "too", "very", "just" };
            bool result;
            try
            {
                result = stopwords.Contains(word);
            }
            catch
            {
                result = false;
            }

            return result;
        }

        private static int CalculateRelevanceScore(string[] queryKeywords, string[] thoughtTopics)
        {
            int score = 0;

            if (queryKeywords == null || queryKeywords.Length == 0 || thoughtTopics == null || thoughtTopics.Length == 0)
            {
                return score;
            }

            for (int i = 0; i < queryKeywords.Length; i++)
            {
                string keyword = queryKeywords[i];
                for (int j = 0; j < thoughtTopics.Length; j++)
                {
                    string topic = thoughtTopics[j];
                    bool matches = topic.Contains(keyword, StringComparison.OrdinalIgnoreCase) || keyword.Contains(topic, StringComparison.OrdinalIgnoreCase);
                    if (matches)
                    {
                        score++;
                    }
                }
            }

            return score;
        }
    }
}
