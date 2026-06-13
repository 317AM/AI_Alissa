using System;
using System.Collections.Generic;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Complete application configuration combining all configuration models.
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Model configuration (name, tokens, timeouts).
        /// </summary>
        public ConfigModel Model { get; set; } = null!;

        /// <summary>
        /// Application settings (features, flags).
        /// </summary>
        public SettingsModel Settings { get; set; } = null!;

        /// <summary>
        /// Application limits (conversation length, message size).
        /// </summary>
        public LimitsModel Limits { get; set; } = null!;

        /// <summary>
        /// Memory system configuration.
        /// </summary>
        public MemoryModel Memory { get; set; } = null!;

        /// <summary>
        /// Prompt construction rules.
        /// </summary>
        public PromptRulesModel PromptRules { get; set; } = new();

        /// <summary>
        /// Personality and behavioral rules.
        /// </summary>
        public PersonalityRulesModel PersonalityRules { get; set; } = new();

        /// <summary>
        /// Memory indexing rules.
        /// </summary>
        public IndexingRulesModel IndexingRules { get; set; } = new();

        /// <summary>
        /// Logging configuration.
        /// </summary>
        public LoggingModel Logging { get; set; } = new();
    }
}
