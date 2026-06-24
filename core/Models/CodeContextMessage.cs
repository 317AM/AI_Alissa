using System.Text.Json.Serialization;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Message sent from external tools (VS Extension, CLI, etc.) to communicate code context.
    /// Used for the /api/code endpoint.
    /// </summary>
    public class CodeContextMessage
    {
        /// <summary>
        /// Message type: "code_context" | "code_request" | "error_context" | "build_result"
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "code_context";

        /// <summary>
        /// Full path to the file being worked on.
        /// </summary>
        [JsonPropertyName("filePath")]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Programming language (csharp, python, javascript, etc.).
        /// </summary>
        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// The selected/active portion of the file.
        /// </summary>
        [JsonPropertyName("selectedText")]
        public string SelectedText { get; set; } = string.Empty;

        /// <summary>
        /// Complete file content.
        /// </summary>
        [JsonPropertyName("fullFileContent")]
        public string FullFileContent { get; set; } = string.Empty;

        /// <summary>
        /// Task or intent: "explain" | "fix" | "review" | "complete" | "test" | "refactor"
        /// </summary>
        [JsonPropertyName("task")]
        public string Task { get; set; } = string.Empty;

        /// <summary>
        /// Error message if Type is "error_context".
        /// </summary>
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Build output if Type is "build_result".
        /// </summary>
        [JsonPropertyName("buildOutput")]
        public string BuildOutput { get; set; } = string.Empty;

        /// <summary>
        /// Project name for context.
        /// </summary>
        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Solution path for context.
        /// </summary>
        [JsonPropertyName("solutionPath")]
        public string SolutionPath { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when this message was created.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
