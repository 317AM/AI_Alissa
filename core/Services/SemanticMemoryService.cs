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
    /// Service for semantic and relational memory management.
    /// Provides similarity matching and memory relationships.
    /// </summary>
    public class SemanticMemoryService : ISemanticMemoryService
    {
        // Constants
        private const string MEMORY_DIR = "memory";
        private const string SEMANTIC_SUBDIR = "semantic";
        private const string JSON_EXTENSION = ".json";
        private const string JSON_PATTERN = "*.json";
        private const double BASE_SCORE_FACTOR = 0.1;
        private const double ACCESS_BONUS_FACTOR = 0.05;
        private const double MAX_ACCESS_BONUS = 0.3;
        private const int DAYS_PER_YEAR = 365;
        private const double MIN_RELEVANCE_SCORE = 0.0;
        private const double MAX_RELEVANCE_SCORE = 1.0;

        private readonly string _basePath;
        private readonly string _memoryPath;
        private List<SemanticMemoryEntry> _cachedMemories;

        public SemanticMemoryService(string basePath)
        {
            _basePath = basePath;
            _memoryPath = Path.Combine(basePath, MEMORY_DIR, SEMANTIC_SUBDIR);
            _cachedMemories = new List<SemanticMemoryEntry>();

            Directory.CreateDirectory(_memoryPath);
            LoadCachedMemories();
        }

        public async Task StoreSemanticMemoryAsync(string content, string category, string[] keywords)
        {
            SemanticMemoryEntry entry = new SemanticMemoryEntry
            {
                Content = content,
                Category = category,
                Keywords = keywords,
                CreatedUtc = DateTime.UtcNow
            };

            _cachedMemories.Add(entry);
            await PersistMemoryAsync(entry);
        }

        public async Task<List<string>> FindSimilarMemoriesAsync(string query, int count = 5)
        {
            string normalized = query.ToLower();
            string[] queryKeywords = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            List<string> scored = _cachedMemories
                .Select(m => new { Entry = m, Score = CalculateSimilarity(m.Keywords, queryKeywords) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(count)
                .Select(x => x.Entry.Content)
                .ToList();

            return await Task.FromResult(scored);
        }

        public async Task LinkMemoriesAsync(string memoryId1, string memoryId2, string relationship)
        {
            SemanticMemoryEntry? memory1 = _cachedMemories.FirstOrDefault(m => m.Id == memoryId1);
            SemanticMemoryEntry? memory2 = _cachedMemories.FirstOrDefault(m => m.Id == memoryId2);

            bool memory1Exists = memory1 != null;
            if (memory1Exists && !memory1.RelatedMemoryIds.Contains(memoryId2))
            {
                List<string> newRelated = memory1.RelatedMemoryIds.ToList();
                newRelated.Add(memoryId2);
                memory1.RelatedMemoryIds = newRelated.ToArray();
                await PersistMemoryAsync(memory1);
            }

            bool memory2Exists = memory2 != null;
            if (memory2Exists && !memory2.RelatedMemoryIds.Contains(memoryId1))
            {
                List<string> newRelated = memory2.RelatedMemoryIds.ToList();
                newRelated.Add(memoryId1);
                memory2.RelatedMemoryIds = newRelated.ToArray();
                await PersistMemoryAsync(memory2);
            }

            await Task.CompletedTask;
        }

        public async Task<List<string>> GetRelatedMemoriesAsync(string topic)
        {
            List<string> related = _cachedMemories
                .Where(m => m.Keywords.Contains(topic, StringComparer.OrdinalIgnoreCase) || m.Content.Contains(topic, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.RelevanceScore)
                .Select(m => m.Content)
                .ToList();

            return await Task.FromResult(related);
        }

        public async Task EvolveMemoryAsync(string memoryId, string updatedContent)
        {
            SemanticMemoryEntry? memory = _cachedMemories.FirstOrDefault(m => m.Id == memoryId);

            bool memoryExists = memory != null;
            if (memoryExists)
            {
                memory.Content = updatedContent;
                memory.LastAccessedUtc = DateTime.UtcNow;
                memory.AccessCount++;
                await PersistMemoryAsync(memory);
            }

            await Task.CompletedTask;
        }

        public async Task RebuildSemanticIndexAsync()
        {
            // Recalculate relevance scores and relationships
            for (int i = 0; i < _cachedMemories.Count; i++)
            {
                SemanticMemoryEntry memory = _cachedMemories[i];
                double daysSinceCreated = (DateTime.UtcNow - memory.CreatedUtc).TotalDays;
                double baseScore = 1.0 - (daysSinceCreated / DAYS_PER_YEAR) * BASE_SCORE_FACTOR;
                double accessBonus = Math.Min(memory.AccessCount * ACCESS_BONUS_FACTOR, MAX_ACCESS_BONUS);
                memory.RelevanceScore = Math.Clamp(baseScore + accessBonus, MIN_RELEVANCE_SCORE, MAX_RELEVANCE_SCORE);
            }

            // Save all updated memories
            for (int i = 0; i < _cachedMemories.Count; i++)
            {
                await PersistMemoryAsync(_cachedMemories[i]);
            }

            await Task.CompletedTask;
        }

        private double CalculateSimilarity(string[] keywords1, string[] keywords2)
        {
            bool keywords1Empty = keywords1.Length == 0;
            bool keywords2Empty = keywords2.Length == 0;
            if (keywords1Empty || keywords2Empty)
            {
                return 0.0;
            }

            int matches = keywords1.Intersect(keywords2, StringComparer.OrdinalIgnoreCase).Count();
            int maxKeywords = Math.Max(keywords1.Length, keywords2.Length);
            double similarity = (double)matches / maxKeywords;

            return similarity;
        }

        private void LoadCachedMemories()
        {
            string[] memoryFiles = Directory.GetFiles(_memoryPath, JSON_PATTERN);

            for (int i = 0; i < memoryFiles.Length; i++)
            {
                string file = memoryFiles[i];
                try
                {
                    string json = File.ReadAllText(file);
                    SemanticMemoryEntry? memory = JsonSerializer.Deserialize<SemanticMemoryEntry>(json);

                    bool memoryValid = memory != null;
                    if (memoryValid)
                    {
                        _cachedMemories.Add(memory);
                    }
                }
                catch
                {
                    // Silently skip corrupted files
                }
            }
        }

        private async Task PersistMemoryAsync(SemanticMemoryEntry entry)
        {
            string filePath = Path.Combine(_memoryPath, $"{entry.Id}{JSON_EXTENSION}");
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(entry, options);

            await File.WriteAllTextAsync(filePath, json);
        }
    }
}
