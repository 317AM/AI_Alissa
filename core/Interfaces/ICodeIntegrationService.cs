using System.Threading.Tasks;

namespace Alissa.Core.Interfaces
{
    /// <summary>
    /// Service for integrating Alissa into external programs and modifying code.
    /// Enables Alissa to read, analyze, and suggest modifications to code files.
    /// </summary>
    public interface ICodeIntegrationService
    {
        /// <summary>
        /// Reads a code file and returns its content for analysis.
        /// </summary>
        Task<string> ReadCodeFileAsync(string filePath);

        /// <summary>
        /// Analyzes code and returns suggestions for improvements.
        /// </summary>
        Task<string> AnalyzeCodeAsync(string codeContent, string language = "csharp");

        /// <summary>
        /// Generates modified code based on requirements.
        /// </summary>
        Task<string> GenerateModificationAsync(string originalCode, string requirement, string language = "csharp");

        /// <summary>
        /// Writes modified code back to a file.
        /// </summary>
        Task WriteModificationAsync(string filePath, string modifiedCode);

        /// <summary>
        /// Checks if a file path is safe to modify (not in protected directories).
        /// </summary>
        Task<bool> IsFileSafeToModifyAsync(string filePath);

        /// <summary>
        /// Creates a backup of a file before modification.
        /// </summary>
        Task<string> CreateBackupAsync(string filePath);
    }
}
