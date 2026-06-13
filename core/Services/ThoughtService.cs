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
        private readonly string _basePath;
        private readonly IChatClient _chatClient;
        private readonly string _thoughtsPath;
        private List<ThoughtEntry> _cachedThoughts;

        public ThoughtService(string basePath, IChatClient chatClient)
        {
            _basePath = basePath;
            _chatClient = chatClient;
            _thoughtsPath = Path.Combine(basePath, "memory", "thoughts");
            _cachedThoughts = new List<ThoughtEntry>();

            Directory.CreateDirectory(_thoughtsPath);
            LoadCachedThoughts();
        }

        public async Task<string> GenerateThoughtAsync(string userMessage, List<Message> conversationContext)
        {
            string systemPrompt = "You are analyzing this conversation. Generate a brief internal thought about what the user is asking, what they might need, and how to best help. Be concise (1-2 sentences). This is internal reasoning only.";

            var thoughts = new List<string>();

            await foreach (var token in _chatClient.StreamAsync(systemPrompt, userMessage))
            {
                thoughts.Add(token);
            }

            string generatedThought = string.Concat(thoughts);
            return generatedThought;
        }

        public async Task StoreThoughtAsync(string thought, string category)
        {
            var entry = new ThoughtEntry
            {
                Content = thought,
                Category = category,
                CreatedUtc = DateTime.UtcNow
            };

            _cachedThoughts.Add(entry);
            await PersistThoughtAsync(entry);
        }

        public async Task<List<string>> GetRelevantThoughtsAsync(string topic)
        {
            var filtered = _cachedThoughts
                .Where(t => t.RelatedTopics.Contains(topic) || t.Content.Contains(topic, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.CreatedUtc)
                .Take(5)
                .Select(t => t.Content)
                .ToList();

            return await Task.FromResult(filtered);
        }

        public async Task<List<string>> GetSessionThoughtsAsync(string sessionId)
        {
            var sessionThoughts = _cachedThoughts
                .Where(t => t.SessionId == sessionId)
                .OrderByDescending(t => t.CreatedUtc)
                .Select(t => t.Content)
                .ToList();

            return await Task.FromResult(sessionThoughts);
        }

        private void LoadCachedThoughts()
        {
            var thoughtFiles = Directory.GetFiles(_thoughtsPath, "*.json");

            foreach (var file in thoughtFiles)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var thought = JsonSerializer.Deserialize<ThoughtEntry>(json);

                    if (thought != null)
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
            string filePath = Path.Combine(_thoughtsPath, $"{entry.Id}.json");
            string json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(filePath, json);
        }
    }
}
