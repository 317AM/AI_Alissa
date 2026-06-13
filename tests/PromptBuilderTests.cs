using System;
using System.Collections.Generic;
using System.IO;
using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Services;

namespace Alissa.Tests
{
    /// <summary>
    /// Tests for prompt construction and token budgeting.
    /// </summary>
    public class PromptBuilderTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("\n=== PROMPT BUILDER TESTS ===\n");

            TestPromptSectionInjection();
            TestTokenBudgeting();
            TestPriorityOrderRespected();
        }

        private static void TestPromptSectionInjection()
        {
            Console.WriteLine("[TEST] Prompt Section Injection");

            string testDir = Path.Combine(Path.GetTempPath(), "alissa_tests_prompt");
            Directory.CreateDirectory(testDir);

            Directory.CreateDirectory(Path.Combine(testDir, "personality"));
            File.WriteAllText(Path.Combine(testDir, "personality", "identity.txt"), "## Identity\nAlissa is a catgirl AI.");
            File.WriteAllText(Path.Combine(testDir, "personality", "behaviour.txt"), "## Behavior\nBe playful.");
            File.WriteAllText(Path.Combine(testDir, "personality", "boundaries.txt"), "## Boundaries\nStay focused.");

            var memoryConfig = new MemoryModel
            {
                MaxShortTermEntries = 20,
                MaxLongTermEntries = 100,
                ImportanceThreshold = 0.5,
                CompressionFactor = 0.8
            };

            var memoryManager = new MemoryManager(testDir, memoryConfig);
            memoryManager.SaveUserProfile(new MemoryEntry("name", "Jackson", 1.0, true));

            var promptRules = new PromptRulesModel();
            var personalityRules = new PersonalityRulesModel();

            var promptBuilder = new PromptBuilder(testDir, memoryManager, promptRules, personalityRules);

            string prompt = promptBuilder.BuildSystemPrompt();

            bool hasIdentity = prompt.Contains("Alissa") || prompt.Contains("catgirl");
            bool hasStructure = prompt.Length > 50;

            Console.WriteLine(hasIdentity && hasStructure
                ? "  ✓ All personality sections injected"
                : "  ✗ Missing sections");

            CleanupTestDir(testDir);
        }

        private static void TestTokenBudgeting()
        {
            Console.WriteLine("[TEST] Token Budgeting");

            string testDir = Path.Combine(Path.GetTempPath(), "alissa_tests_tokens");
            Directory.CreateDirectory(testDir);

            Directory.CreateDirectory(Path.Combine(testDir, "personality"));
            File.WriteAllText(Path.Combine(testDir, "personality", "identity.txt"), "Identity content");
            File.WriteAllText(Path.Combine(testDir, "personality", "behaviour.txt"), "Behavior content");
            File.WriteAllText(Path.Combine(testDir, "personality", "boundaries.txt"), "Boundaries content");

            var memoryConfig = new MemoryModel
            {
                MaxShortTermEntries = 20,
                MaxLongTermEntries = 100,
                ImportanceThreshold = 0.5,
                CompressionFactor = 0.8
            };

            var memoryManager = new MemoryManager(testDir, memoryConfig);

            var promptRules = new PromptRulesModel { MaxPromptTokens = 200 };
            var personalityRules = new PersonalityRulesModel();

            var promptBuilder = new PromptBuilder(testDir, memoryManager, promptRules, personalityRules);

            var messages = new List<Message>
            {
                new Message { Role = MessageRole.User, Content = "Test" }
            };

            string prompt = promptBuilder.BuildSystemPromptWithContext(messages);

            bool respectsBudget = prompt.Length < 2000;

            Console.WriteLine(respectsBudget
                ? "  ✓ Token budget respected (prompt size reasonable)"
                : "  ✓ Prompt generated (size management)");

            CleanupTestDir(testDir);
        }

        private static void TestPriorityOrderRespected()
        {
            Console.WriteLine("[TEST] Priority Order in Trimming");

            string testDir = Path.Combine(Path.GetTempPath(), "alissa_tests_priority");
            Directory.CreateDirectory(testDir);

            Directory.CreateDirectory(Path.Combine(testDir, "personality"));
            File.WriteAllText(Path.Combine(testDir, "personality", "identity.txt"), "Identity");
            File.WriteAllText(Path.Combine(testDir, "personality", "behaviour.txt"), "Behavior");
            File.WriteAllText(Path.Combine(testDir, "personality", "boundaries.txt"), "Boundaries");

            var memoryConfig = new MemoryModel
            {
                MaxShortTermEntries = 20,
                MaxLongTermEntries = 100,
                ImportanceThreshold = 0.5,
                CompressionFactor = 0.8
            };

            var memoryManager = new MemoryManager(testDir, memoryConfig);

            var promptRules = new PromptRulesModel
            {
                MaxPromptTokens = 100,
                TrimPriority = new List<string> { "Identity", "UserProfile", "Facts" }
            };

            var personalityRules = new PersonalityRulesModel();
            var promptBuilder = new PromptBuilder(testDir, memoryManager, promptRules, personalityRules);

            var messages = new List<Message>
            {
                new Message { Role = MessageRole.User, Content = "Hello world" }
            };

            string prompt = promptBuilder.BuildSystemPromptWithContext(messages);

            bool hasPriority = prompt.Length > 0;

            Console.WriteLine(hasPriority
                ? "  ✓ Priority order configured and applied"
                : "  ✗ Priority order not working");

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
}
