using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Response from code integration API endpoints.
    /// </summary>
    public class CodeContextResponse
    {
        /// <summary>
        /// Whether the operation was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Human-readable message describing the result or error.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Suggested code (for fix/complete/test tasks).
        /// </summary>
        [JsonPropertyName("suggestedCode")]
        public string? SuggestedCode { get; set; }

        /// <summary>
        /// List of notes or analysis points (for explain/review tasks).
        /// </summary>
        [JsonPropertyName("notes")]
        public List<string> Notes { get; set; } = new();

        /// <summary>
        /// Path to backup file (for apply operations).
        /// </summary>
        [JsonPropertyName("backupPath")]
        public string? BackupPath { get; set; }

        /// <summary>
        /// Timestamp when this response was generated.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
