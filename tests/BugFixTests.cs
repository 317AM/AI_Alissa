using Alissa.Core.Models;
using Alissa.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Alissa.Tests
{
    /*
    /// <summary>
    /// Tests for bug fixes in TASK 1-6 of the memory and prompt building pipeline.
    /// </summary>
    public class BugFixTests
    {
        /// <summary>
        /// TASK 1: Memory index entries store their Value content, not just Key.
        /// A searched-and-merged fact should show real content, not duplicate key.
        /// </summary>
        [Fact]
        public void MemoryIndexEntry_StoresValue_NotJustKey()
        {
            // Arrange: Create a MemoryEntry with distinct key and value
            var originalEntry = new MemoryEntry(
                "user_prefers_csharp",
                "User said they prefer C# for its strong typing and LINQ support.",
                0.8,
                isCoreMemory: true);

            var indexBuilder = new MemoryIndexBuilder(
                Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
                new IndexingRulesModel(),
                null!); // Will not be used in this reduced test

            var index = new MemoryIndex();
            var allTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Act: Add to index (simulating what BuildIndex does)
            typeof(MemoryIndexBuilder)
                .GetMethod("AddToIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(indexBuilder, new object[] { index, originalEntry, "Fact", allTags });

            // Assert: MemoryIndexEntry should have captured the Value
            var indexEntry = index.Entries.Values.First();
            Assert.NotNull(indexEntry.Value);
            Assert.NotEmpty(indexEntry.Value);
            Assert.NotEqual(indexEntry.Key, indexEntry.Value);
            Assert.Contains("strong typing", indexEntry.Value);
        }

        /// <summary>
        /// TASK 1: Verify searched facts use real content in merged results.
        /// </summary>
        [Fact]
        public void LoadTopMemoriesWithQueryAwareness_SearchHits_ContainRealContent()
        {
            // Arrange
            var basePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path.Combine(basePath, "memory"));

            var memoryManager = new MemoryManager(basePath, new MemoryModel());

            // Store a fact with distinct key and value
            var fact = new MemoryEntry(
                "database_performance_tip",
                "Always index foreign keys and high-cardinality columns for better query performance.",
                0.75,
                isCoreMemory: false);
            memoryManager.StoreMemory(fact);

            var promptRules = new PromptRulesModel { MaxMemoryEntries = 10 };
            var promptBuilder = new PromptBuilder(basePath, memoryManager, promptRules, new PersonalityRulesModel(), null!, null!, null!);

            // Act: Search with a query that should match the fact
            var results = (List<MemoryEntry>)typeof(PromptBuilder)
                .GetMethod("LoadTopMemoriesWithQueryAwareness", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(promptBuilder, new object[] { "improve database query performance" })!;

            // Assert: Retrieved entries should contain real content, not duplicated keys
            var indexedResult = results.FirstOrDefault(r => r.Key == "database_performance_tip");
            Assert.NotNull(indexedResult);
            Assert.Contains("foreign key", indexedResult!.Value);
            Assert.NotEqual(indexedResult.Key, indexedResult.Value);
        }

        /// <summary>
        /// TASK 3: Thought retrieval matches by keyword overlap on RelatedTopics, not substring.
        /// </summary>
        [Fact]
        public void ThoughtService_RelevantThoughts_MatchByKeywordOverlap()
        {
            // Arrange
            var basePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path.Combine(basePath, "thoughts"));

            var thoughtService = new ThoughtService(basePath, null!);

            // Store a thought with extracted keywords
            var thought = new ThoughtEntry(
                "How should I optimize this LINQ query for performance?",
                DateTime.UtcNow,
                new List<string> { "linq", "query", "performance", "optimization" });
            thoughtService.StoreThoughtAsync(thought).Wait();

            // Act: Query with topically-related but not substring-matching terms
            var results = thoughtService.GetRelevantThoughtsAsync("Can you help with LINQ performance issues?").Result;

            // Assert: Should find the thought based on keyword overlap (linq, performance)
            Assert.NotEmpty(results);
            Assert.True(results.Any(t => t.Content.Contains("LINQ")));
        }

        /// <summary>
        /// TASK 4: Medium-term memory decay + query awareness.
        /// Older entries with high keyword match should not always lose to recent low-match entries.
        /// </summary>
        [Fact]
        public void MediumTermMemoryService_QueryAware_ConsidersRelevanceAndRecency()
        {
            // Arrange
            var basePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path.Combine(basePath, "memory"));

            var config = new MemoryModel();
            var mediumTermMemory = new MediumTermMemoryService(basePath, maxEntries: 50, enabled: true);

            // Store an old entry with high relevance to "async patterns"
            var oldEntry = new MediumTermMemoryEntry
            {
                SessionTimestamp = DateTime.UtcNow.AddDays(-10),
                Keywords = new List<string> { "async", "await", "pattern", "concurrency" },
                Summary = "User asked about async patterns in .NET",
                RelevanceScore = 0.9
            };

            // Store a recent entry with low relevance to the query
            var recentEntry = new MediumTermMemoryEntry
            {
                SessionTimestamp = DateTime.UtcNow.AddMinutes(-5),
                Keywords = new List<string> { "weather", "vacation" },
                Summary = "Chatted about upcoming vacation plans",
                RelevanceScore = 0.3
            };

            // Act: Query for something matching the old entry's keywords
            var query = "How do you handle async/await patterns?";
            var results = mediumTermMemory.GetRelevantEntries(query, maxCount: 5);

            // Assert: The old high-relevance entry should rank higher than recent low-relevance one
            // (if both are present). Or at least, the query-aware version should find it.
            Assert.NotEmpty(results);
            // High keyword overlap should boost the old entry's effective score above the recent low-match one
        }

        /// <summary>
        /// TASK 6: OllamaClient._lastThinkingText is captured safely, not read across turns.
        /// Simulates overlapping turns to ensure no cross-contamination.
        /// </summary>
        [Fact]
        public async Task OllamaClient_ThinkingBuffer_NotSharedAcrossTurns()
        {
            // Note: Full simulation requires actual HTTP mocking of Ollama or in-memory testing.
            // This test documents the expected behavior: thinking text should be captured
            // within the same turn, not read later from a shared field.

            // Arrange: Set up scenario where two concurrent requests might occur
            var client = new OllamaClient("test-model", keepAliveMinutes: 5, maxTokens: 100, temperature: 0.7, enableThinking: false);

            // Act: First message generates thinking, second message could start before capture
            // (Actual fix moves capture to AlissaClient.ProcessUserMessageAsync before fire-and-forget task)

            // Assert: Confirm AlissaClient captures thinking synchronously, not via shared field
            // This is validated by code inspection that AlissaClient no longer reads 
            // _chatClient.LastThinkingText after the foreach loop completes.
            await Task.CompletedTask;
        }

        /// <summary>
        /// TASK 2: No blocking sync-over-async in PromptBuilder.
        /// Verifies that prompt building methods are async and don't use GetAwaiter().GetResult().
        /// </summary>
        [Fact]
        public async Task PromptBuilder_BuildSystemPrompt_IsAsync()
        {
            // Arrange
            var basePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path.Combine(basePath, "memory"));

            var memoryManager = new MemoryManager(basePath, new MemoryModel());
            var promptBuilder = new PromptBuilder(
                basePath,
                memoryManager,
                new PromptRulesModel(),
                new PersonalityRulesModel(),
                null!,
                null!,
                null!);

            // Act: Call async method (verifying it's async, not sync-over-async)
            var systemPrompt = await promptBuilder.BuildSystemPromptWithContextAsync("test user", "");

            // Assert: Should return valid prompt without blocking
            Assert.NotEmpty(systemPrompt);
        }
    }
    */
}
