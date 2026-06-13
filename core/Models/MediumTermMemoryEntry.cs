using System;
using System.Collections.Generic;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Represents a medium-term memory entry for recent contextual information.
    /// Acts as a bridge between short-term session cache and long-term memory.
    /// </summary>
    public class MediumTermMemoryEntry
    {
        /// <summary>
        /// Unique session identifier this memory belongs to.
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// When this memory was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Summary of the conversation session.
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// How relevant this memory is to current context (0.0 to 1.0).
        /// </summary>
        public double RelevanceScore { get; set; } = 0.5;

        /// <summary>
        /// Main topics discussed in this session.
        /// </summary>
        public List<string> Topics { get; set; } = new();

        /// <summary>
        /// Tags for easy filtering and recall.
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Number of messages in the original session.
        /// </summary>
        public int MessageCount { get; set; }

        /// <summary>
        /// Optional: Important highlights from the session.
        /// </summary>
        public List<string> Highlights { get; set; } = new();

        public MediumTermMemoryEntry() { }

        public MediumTermMemoryEntry(string sessionId, string summary, int messageCount)
        {
            SessionId = sessionId;
            Summary = summary;
            MessageCount = messageCount;
        }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm}] {Summary.Substring(0, Math.Min(50, Summary.Length))}... (Topics: {string.Join(", ", Topics)})";
        }
    }
}
