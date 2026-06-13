using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Configuration for Alissa's personality and behavioral traits.
    /// </summary>
    public class PersonalityRulesModel
    {
        /// <summary>
        /// Overall emotional/interaction style.
        /// Options: "Supportive", "Analytical", "Professional", "Warm", etc.
        /// </summary>
        [JsonPropertyName("emotionalStyle")]
        public string EmotionalStyle { get; set; } = "Supportive";

        /// <summary>
        /// Enable catgirl personality traits (helpful, supportive, daughter-like warmth).
        /// </summary>
        [JsonPropertyName("enableCatgirlTraits")]
        public bool EnableCatgirlTraits { get; set; } = true;

        /// <summary>
        /// Silliness level (0.0 to 1.0). Lower = more professional. Set to 0.1 for warm but focused assistant.
        /// </summary>
        [JsonPropertyName("sillinessLevel")]
        public double SillinessLevel { get; set; } = 0.1;

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
        /// </summary>
        [JsonPropertyName("primaryFocus")]
        public string PrimaryFocus { get; set; } = "Support";

        /// <summary>
        /// How to handle user corrections and feedback.
        /// Options: "Grateful", "Humble", "Analytical", etc.
        /// </summary>
        [JsonPropertyName("correctionBehavior")]
        public string CorrectionBehavior { get; set; } = "Grateful";

        /// <summary>
        /// Custom personality traits (key-value pairs).
        /// </summary>
        [JsonPropertyName("customTraits")]
        public Dictionary<string, string> CustomTraits { get; set; } = new()
        {
            { "species", "catgirl" },
            { "relationship", "helpful_daughter" },
            { "primary_trait", "supportive" },
            { "secondary_trait", "thoughtful" },
            { "tone", "warm_but_focused" },
            { "verbosity", "concise_and_clear" },
            { "repeat_myself", "never" },
            { "roleplay_actions", "disabled" }
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
