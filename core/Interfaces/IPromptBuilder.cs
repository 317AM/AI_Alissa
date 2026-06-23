using Alissa.Core.Models;

namespace Alissa.Core.Interfaces
{
    public interface IPromptBuilder
    {
        /// <summary>
        /// Builds the system prompt from personality and memory
        /// </summary>
        string BuildSystemPrompt();

        /// <summary>
        /// Builds the system prompt with recent conversation context and query-aware memory retrieval
        /// </summary>
        /// <param name="recentMessages">Recent conversation messages</param>
        /// <param name="currentUserInput">Current user input for query-aware memory search</param>
        string BuildSystemPromptWithContext(List<Message> recentMessages, string currentUserInput = "");
    }
}
