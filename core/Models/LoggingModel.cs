using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Configuration for logging behavior across the system.
    /// </summary>
    public class LoggingModel
    {
        /// <summary>
        /// Enable logging of all conversations.
        /// </summary>
        [JsonPropertyName("enableConversationLogs")]
        public bool EnableConversationLogs { get; set; } = true;

        /// <summary>
        /// Enable logging of generated summaries.
        /// </summary>
        [JsonPropertyName("enableSummaryLogs")]
        public bool EnableSummaryLogs { get; set; } = true;

        /// <summary>
        /// Enable logging of memory extraction and updates.
        /// </summary>
        [JsonPropertyName("enableMemoryLogs")]
        public bool EnableMemoryLogs { get; set; } = true;

        /// <summary>
        /// Enable detailed debug logging.
        /// </summary>
        [JsonPropertyName("enableDebugLogs")]
        public bool EnableDebugLogs { get; set; } = false;

        /// <summary>
        /// Enable logging of prompt construction.
        /// </summary>
        [JsonPropertyName("enablePromptLogs")]
        public bool EnablePromptLogs { get; set; } = false;

        /// <summary>
        /// Directory where logs are stored.
        /// </summary>
        [JsonPropertyName("logsDirectory")]
        public string LogsDirectory { get; set; } = "logs";

        /// <summary>
        /// Log level threshold (Trace, Debug, Info, Warning, Error, Critical).
        /// </summary>
        [JsonPropertyName("minLogLevel")]
        public string MinLogLevel { get; set; } = "Info";

        /// <summary>
        /// Maximum size of a single log file in MB before rotation.
        /// </summary>
        [JsonPropertyName("maxLogFileSizeMb")]
        public int MaxLogFileSizeMb { get; set; } = 10;

        /// <summary>
        /// Log format (Simple, Json, Detailed).
        /// </summary>
        [JsonPropertyName("logFormat")]
        public string LogFormat { get; set; } = "Simple";
    }
}
