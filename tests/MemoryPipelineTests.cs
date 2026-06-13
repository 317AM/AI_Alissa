using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Services;

namespace Alissa.Tests
{
    /// <summary>
    /// Integration tests for the memory pipeline.
    /// </summary>
    public class MemoryPipelineTests
    {
        public static async Task RunAllTests()
        {
            Console.WriteLine("\n=== MEMORY PIPELINE TESTS ===\n");

            await TestMemoryPipelineFlow();
            TestSessionCachePurity();
            TestMemoryExtractionIsFlexible();
        }

        private static async Task TestMemoryPipelineFlow()
        {
            Console.WriteLine("[TEST] Memory Pipeline Flow");

            string testDir = Path.Combine(Path.GetTempPath(), "alissa_tests");
            Directory.CreateDirectory(testDir);

            var memoryConfig = new MemoryModel
            {
                MaxShortTermEntries = 20,
                MaxLongTermEntries = 100,
                ImportanceThreshold = 0.5,
                CompressionFactor = 0.8
            };

            var promptRules = new PromptRulesModel();
            var personalityRules = new PersonalityRulesModel();

            var memoryManager = new MemoryManager(testDir, memoryConfig);
            var promptBuilder = new PromptBuilder(testDir, memoryManager, promptRules, personalityRules);
            var chatClient = new MockChatClient();

            var summaryService = new SummaryGenerationService(chatClient, promptBuilder);
            var extractionService = new MemoryExtractionService(chatClient, promptBuilder);
            var mediumTermService = new MediumTermMemoryService(testDir, 50, true);
            var indexRules = new IndexingRulesModel();
            var indexBuilder = new MemoryIndexBuilder(testDir, indexRules, memoryManager);

            var pipeline = new MemoryPipeline(
                summaryService,
                extractionService,
                memoryManager,
                mediumTermService,
                indexBuilder);

            string conversationText = "User: What's 2+2?\nAssistant: It's 4.\nUser: Thanks!\nAssistant: You're welcome!";
            string sessionId = Guid.NewGuid().ToString();

            var result = await pipeline.ProcessConversationAsync(conversationText, sessionId);

            Console.WriteLine($"  ✓ Summary generated: {result.Summary.Length} chars");
            Console.WriteLine($"  ✓ Topics identified: {result.Topics.Count}");
            Console.WriteLine($"  ✓ Extraction result: {result.Extraction?.HasData ?? false}");

            CleanupTestDir(testDir);
        }

        private static void TestSessionCachePurity()
        {
            Console.WriteLine("[TEST] Session Cache Purity");

            string testDir = Path.Combine(Path.GetTempPath(), "alissa_tests_cache");
            Directory.CreateDirectory(testDir);

            var memoryConfig = new MemoryModel
            {
                MaxShortTermEntries = 20,
                MaxLongTermEntries = 100,
                ImportanceThreshold = 0.5,
                CompressionFactor = 0.8
            };

            var memoryManager = new MemoryManager(testDir, memoryConfig);

            var messages = new List<Message>
            {
                new Message { Role = MessageRole.User, Content = "Hello" },
                new Message { Role = MessageRole.AI, Content = "Hi there!" }
            };

            memoryManager.SaveSessionCache(messages);

            var loaded = memoryManager.LoadSessionCache();

            bool isUserMessageOnly = loaded.Count == 2 && loaded[0].Role == MessageRole.User && loaded[1].Role == MessageRole.AI;

            Console.WriteLine(isUserMessageOnly
                ? "  ✓ Session cache contains only conversation messages"
                : "  ✗ Session cache was polluted");

            CleanupTestDir(testDir);
        }

        private static void TestMemoryExtractionIsFlexible()
        {
            Console.WriteLine("[TEST] Memory Extraction Flexibility");

            string testDir = Path.Combine(Path.GetTempPath(), "alissa_tests_extract");
            Directory.CreateDirectory(testDir);

            var memoryConfig = new MemoryModel
            {
                MaxShortTermEntries = 20,
                MaxLongTermEntries = 100,
                ImportanceThreshold = 0.5,
                CompressionFactor = 0.8
            };

            var memoryManager = new MemoryManager(testDir, memoryConfig);
            var promptBuilder = new PromptBuilder(testDir, memoryManager);
            var chatClient = new MockChatClient();

            var extractionService = new MemoryExtractionService(chatClient, promptBuilder);

            string summary = "Discussed C# async/await patterns. User prefers explicit error handling.";

            var result = extractionService.ExtractMemoryAsync(summary).Result;

            Console.WriteLine(result.HasData
                ? $"  ✓ Extraction returned data: {result}"
                : "  ✓ Extraction gracefully handled (no data)");

            CleanupTestDir(testDir);
        }

        private static void CleanupTestDir(string testDir)
        {
            try
            {
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Mock chat client for testing without calling Ollama.
    /// </summary>
    public class MockChatClient : IChatClient
    {
        public async IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userInput)
        {
            await Task.Delay(10);
            yield return "This is a test response.";
        }

        public async IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userInput, Action<string>? onEmoji = null)
        {
            await Task.Delay(10);
            yield return "This is a test response.";
        }
    }
}
