using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Represents a summary of a conversation session with metadata for memory extraction.
    /// </summary>
    public class ConversationSummary
    {
        /// <summary>
        /// Unique identifier for this summary.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Version of the summary schema.
        /// </summary>
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// The actual summary text.
        /// </summary>
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Key highlights from the conversation.
        /// </summary>
        [JsonPropertyName("highlights")]
        public List<string> Highlights { get; set; } = new();

        /// <summary>
        /// When this summary was created.
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Legacy compatibility field. Use CreatedUtc instead.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp
        {
            get => CreatedUtc;
            set => CreatedUtc = value;
        }

        /// <summary>
        /// Number of messages in the original conversation.
        /// </summary>
        [JsonPropertyName("messageCount")]
        public int MessageCount { get; set; }

        /// <summary>
        /// Topics discussed in this conversation.
        /// </summary>
        [JsonPropertyName("topics")]
        public List<string> Topics { get; set; } = new();

        /// <summary>
        /// Tags for easy filtering.
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// When memory extraction was performed on this summary.
        /// </summary>
        [JsonPropertyName("extractedUtc")]
        public DateTime? ExtractedUtc { get; set; }

        /// <summary>
        /// The memory extraction result if available.
        /// </summary>
        [JsonPropertyName("extraction")]
        public MemoryExtractionResult? Extraction { get; set; }

        /// <summary>
        /// Whether this summary has been processed for memory extraction.
        /// </summary>
        [JsonPropertyName("isProcessed")]
        public bool IsProcessed { get; set; }

        public override string ToString()
        {
            return $"[{CreatedUtc:yyyy-MM-dd HH:mm}] {Summary.Substring(0, Math.Min(50, Summary.Length))}... ({MessageCount} msgs)";
        }
    }
}
