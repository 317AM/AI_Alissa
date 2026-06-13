using System;
using System.Text.Json.Serialization;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Represents a semantic memory entry with relational metadata.
    /// </summary>
    public class SemanticMemoryEntry
    {
        /// <summary>
        /// Unique identifier.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Memory content.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Category (fact, skill, learning, profile, etc.).
        /// </summary>
        [JsonPropertyName("category")]
        public string Category { get; set; } = "general";

        /// <summary>
        /// Keywords for semantic search.
        /// </summary>
        [JsonPropertyName("keywords")]
        public string[] Keywords { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Created timestamp.
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last accessed timestamp.
        /// </summary>
        [JsonPropertyName("lastAccessedUtc")]
        public DateTime LastAccessedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Relevance score (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("relevanceScore")]
        public double RelevanceScore { get; set; } = 1.0;

        /// <summary>
        /// IDs of related memories.
        /// </summary>
        [JsonPropertyName("relatedMemoryIds")]
        public string[] RelatedMemoryIds { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Access count for determining importance.
        /// </summary>
        [JsonPropertyName("accessCount")]
        public int AccessCount { get; set; } = 0;
    }
}
