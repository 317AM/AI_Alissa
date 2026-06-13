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
        private readonly string _mediumTermDir;
        private readonly int _maxEntries;
        private readonly bool _enabled;

        public MediumTermMemoryService(string basePath, int maxEntries = 50, bool enabled = true)
        {
            string memoryDir = Path.Combine(basePath, "memory");
            _mediumTermDir = Path.Combine(memoryDir, "medium_term");
            _maxEntries = maxEntries;
            _enabled = enabled;

            Directory.CreateDirectory(_mediumTermDir);
        }

        /// <summary>
        /// Gets the file path for medium-term memory storage.
        /// </summary>
        private string MemoryFilePath => Path.Combine(_mediumTermDir, "recent_context.json");

        /// <summary>
        /// Saves a new medium-term memory entry.
        /// </summary>
        public void SaveEntry(MediumTermMemoryEntry entry)
        {
            if (!_enabled)
            {
                return;
            }

            var entries = LoadEntries();

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

            if (!File.Exists(filePath))
            {
                return new List<MediumTermMemoryEntry>();
            }

            try
            {
                string json = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<MediumTermMemoryEntry>();
                }

                var entries = JsonSerializer.Deserialize<List<MediumTermMemoryEntry>>(json);
                return entries ?? new List<MediumTermMemoryEntry>();
            }
            catch
            {
                return new List<MediumTermMemoryEntry>();
            }
        }

        /// <summary>
        /// Gets the most relevant medium-term memories based on relevance score and recency.
        /// </summary>
        public List<MediumTermMemoryEntry> GetRelevantEntries(int maxCount = 5)
        {
            var entries = LoadEntries();

            return entries
                .OrderByDescending(e => e.RelevanceScore)
                .ThenByDescending(e => e.Timestamp)
                .Take(maxCount)
                .ToList();
        }

        /// <summary>
        /// Gets recent memories by time.
        /// </summary>
        public List<MediumTermMemoryEntry> GetRecentEntries(int maxCount = 3, int hoursBack = 24)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-hoursBack);

            var entries = LoadEntries();

            return entries
                .Where(e => e.Timestamp >= cutoffTime)
                .OrderByDescending(e => e.Timestamp)
                .Take(maxCount)
                .ToList();
        }

        /// <summary>
        /// Gets entries matching specific tags.
        /// </summary>
        public List<MediumTermMemoryEntry> GetEntriesByTag(string tag, int maxCount = 10)
        {
            var entries = LoadEntries();

            return entries
                .Where(e => e.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(e => e.RelevanceScore)
                .Take(maxCount)
                .ToList();
        }

        /// <summary>
        /// Gets entries matching specific topic.
        /// </summary>
        public List<MediumTermMemoryEntry> GetEntriesByTopic(string topic, int maxCount = 10)
        {
            var entries = LoadEntries();

            return entries
                .Where(e => e.Topics.Any(t => t.Contains(topic, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(e => e.RelevanceScore)
                .Take(maxCount)
                .ToList();
        }

        /// <summary>
        /// Deletes all entries older than a specified number of days.
        /// </summary>
        public void PruneOlderThan(int days)
        {
            var entries = LoadEntries();

            var cutoffTime = DateTime.UtcNow.AddDays(-days);

            var remaining = entries
                .Where(e => e.Timestamp >= cutoffTime)
                .ToList();

            PersistEntries(remaining);
        }

        /// <summary>
        /// Updates the relevance score of an entry.
        /// </summary>
        public void UpdateRelevance(string sessionId, double newRelevance)
        {
            var entries = LoadEntries();

            var entry = entries.FirstOrDefault(e => e.SessionId == sessionId);

            if (entry != null)
            {
                entry.RelevanceScore = Math.Clamp(newRelevance, 0.0, 1.0);
                PersistEntries(entries);
            }
        }

        /// <summary>
        /// Clears all medium-term memory entries.
        /// </summary>
        public void Clear()
        {
            if (File.Exists(MemoryFilePath))
            {
                File.Delete(MemoryFilePath);
            }
        }

        private void PruneOldEntries(List<MediumTermMemoryEntry> entries)
        {
            if (entries.Count > _maxEntries)
            {
                var sorted = entries
                    .OrderByDescending(e => e.RelevanceScore)
                    .ThenByDescending(e => e.Timestamp)
                    .Take(_maxEntries)
                    .ToList();

                entries.Clear();
                entries.AddRange(sorted);
            }
        }

        private void PersistEntries(List<MediumTermMemoryEntry> entries)
        {
            string filePath = MemoryFilePath;

            string json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(filePath, json);
        }
    }
}
