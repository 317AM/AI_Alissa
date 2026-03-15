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
        /// Builds the system prompt with recent conversation context
        /// </summary>
        string BuildSystemPromptWithContext(List<Message> recentMessages);
    }
}
