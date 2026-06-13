using Alissa.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Alissa.Core.Interfaces
{
    /// <summary>
    /// Service for managing Alissa's internal thoughts and reasoning.
    /// Thoughts are stored separately from conversation for deeper understanding.
    /// </summary>
    public interface IThoughtService
    {
        /// <summary>
        /// Generates internal thoughts about a user message.
        /// Thoughts are not displayed but inform context and memory.
        /// </summary>
        Task<string> GenerateThoughtAsync(string userMessage, List<Message> conversationContext);

        /// <summary>
        /// Stores a thought to persistent storage for later reference.
        /// </summary>
        Task StoreThoughtAsync(string thought, string category);

        /// <summary>
        /// Retrieves relevant thoughts related to current conversation.
        /// </summary>
        Task<List<string>> GetRelevantThoughtsAsync(string topic);

        /// <summary>
        /// Gets all stored thoughts for a session.
        /// </summary>
        Task<List<string>> GetSessionThoughtsAsync(string sessionId);
    }
}
