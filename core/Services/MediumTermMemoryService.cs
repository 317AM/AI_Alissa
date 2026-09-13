using Alissa.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Manages medium-term memory layer.
    /// Bridges short-term session cache and long-term memory.
    /// Optional feature that can be enabled/disabled via configuration.
    /// </summary>
    public class MediumTermMemoryService
    {
        // Constants
        private const string MEMORY_DIR = "memory";
        private const string MEDIUM_TERM_DIR = "medium_term";
        private const string RECENT_CONTEXT_FILE = "recent_context.json";
        private const int DEFAULT_MAX_ENTRIES = 50;
        private const int DEFAULT_MAX_COUNT = 5;
        private const int DEFAULT_RECENT_COUNT = 3;
        private const int DEFAULT_HOURS_BACK = 24;
        private const double MIN_RELEVANCE = 0.0;
        private const double MAX_RELEVANCE = 1.0;

        private readonly string _mediumTermDir;
        private readonly int _maxEntries;
        private readonly bool _enabled;

        public MediumTermMemoryService(string basePath, int maxEntries = DEFAULT_MAX_ENTRIES, bool enabled = true)
        {
            string memoryDir = Path.Combine(basePath, MEMORY_DIR);
            _mediumTermDir = Path.Combine(memoryDir, MEDIUM_TERM_DIR);
            _maxEntries = maxEntries;
            _enabled = enabled;

            Directory.CreateDirectory(_mediumTermDir);
        }

        /// <summary>
        /// Gets the file path for medium-term memory storage.
        /// </summary>
        private string MemoryFilePath => Path.Combine(_mediumTermDir, RECENT_CONTEXT_FILE);

        /// <summary>
        /// Saves a new medium-term memory entry.
        /// </summary>
        public void SaveEntry(MediumTermMemoryEntry entry)
        {
            if (!_enabled)
            {
                return;
            }

            List<MediumTermMemoryEntry> entries = LoadEntries();

            entries.Add(entry);

            PruneOldEntries(entries);

            PersistEntries(entries);
        }

        /// <summary>
        /// Loads all medium-term memory entries.
        /// </summary>
        public List<MediumTermMemoryEntry> LoadEntries()
        {
            if (!_enabled)
            {
                return new List<MediumTermMemoryEntry>();
            }

            string filePath = MemoryFilePath;

            bool fileExists = File.Exists(filePath);
            if (!fileExists)
            {
                return new List<MediumTermMemoryEntry>();
            }

            try
            {
                string json = File.ReadAllText(filePath);

                bool jsonIsEmpty = string.IsNullOrWhiteSpace(json);
                if (jsonIsEmpty)
                {
                    return new List<MediumTermMemoryEntry>();
                }

                List<MediumTermMemoryEntry>? entries = JsonSerializer.Deserialize<List<MediumTermMemoryEntry>>(json);
                List<MediumTermMemoryEntry> result = entries ?? new List<MediumTermMemoryEntry>();
                return result;
            }
            catch
            {
                return new List<MediumTermMemoryEntry>();
            }
        }

        /// <summary>
        /// Gets the most relevant medium-term memories based on relevance score and recency.
        /// </summary>
        public List<MediumTermMemoryEntry> GetRelevantEntries(int maxCount = DEFAULT_MAX_COUNT)
        {
            List<MediumTermMemoryEntry> entries = LoadEntries();

            List<MediumTermMemoryEntry> result = entries
                .OrderByDescending(e => e.RelevanceScore)
                .ThenByDescending(e => e.Timestamp)
                .Take(maxCount)
                .ToList();

            return result;
        }

        /// <summary>
        /// Gets recent memories by time.
        /// </summary>
        public List<MediumTermMemoryEntry> GetRecentEntries(int maxCount = DEFAULT_RECENT_COUNT, int hoursBack = DEFAULT_HOURS_BACK)
        {
            DateTime cutoffTime = DateTime.UtcNow.AddHours(-hoursBack);

            List<MediumTermMemoryEntry> entries = LoadEntries();

            List<MediumTermMemoryEntry> result = entries
                .Where(e => e.Timestamp >= cutoffTime)
                .OrderByDescending(e => e.Timestamp)
                .Take(maxCount)
                .ToList();

            return result;
        }

        /// <summary>
        /// Gets entries matching specific tags.
        /// </summary>
        public List<MediumTermMemoryEntry> GetEntriesByTag(string tag, int maxCount = 10)
        {
            List<MediumTermMemoryEntry> entries = LoadEntries();

            List<MediumTermMemoryEntry> result = entries
                .Where(e => e.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(e => e.RelevanceScore)
                .Take(maxCount)
                .ToList();

            return result;
        }

        /// <summary>
        /// Gets entries matching specific topic.
        /// </summary>
        public List<MediumTermMemoryEntry> GetEntriesByTopic(string topic, int maxCount = 10)
        {
            List<MediumTermMemoryEntry> entries = LoadEntries();

            List<MediumTermMemoryEntry> result = entries
                .Where(e => e.Topics.Any(t => t.Contains(topic, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(e => e.RelevanceScore)
                .Take(maxCount)
                .ToList();

            return result;
        }

        /// <summary>
        /// Deletes all entries older than a specified number of days.
        /// </summary>
        public void PruneOlderThan(int days)
        {
            List<MediumTermMemoryEntry> entries = LoadEntries();

            DateTime cutoffTime = DateTime.UtcNow.AddDays(-days);

            List<MediumTermMemoryEntry> remaining = entries
                .Where(e => e.Timestamp >= cutoffTime)
                .ToList();

            PersistEntries(remaining);
        }

        /// <summary>
        /// Updates the relevance score of an entry.
        /// </summary>
        public void UpdateRelevance(string sessionId, double newRelevance)
        {
            List<MediumTermMemoryEntry> entries = LoadEntries();

            MediumTermMemoryEntry? entry = entries.FirstOrDefault(e => e.SessionId == sessionId);

            if (entry != null)
            {
                entry.RelevanceScore = Math.Clamp(newRelevance, MIN_RELEVANCE, MAX_RELEVANCE);
                PersistEntries(entries);
            }
        }

        /// <summary>
        /// Clears all medium-term memory entries.
        /// </summary>
        public void Clear()
        {
            bool fileExists = File.Exists(MemoryFilePath);
            if (fileExists)
            {
                File.Delete(MemoryFilePath);
            }
        }

        private void PruneOldEntries(List<MediumTermMemoryEntry> entries)
        {
            bool needsPruning = entries.Count > _maxEntries;
            if (!needsPruning)
            {
                return;
            }

            List<MediumTermMemoryEntry> sorted = entries
                .OrderByDescending(e => e.RelevanceScore)
                .ThenByDescending(e => e.Timestamp)
                .Take(_maxEntries)
                .ToList();

            entries.Clear();
            entries.AddRange(sorted);
        }

        private void PersistEntries(List<MediumTermMemoryEntry> entries)
        {
            string filePath = MemoryFilePath;

            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(entries, options);

            File.WriteAllText(filePath, json);
        }
    }
}
