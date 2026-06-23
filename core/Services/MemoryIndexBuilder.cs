using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Builds and maintains a lazy, "human-like" memory index.
    /// Designed to be inefficient and forgetful, not the easy way.
    /// 
    /// This index:
    /// - Rebuilds on access (lazy, wasteful)
    /// - Uses heuristic scoring (not optimal)
    /// - Applies relevance decay (forgets things)
    /// - Keeps limited history (doesn't remember everything)
    /// </summary>
    public class MemoryIndexBuilder
    {
        private readonly string _basePath;
        private readonly IndexingRulesModel _rules;
        private readonly IMemoryManager _memoryManager;
        private MemoryIndex? _cachedIndex;
        private DateTime _lastIndexBuild = DateTime.MinValue;

        public MemoryIndexBuilder(string basePath, IndexingRulesModel rules, IMemoryManager memoryManager)
        {
            _basePath = basePath;
            _rules = rules;
            _memoryManager = memoryManager;
        }

        /// <summary>
        /// Gets the index file path.
        /// </summary>
        private string IndexFilePath => Path.Combine(_basePath, "memory", "memory_index.json");

        /// <summary>
        /// Builds the memory index from all memory sources.
        /// This is intentionally inefficient (loads everything, rebuilds from scratch).
        /// </summary>
        public MemoryIndex BuildIndex()
        {
            var index = new MemoryIndex
            {
                LastBuiltUtc = DateTime.UtcNow
            };

            var allTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allTopics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            LoadMemoryEntries(index, allTags);
            LoadMediumTermMemories(index, allTags, allTopics);

            index.AllTags = allTags.ToList();
            index.AllTopics = allTopics.ToList();
            index.TotalMemories = index.Entries.Count;

            ApplyForgetfulness(index);

            PersistIndex(index);

            _cachedIndex = index;
            _lastIndexBuild = DateTime.UtcNow;

            return index;
        }

        /// <summary>
        /// Gets the current index, rebuilding if necessary based on configuration.
        /// </summary>
        public MemoryIndex GetIndex()
        {
            if (_rules.RebuildOnAccess || _cachedIndex == null)
            {
                return BuildIndex();
            }

            return _cachedIndex;
        }

        /// <summary>
        /// Searches the index for entries matching a query.
        /// Uses heuristic scoring to find relevant memories.
        /// </summary>
        public List<MemoryIndexEntry> Search(string query, int maxResults = 10)
        {
            var index = GetIndex();

            if (index.Entries.Count == 0)
            {
                return new List<MemoryIndexEntry>();
            }

            var queryWords = query.ToLower().Split(new[] { ' ', ',', '.', ';' }, StringSplitOptions.RemoveEmptyEntries);

            var results = index.Entries.Values
                .AsParallel()
                .Select(entry => new
                {
                    Entry = entry,
                    Score = CalculateRelevanceScore(entry, queryWords)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .Select(x => x.Entry)
                .ToList();

            return results;
        }

        /// <summary>
        /// Gets entries by tag from the index.
        /// </summary>
        public List<MemoryIndexEntry> GetByTag(string tag, int maxResults = 10)
        {
            var index = GetIndex();

            return index.Entries.Values
                .Where(e => e.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(e => e.Relevance)
                .Take(maxResults)
                .ToList();
        }

        /// <summary>
        /// Gets all entries sorted by relevance.
        /// </summary>
        public List<MemoryIndexEntry> GetAllSorted(int maxResults = 10)
        {
            var index = GetIndex();

            return index.Entries.Values
                .OrderByDescending(e => e.Relevance)
                .ThenByDescending(e => e.Timestamp)
                .Take(maxResults)
                .ToList();
        }

        /// <summary>
        /// Clears the index cache to force rebuild on next access.
        /// </summary>
        public void InvalidateCache()
        {
            _cachedIndex = null;
            _lastIndexBuild = DateTime.MinValue;
        }

        private void LoadMemoryEntries(MemoryIndex index, HashSet<string> allTags)
        {
            try
            {
                var profile = _memoryManager.LoadUserProfile();
                foreach (var entry in profile)
                {
                    AddToIndex(index, entry, "UserProfile", allTags);
                }

                var facts = _memoryManager.LoadFacts();
                foreach (var entry in facts)
                {
                    AddToIndex(index, entry, "Fact", allTags);
                }

                var skills = _memoryManager.LoadSkills();
                foreach (var entry in skills)
                {
                    AddToIndex(index, entry, "Skill", allTags);
                }

                var learnings = _memoryManager.LoadSystemLearnings();
                foreach (var entry in learnings)
                {
                    AddToIndex(index, entry, "SystemLearning", allTags);
                }
            }
            catch
            {
                // Silently handle load failures
            }
        }

        private void LoadMediumTermMemories(MemoryIndex index, HashSet<string> allTags, HashSet<string> allTopics)
        {
            try
            {
                var mediumTermService = new MediumTermMemoryService(_basePath, enabled: true);
                var entries = mediumTermService.LoadEntries();

                foreach (var entry in entries)
                {
                    var indexEntry = new MemoryIndexEntry
                    {
                        Key = entry.SessionId,
                        Category = "MediumTermMemory",
                        Relevance = entry.RelevanceScore,
                        Timestamp = entry.Timestamp,
                        Tags = entry.Tags
                    };

                    string indexKey = $"{entry.SessionId}_medium";

                    if (!index.Entries.ContainsKey(indexKey))
                    {
                        index.Entries[indexKey] = indexEntry;
                    }

                    foreach (var tag in entry.Tags)
                    {
                        allTags.Add(tag);
                    }

                    foreach (var topic in entry.Topics)
                    {
                        allTopics.Add(topic);
                    }
                }
            }
            catch
            {
                // Silently handle load failures
            }
        }

        private void AddToIndex(MemoryIndex index, MemoryEntry entry, string category, HashSet<string> allTags)
        {
            var indexEntry = new MemoryIndexEntry
            {
                Key = entry.Key,
                Category = category,
                Relevance = entry.Relevance,
                Timestamp = entry.Timestamp,
                IsCoreMemory = entry.IsCoreMemory,
                Tags = ExtractTags(entry.Value)
            };

            string indexKey = $"{category}_{entry.Key}";

            if (!index.Entries.ContainsKey(indexKey))
            {
                index.Entries[indexKey] = indexEntry;
            }

            foreach (var tag in indexEntry.Tags)
            {
                allTags.Add(tag);
            }
        }

        private void ApplyForgetfulness(MemoryIndex index)
        {
            if (!_rules.ApplyForgetfulness)
            {
                return;
            }

            var now = DateTime.UtcNow;

            var entriesToRemove = new List<string>();

            var nonCoreEntries = index.Entries
                .Where(kvp => !kvp.Value.IsCoreMemory)
                .ToList();

            foreach (var kvp in nonCoreEntries)
            {
                var entry = kvp.Value;

                var ageInDays = (now - entry.Timestamp).TotalDays;

                if (ageInDays > _rules.DecayAfterDays)
                {
                    var decayPeriods = (int)((ageInDays - _rules.DecayAfterDays) / 1.0);
                    var decayAmount = Math.Pow(1.0 - _rules.DecayRatePerDay, decayPeriods);
                    entry.Relevance *= decayAmount;

                    if (entry.Relevance < _rules.ForgettingThreshold)
                    {
                        entriesToRemove.Add(kvp.Key);
                    }
                }
            }

            foreach (var key in entriesToRemove)
            {
                index.Entries.Remove(key);
            }

            if (index.Entries.Count > _rules.MaxIndexSize)
            {
                var sorted = index.Entries.Values
                    .OrderByDescending(e => e.Relevance)
                    .ThenByDescending(e => e.Timestamp)
                    .ToList();

                var keysToKeep = new HashSet<string>();

                for (int i = 0; i < Math.Min(_rules.MaxIndexSize, sorted.Count); i++)
                {
                    var entry = sorted[i];
                    var key = index.Entries.FirstOrDefault(kvp => kvp.Value == entry).Key;
                    if (!string.IsNullOrEmpty(key))
                    {
                        keysToKeep.Add(key);
                    }
                }

                var keysToRemove = index.Entries.Keys.Where(k => !keysToKeep.Contains(k)).ToList();

                foreach (var key in keysToRemove)
                {
                    index.Entries.Remove(key);
                }
            }
        }

        private double CalculateRelevanceScore(MemoryIndexEntry entry, string[] queryWords)
        {
            double score = entry.Relevance;

            var keyWords = entry.Key.ToLower().Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in queryWords)
            {
                if (keyWords.Any(k => k.StartsWith(word)))
                {
                    score += 0.3;
                }
            }

            foreach (var tag in entry.Tags)
            {
                foreach (var word in queryWords)
                {
                    if (tag.Contains(word, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 0.15;
                    }
                }
            }

            var recencyBonus = 1.0 - ((DateTime.UtcNow - entry.Timestamp).TotalDays / 30.0);
            score += Math.Max(0, recencyBonus * 0.1);

            return Math.Min(2.0, score);
        }

        private static List<string> ExtractTags(string text)
        {
            var tags = new List<string>();

            var words = text.ToLower().Split(new[] { ' ', ',', '.', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words.Take(10))
            {
                if (word.Length > 4)
                {
                    tags.Add(word);
                }
            }

            return tags.Distinct().ToList();
        }

        private void PersistIndex(MemoryIndex index)
        {
            try
            {
                string filePath = IndexFilePath;
                string json = JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // Silently fail on persistence
            }
        }
    }
}
