using System;
using System.Text.Json.Serialization;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Represents a single thought or reasoning stored by Alissa.
    /// </summary>
    public class ThoughtEntry
    {
        /// <summary>
        /// Unique identifier for this thought.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The thought content.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Category of thought (reasoning, analysis, decision, reflection, etc.).
        /// </summary>
        [JsonPropertyName("category")]
        public string Category { get; set; } = "general";

        /// <summary>
        /// When the thought was created.
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Session ID associated with this thought.
        /// </summary>
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Confidence level (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; } = 1.0;

        /// <summary>
        /// Related memory IDs or topics.
        /// </summary>
        [JsonPropertyName("relatedTopics")]
        public string[] RelatedTopics { get; set; } = Array.Empty<string>();
    }
}
