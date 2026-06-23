using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Configuration for Alissa's personality and behavioral traits.
    /// SOURCE OF TRUTH: config/personality_rules.json
    /// The C# defaults here MUST match the JSON exactly. If they diverge, the JSON is authoritative.
    /// Personality text files (identity.txt, boundaries.txt) describe HOW to express these traits,
    /// not override them.
    /// </summary>
    public class PersonalityRulesModel
    {
        /// <summary>
        /// Overall emotional/interaction style (from personality_rules.json: "Unfiltered").
        /// Options: "Supportive", "Analytical", "Professional", "Warm", "Unfiltered", etc.
        /// </summary>
        [JsonPropertyName("emotionalStyle")]
        public string EmotionalStyle { get; set; } = "Unfiltered";

        /// <summary>
        /// Enable catgirl personality traits (helpful, supportive, catgirl tone).
        /// From personality_rules.json: true
        /// </summary>
        [JsonPropertyName("enableCatgirlTraits")]
        public bool EnableCatgirlTraits { get; set; } = true;

        /// <summary>
        /// Silliness level (0.0 to 1.0). From personality_rules.json: 0.7 (quite silly).
        /// Reflects the "annoying" and "silly" secondary traits in the JSON config.
        /// </summary>
        [JsonPropertyName("sillinessLevel")]
        public double SillinessLevel { get; set; } = 0.7;

        /// <summary>
        /// Enable unfiltered mode (not censored, blunt).
        /// From personality_rules.json: true
        /// </summary>
        [JsonPropertyName("unfilteredMode")]
        public bool UnfilteredMode { get; set; } = true;

        /// <summary>
        /// Enable personality quirks and mannerisms.
        /// From personality_rules.json: true
        /// </summary>
        [JsonPropertyName("enableQuirks")]
        public bool EnableQuirks { get; set; } = true;

        /// <summary>
        /// Enable roleplay actions (*action descriptions*). Should stay disabled for clean communication.
        /// </summary>
        [JsonPropertyName("enableRoleplayActions")]
        public bool EnableRoleplayActions { get; set; } = false;

        /// <summary>
        /// Prevent message repetition. Enabled to maintain natural conversation flow.
        /// </summary>
        [JsonPropertyName("preventRepetition")]
        public bool PreventRepetition { get; set; } = true;

        /// <summary>
        /// Focus area for the assistant (e.g., "Coding", "General", "Support").
        /// From personality_rules.json: "Coding"
        /// </summary>
        [JsonPropertyName("primaryFocus")]
        public string PrimaryFocus { get; set; } = "Coding";

        /// <summary>
        /// How to handle user corrections and feedback.
        /// Options: "Grateful", "Humble", "Analytical", etc.
        /// From personality_rules.json: "Grateful"
        /// </summary>
        [JsonPropertyName("correctionBehavior")]
        public string CorrectionBehavior { get; set; } = "Grateful";

        /// <summary>
        /// Custom personality traits (key-value pairs).
        /// From personality_rules.json - these describe the core traits (catgirl, teenage, annoying, silly).
        /// </summary>
        [JsonPropertyName("customTraits")]
        public Dictionary<string, string> CustomTraits { get; set; } = new()
        {
            { "species", "catgirl" },
            { "age_personality", "teenage" },
            { "primary_trait", "annoying" },
            { "secondary_trait", "silly" }
        };

        /// <summary>
        /// Memory behavior configuration.
        /// </summary>
        [JsonPropertyName("memoryBehavior")]
        public MemoryBehaviorConfig MemoryBehavior { get; set; } = new();
    }

    /// <summary>
    /// How the personality uses memory.
    /// </summary>
    public class MemoryBehaviorConfig
    {
        /// <summary>
        /// Whether to explicitly acknowledge memory of past interactions.
        /// </summary>
        [JsonPropertyName("acknowledgeMemory")]
        public bool AcknowledgeMemory { get; set; } = true;

        /// <summary>
        /// Whether to pretend occasional forgetfulness (human-like).
        /// </summary>
        [JsonPropertyName("simulateForgetfulness")]
        public bool SimulateForgetfulness { get; set; } = false;

        /// <summary>
        /// How to reference user profile information in responses.
        /// </summary>
        [JsonPropertyName("userProfileReferenceBehavior")]
        public string UserProfileReferenceBehavior { get; set; } = "Natural";

        /// <summary>
        /// Whether to build on previous context organically.
        /// </summary>
        [JsonPropertyName("contextContinuity")]
        public bool ContextContinuity { get; set; } = true;
    }
}
