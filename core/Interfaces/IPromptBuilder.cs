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
        /// Builds the system prompt with recent conversation context and query-aware memory retrieval.
        /// This is the async version that properly awaits all async dependencies without blocking.
        /// </summary>
        /// <param name="recentMessages">Recent conversation messages</param>
        /// <param name="currentUserInput">Current user input for query-aware memory search</param>
        Task<string> BuildSystemPromptWithContextAsync(List<Message> recentMessages, string currentUserInput = "");
    }
}
