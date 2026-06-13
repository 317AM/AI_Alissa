using Alissa.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Alissa.Core.Interfaces
{
    /// <summary>
    /// Enhanced memory service that provides semantic and relational memory capabilities.
    /// Supports full memory lifecycle: store, retrieve, relate, and evolve.
    /// </summary>
    public interface ISemanticMemoryService
    {
        /// <summary>
        /// Stores a memory with semantic embeddings for similarity matching.
        /// </summary>
        Task StoreSemanticMemoryAsync(string content, string category, string[] keywords);

        /// <summary>
        /// Retrieves memories similar to the given query.
        /// </summary>
        Task<List<string>> FindSimilarMemoriesAsync(string query, int count = 5);

        /// <summary>
        /// Creates relationships between memories for contextual understanding.
        /// </summary>
        Task LinkMemoriesAsync(string memoryId1, string memoryId2, string relationship);

        /// <summary>
        /// Gets memories related to a specific topic or context.
        /// </summary>
        Task<List<string>> GetRelatedMemoriesAsync(string topic);

        /// <summary>
        /// Updates memory metadata and relationships over time.
        /// </summary>
        Task EvolveMemoryAsync(string memoryId, string updatedContent);

        /// <summary>
        /// Rebuilds semantic index for better search performance.
        /// </summary>
        Task RebuildSemanticIndexAsync();
    }
}
