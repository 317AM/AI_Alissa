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
        private readonly string _basePath;
        private readonly string _memoryPath;
        private List<SemanticMemoryEntry> _cachedMemories;

        public SemanticMemoryService(string basePath)
        {
            _basePath = basePath;
            _memoryPath = Path.Combine(basePath, "memory", "semantic");
            _cachedMemories = new List<SemanticMemoryEntry>();

            Directory.CreateDirectory(_memoryPath);
            LoadCachedMemories();
        }

        public async Task StoreSemanticMemoryAsync(string content, string category, string[] keywords)
        {
            var entry = new SemanticMemoryEntry
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
            var normalized = query.ToLower();
            var queryKeywords = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var scored = _cachedMemories
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
            var memory1 = _cachedMemories.FirstOrDefault(m => m.Id == memoryId1);
            var memory2 = _cachedMemories.FirstOrDefault(m => m.Id == memoryId2);

            if (memory1 != null && !memory1.RelatedMemoryIds.Contains(memoryId2))
            {
                var newRelated = memory1.RelatedMemoryIds.ToList();
                newRelated.Add(memoryId2);
                memory1.RelatedMemoryIds = newRelated.ToArray();
                await PersistMemoryAsync(memory1);
            }

            if (memory2 != null && !memory2.RelatedMemoryIds.Contains(memoryId1))
            {
                var newRelated = memory2.RelatedMemoryIds.ToList();
                newRelated.Add(memoryId1);
                memory2.RelatedMemoryIds = newRelated.ToArray();
                await PersistMemoryAsync(memory2);
            }

            await Task.CompletedTask;
        }

        public async Task<List<string>> GetRelatedMemoriesAsync(string topic)
        {
            var related = _cachedMemories
                .Where(m => m.Keywords.Contains(topic, StringComparer.OrdinalIgnoreCase) || m.Content.Contains(topic, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.RelevanceScore)
                .Select(m => m.Content)
                .ToList();

            return await Task.FromResult(related);
        }

        public async Task EvolveMemoryAsync(string memoryId, string updatedContent)
        {
            var memory = _cachedMemories.FirstOrDefault(m => m.Id == memoryId);

            if (memory != null)
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
            foreach (var memory in _cachedMemories)
            {
                double baseScore = 1.0 - (DateTime.UtcNow - memory.CreatedUtc).TotalDays / 365.0 * 0.1;
                double accessBonus = Math.Min(memory.AccessCount * 0.05, 0.3);
                memory.RelevanceScore = Math.Clamp(baseScore + accessBonus, 0.0, 1.0);
            }

            // Save all updated memories
            foreach (var memory in _cachedMemories)
            {
                await PersistMemoryAsync(memory);
            }

            await Task.CompletedTask;
        }

        private double CalculateSimilarity(string[] keywords1, string[] keywords2)
        {
            if (keywords1.Length == 0 || keywords2.Length == 0)
            {
                return 0.0;
            }

            int matches = keywords1.Intersect(keywords2, StringComparer.OrdinalIgnoreCase).Count();
            double similarity = (double)matches / Math.Max(keywords1.Length, keywords2.Length);
            return similarity;
        }

        private void LoadCachedMemories()
        {
            var memoryFiles = Directory.GetFiles(_memoryPath, "*.json");

            foreach (var file in memoryFiles)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var memory = JsonSerializer.Deserialize<SemanticMemoryEntry>(json);

                    if (memory != null)
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
            string filePath = Path.Combine(_memoryPath, $"{entry.Id}.json");
            string json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(filePath, json);
        }
    }
}
