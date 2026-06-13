using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Represents Alissa's persona and current context state.
    /// Fields remain empty by default and are extended as interactions progress.
    /// </summary>
    public class PersonaModel
    {
        /// <summary>
        /// Version of the persona schema.
        /// </summary>
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// When this persona was created.
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this persona was last updated.
        /// </summary>
        [JsonPropertyName("updatedUtc")]
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Current user information (empty initially, populated through conversation).
        /// </summary>
        [JsonPropertyName("current_user")]
        public UserContext CurrentUser { get; set; } = new();

        /// <summary>
        /// Alissa's current appearance/presentation (empty initially).
        /// </summary>
        [JsonPropertyName("appearance")]
        public AppearanceContext Appearance { get; set; } = new();

        /// <summary>
        /// Current context about code being discussed (empty initially).
        /// </summary>
        [JsonPropertyName("current_code")]
        public CodeContext CurrentCode { get; set; } = new();

        /// <summary>
        /// Additional custom fields for future expansion.
        /// </summary>
        [JsonPropertyName("custom_fields")]
        public Dictionary<string, string> CustomFields { get; set; } = new();
    }

    /// <summary>
    /// User context within Alissa's persona.
    /// </summary>
    public class UserContext
    {
        /// <summary>
        /// User's name if known.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// User's preferences.
        /// </summary>
        [JsonPropertyName("preferences")]
        public List<string> Preferences { get; set; } = new();

        /// <summary>
        /// Known information about the user.
        /// </summary>
        [JsonPropertyName("known_info")]
        public Dictionary<string, string> KnownInfo { get; set; } = new();
    }

    /// <summary>
    /// Alissa's appearance context.
    /// </summary>
    public class AppearanceContext
    {
        /// <summary>
        /// Description of current visual presentation.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Mood or emotional presentation.
        /// </summary>
        [JsonPropertyName("mood")]
        public string Mood { get; set; } = string.Empty;

        /// <summary>
        /// Other appearance-related attributes.
        /// </summary>
        [JsonPropertyName("attributes")]
        public Dictionary<string, string> Attributes { get; set; } = new();
    }

    /// <summary>
    /// Current code context being discussed.
    /// </summary>
    public class CodeContext
    {
        /// <summary>
        /// Name or identifier of the code being worked on.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Language of the code (C#, C++, etc).
        /// </summary>
        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Problem or task being addressed.
        /// </summary>
        [JsonPropertyName("task")]
        public string Task { get; set; } = string.Empty;

        /// <summary>
        /// Code snippet (if small enough to track).
        /// </summary>
        [JsonPropertyName("snippet")]
        public string Snippet { get; set; } = string.Empty;

        /// <summary>
        /// Related concepts or patterns being discussed.
        /// </summary>
        [JsonPropertyName("related_concepts")]
        public List<string> RelatedConcepts { get; set; } = new();
    }
}
