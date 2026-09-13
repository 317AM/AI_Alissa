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
        // Constants
        private const string MEMORY_DIR = "memory";
        private const string INDEX_FILE = "memory_index.json";
        private const string USER_PROFILE_CATEGORY = "UserProfile";
        private const string FACT_CATEGORY = "Fact";
        private const string SKILL_CATEGORY = "Skill";
        private const string SYSTEM_LEARNING_CATEGORY = "SystemLearning";
        private const string MEDIUM_TERM_CATEGORY = "MediumTermMemory";
        private const int DEFAULT_MAX_RESULTS = 10;
        private const float MIN_SCORE_THRESHOLD = 0;

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
        private string IndexFilePath => Path.Combine(_basePath, MEMORY_DIR, INDEX_FILE);

        /// <summary>
        /// Builds the memory index from all memory sources.
        /// This is intentionally inefficient (loads everything, rebuilds from scratch).
        /// </summary>
        public MemoryIndex BuildIndex()
        {
            MemoryIndex index = new MemoryIndex
            {
                LastBuiltUtc = DateTime.UtcNow
            };

            HashSet<string> allTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> allTopics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
            bool shouldRebuild = _rules.RebuildOnAccess || _cachedIndex == null;
            if (shouldRebuild)
            {
                return BuildIndex();
            }

            return _cachedIndex;
        }

        /// <summary>
        /// Searches the index for entries matching a query.
        /// Uses heuristic scoring to find relevant memories.
        /// </summary>
        public List<MemoryIndexEntry> Search(string query, int maxResults = DEFAULT_MAX_RESULTS)
        {
            MemoryIndex index = GetIndex();

            bool hasEntries = index.Entries.Count > 0;
            if (!hasEntries)
            {
                return new List<MemoryIndexEntry>();
            }

            string[] queryWords = query.ToLower().Split(new[] { ' ', ',', '.', ';' }, StringSplitOptions.RemoveEmptyEntries);

            List<MemoryIndexEntry> results = index.Entries.Values
                .AsParallel()
                .Select(entry => new
                {
                    Entry = entry,
                    Score = CalculateRelevanceScore(entry, queryWords)
                })
                .Where(x => x.Score > MIN_SCORE_THRESHOLD)
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .Select(x => x.Entry)
                .ToList();

            return results;
        }

        /// <summary>
        /// Gets entries by tag from the index.
        /// </summary>
        public List<MemoryIndexEntry> GetByTag(string tag, int maxResults = DEFAULT_MAX_RESULTS)
        {
            MemoryIndex index = GetIndex();

            List<MemoryIndexEntry> results = index.Entries.Values
                .Where(e => e.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(e => e.Relevance)
                .Take(maxResults)
                .ToList();

            return results;
        }

        /// <summary>
        /// Gets all entries sorted by relevance.
        /// </summary>
        public List<MemoryIndexEntry> GetAllSorted(int maxResults = DEFAULT_MAX_RESULTS)
        {
            MemoryIndex index = GetIndex();

            List<MemoryIndexEntry> results = index.Entries.Values
                .OrderByDescending(e => e.Relevance)
                .ThenByDescending(e => e.Timestamp)
                .Take(maxResults)
                .ToList();

            return results;
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
                List<MemoryEntry> profile = _memoryManager.LoadUserProfile();
                for (int i = 0; i < profile.Count; i++)
                {
                    AddToIndex(index, profile[i], USER_PROFILE_CATEGORY, allTags);
                }

                List<MemoryEntry> facts = _memoryManager.LoadFacts();
                for (int i = 0; i < facts.Count; i++)
                {
                    AddToIndex(index, facts[i], FACT_CATEGORY, allTags);
                }

                List<MemoryEntry> skills = _memoryManager.LoadSkills();
                for (int i = 0; i < skills.Count; i++)
                {
                    AddToIndex(index, skills[i], SKILL_CATEGORY, allTags);
                }

                List<MemoryEntry> learnings = _memoryManager.LoadSystemLearnings();
                for (int i = 0; i < learnings.Count; i++)
                {
                    AddToIndex(index, learnings[i], SYSTEM_LEARNING_CATEGORY, allTags);
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
            MemoryIndexEntry indexEntry = new MemoryIndexEntry
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
