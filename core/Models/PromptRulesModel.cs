using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Configuration for prompt construction and token management.
    /// </summary>
    public class PromptRulesModel
    {
        /// <summary>
        /// Maximum tokens allowed in the final prompt.
        /// </summary>
        [JsonPropertyName("maxPromptTokens")]
        public int MaxPromptTokens { get; set; } = 4096;

        /// <summary>
        /// Maximum number of messages to include in session context.
        /// </summary>
        [JsonPropertyName("maxSessionMessages")]
        public int MaxSessionMessages { get; set; } = 10;

        /// <summary>
        /// Maximum number of memory entries to inject into prompt.
        /// </summary>
        [JsonPropertyName("maxMemoryEntries")]
        public int MaxMemoryEntries { get; set; } = 15;

        /// <summary>
        /// Priority order for trimming when token budget exceeded.
        /// Lower index = higher priority (never trimmed).
        /// Order: Identity > UserProfile > Facts > RecentContext > MediumTermMemory > Skills > InternalNotes > SystemLearnings > SessionCache
        /// </summary>
        [JsonPropertyName("trimPriority")]
        public List<string> TrimPriority { get; set; } = new()
        {
            "Identity",
            "UserProfile",
            "Facts",
            "RecentContext",
            "MediumTermMemory",
            "Skills",
            "InternalNotes",
            "SystemLearnings",
            "SessionCache"
        };

        /// <summary>
        /// Whether to include medium-term memory in prompts.
        /// </summary>
        [JsonPropertyName("includeMediumTermMemory")]
        public bool IncludeMediumTermMemory { get; set; } = false;

        /// <summary>
        /// Whether to include persona fields in prompts.
        /// </summary>
        [JsonPropertyName("includePersonaFields")]
        public bool IncludePersonaFields { get; set; } = true;

        /// <summary>
        /// Approximate tokens per line of text (for budgeting).
        /// </summary>
        [JsonPropertyName("tokensPerLine")]
        public int TokensPerLine { get; set; } = 4;
    }
}
