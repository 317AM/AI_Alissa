using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Configuration for the lazy, "human-like" memory indexing system.
    /// Designed to be inefficient and forgetful, not the easy way.
    /// </summary>
    public class IndexingRulesModel
    {
        /// <summary>
        /// Rebuild index on every memory access (lazy, inefficient).
        /// </summary>
        [JsonPropertyName("rebuildOnAccess")]
        public bool RebuildOnAccess { get; set; } = true;

        /// <summary>
        /// Use partial indexing (don't index everything, be selective).
        /// </summary>
        [JsonPropertyName("usePartialIndexing")]
        public bool UsePartialIndexing { get; set; } = true;

        /// <summary>
        /// Apply heuristic forgetting (decay relevance over time, lose old memories).
        /// </summary>
        [JsonPropertyName("applyForgetfulness")]
        public bool ApplyForgetfulness { get; set; } = true;

        /// <summary>
        /// Days after which memory relevance starts decaying.
        /// </summary>
        [JsonPropertyName("decayAfterDays")]
        public int DecayAfterDays { get; set; } = 7;

        /// <summary>
        /// Per-day relevance decay rate (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("decayRatePerDay")]
        public double DecayRatePerDay { get; set; } = 0.05;

        /// <summary>
        /// Minimum relevance score before a memory is considered "forgotten" (removed from index).
        /// </summary>
        [JsonPropertyName("forgettingThreshold")]
        public double ForgettingThreshold { get; set; } = 0.1;

        /// <summary>
        /// When rebuilding, only index memories matching these tags/categories.
        /// Empty list = no filtering (index all).
        /// </summary>
        [JsonPropertyName("indexingFilters")]
        public List<string> IndexingFilters { get; set; } = new();

        /// <summary>
        /// Maximum number of memory entries to keep in active index.
        /// Older/less relevant memories are pruned.
        /// </summary>
        [JsonPropertyName("maxIndexSize")]
        public int MaxIndexSize { get; set; } = 500;

        /// <summary>
        /// Instead of using timestamp proximity, use heuristic scoring.
        /// This makes memory lookup "dumber" but more interesting.
        /// </summary>
        [JsonPropertyName("useHeuristicScoring")]
        public bool UseHeuristicScoring { get; set; } = true;

        /// <summary>
        /// Number of times a memory must be accessed before it's "consolidated" (kept longer).
        /// </summary>
        [JsonPropertyName("accessCountForConsolidation")]
        public int AccessCountForConsolidation { get; set; } = 3;
    }
}
