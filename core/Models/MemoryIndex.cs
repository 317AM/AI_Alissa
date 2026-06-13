using System;
using System.Collections.Generic;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Index for fast memory lookup across all memory types.
    /// Built lazily to simulate human-like forgetting and inefficiency.
    /// </summary>
    public class MemoryIndex
    {
        /// <summary>
        /// Version of the index format.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// When this index was last built.
        /// </summary>
        public DateTime LastBuiltUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Maps from memory keys to their locations and relevance.
        /// </summary>
        public Dictionary<string, MemoryIndexEntry> Entries { get; set; } = new();

        /// <summary>
        /// Total number of indexed memories.
        /// </summary>
        public int TotalMemories { get; set; }

        /// <summary>
        /// Tags that appear in memory (for filtering).
        /// </summary>
        public List<string> AllTags { get; set; } = new();

        /// <summary>
        /// Topics that appear in memory (for context).
        /// </summary>
        public List<string> AllTopics { get; set; } = new();

        public override string ToString()
        {
            return $"Index: {TotalMemories} memories, {Entries.Count} keys, built {LastBuiltUtc:yyyy-MM-dd HH:mm:ss}";
        }
    }

    /// <summary>
    /// Single entry in the memory index.
    /// </summary>
    public class MemoryIndexEntry
    {
        /// <summary>
        /// Key of the memory entry.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Category this memory belongs to (UserProfile, Fact, Skill, SystemLearning, etc).
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Relevance score for sorting.
        /// </summary>
        public double Relevance { get; set; } = 0.5;

        /// <summary>
        /// When this memory was created/updated.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Is this a core memory (always kept)?
        /// </summary>
        public bool IsCoreMemory { get; set; }

        /// <summary>
        /// Tags associated with this memory.
        /// </summary>
        public List<string> Tags { get; set; } = new();

        public override string ToString()
        {
            return $"{Category}: {Key} (Relevance: {Relevance:F2})";
        }
    }
}
