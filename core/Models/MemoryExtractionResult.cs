using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Result of memory extraction from a conversation summary.
    /// Contains structured memory data organized by category.
    /// </summary>
    public class MemoryExtractionResult
    {
        /// <summary>
        /// User profile information extracted from conversation.
        /// </summary>
        [JsonPropertyName("user_profile")]
        public Dictionary<string, string> UserProfile { get; set; } = new();

        /// <summary>
        /// Factual information extracted from conversation.
        /// </summary>
        [JsonPropertyName("facts")]
        public Dictionary<string, string> Facts { get; set; } = new();

        /// <summary>
        /// Skills and capabilities discussed or demonstrated.
        /// </summary>
        [JsonPropertyName("skills")]
        public Dictionary<string, string> Skills { get; set; } = new();

        /// <summary>
        /// System learnings and behavioral improvements.
        /// </summary>
        [JsonPropertyName("system_learnings")]
        public Dictionary<string, string> SystemLearnings { get; set; } = new();

        /// <summary>
        /// Check if extraction has any meaningful data.
        /// </summary>
        public bool HasData
        {
            get
            {
                return UserProfile.Count > 0 || Facts.Count > 0 || Skills.Count > 0 || SystemLearnings.Count > 0;
            }
        }

        public override string ToString()
        {
            return $"Profile: {UserProfile.Count}, Facts: {Facts.Count}, Skills: {Skills.Count}, Learnings: {SystemLearnings.Count}";
        }
    }
}
