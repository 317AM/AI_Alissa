using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Services;

namespace Alissa.Tests
{
    /// <summary>
    /// Tests for Phase 1 improvements: Medium-term memory, Thought service, and User context.
    /// </summary>
    public class Phase1ImprovementTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("\n=== PHASE 1 IMPROVEMENT TESTS ===\n");

            TestMediumTermMemoryWiring();
            TestThoughtServiceWiring();
            TestUserContextService();
            TestPersonalityConsistency();
            TestQueryAwareMemoryRetrieval();
        }

        private static void TestMediumTermMemoryWiring()
        {
            Console.WriteLine("[TEST] Medium-Term Memory Wiring");

            string testDir = Path.Combine(Path.GetTempPath(), "alissa_tests_phase1_medium");
            Directory.CreateDirectory(testDir);

            try
            {
                // Setup
                var memoryConfig = new MemoryModel
                {
                    MaxShortTermEntries = 20,
                    MaxLongTermEntries = 100,
                    ImportanceThreshold = 0.5,
                    CompressionFactor = 0.8
                };

                var memoryManager = new MemoryManager(testDir, memoryConfig);
                var mediumTermService = new MediumTermMemoryService(testDir, enabled: true);

                // Save a medium-term memory entry
                var entry = new MediumTermMemoryEntry
                {
                    Summary = "Discussed C# best practices",
                    Topics = new List<string> { "csharp", "architecture" },
                    Tags = new List<string> { "code_review" },
                    Timestamp = DateTime.UtcNow,
                    RelevanceScore = 0.8
                };

                mediumTermService.SaveEntry(entry);

                // Retrieve and verify
                var retrieved = mediumTermService.GetRelevantEntries(maxCount: 5);
                var hasMediumTermEntry = retrieved.Any();

                Console.WriteLine(hasMediumTermEntry
                    ? "  ✓ Medium-term memory successfully wired and retrievable"
                    : "  ✗ Failed to retrieve medium-term memory entry");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Test failed: {ex.Message}");
            }
            finally
            {
                try { Directory.Delete(testDir, true); } catch { }
            }
        }

        private static void TestThoughtServiceWiring()
        {
            Console.WriteLine("[TEST] Thought Service Wiring");

            string testDir = Path.Combine(Path.GetTempPath(), "alissa_tests_phase1_thought");
            Directory.CreateDirectory(testDir);

            try
            {
                // Create a simple mock chat client for testing
                var mockChatClient = new MockChatClientForThoughts();

                // Create thought service
                var thoughtService = new ThoughtService(testDir, mockChatClient);

                // Store a thought
                thoughtService.StoreThoughtAsync("User wants help with architecture", "session_reflection").Wait();

                // Retrieve thoughts
                var thoughts = thoughtService.GetRelevantThoughtsAsync("architecture").GetAwaiter().GetResult();
                var hasThought = thoughts.Any();

                Console.WriteLine(hasThought
                    ? "  ✓ Thought service successfully wired and storing thoughts"
                    : "  ✗ Failed to retrieve stored thoughts");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Test failed: {ex.Message}");
            }
            finally
            {
                try { Directory.Delete(testDir, true); } catch { }
            }
        }

        private static void TestUserContextService()
        {
            Console.WriteLine("[TEST] User Context Service");

            string testDir = Path.Combine(Path.GetTempPath(), "alissa_tests_phase1_userctx");
            Directory.CreateDirectory(testDir);

            try
            {
                var userContextService = new UserContextService(testDir);

                // Set manual user
                userContextService.SetManualUserAsync("Jackson").Wait();

                // Retrieve current user
                var currentUser = userContextService.GetCurrentUserAsync().GetAwaiter().GetResult();
                var isJackson = currentUser == "Jackson";

                // Check override status
                var hasOverride = userContextService.HasManualUserOverrideAsync().GetAwaiter().GetResult();

                Console.WriteLine((isJackson && hasOverride)
                    ? "  ✓ User context service correctly wired with manual override"
                    : "  ✗ User context service failed to maintain override");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Test failed: {ex.Message}");
            }
            finally
            {
                try { Directory.Delete(testDir, true); } catch { }
            }
        }

        private static void TestPersonalityConsistency()
        {
            Console.WriteLine("[TEST] Personality Rules Consistency");

            try
            {
                var personalityRules = new PersonalityRulesModel();

                // Verify defaults match personality_rules.json
                var sillinessCorrect = personalityRules.SillinessLevel == 0.7;
                var catgirlEnabled = personalityRules.EnableCatgirlTraits == true;
                var unfilteredEnabled = personalityRules.UnfilteredMode == true;
                var quirkEnabled = personalityRules.EnableQuirks == true;
                var primaryTraitAnnoying = personalityRules.CustomTraits.ContainsKey("primary_trait") &&
                                          personalityRules.CustomTraits["primary_trait"] == "annoying";

                var isConsistent = sillinessCorrect && catgirlEnabled && unfilteredEnabled && quirkEnabled && primaryTraitAnnoying;

                Console.WriteLine(isConsistent
                    ? "  ✓ Personality rules consistent with personality_rules.json"
                    : "  ✗ Personality rules mismatch with JSON config");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Test failed: {ex.Message}");
            }
        }

        private static void TestQueryAwareMemoryRetrieval()
        {
            Console.WriteLine("[TEST] Query-Aware Memory Retrieval");

            string testDir = Path.Combine(Path.GetTempPath(), "alissa_tests_phase1_queryaware");
            Directory.CreateDirectory(testDir);

            try
            {
                // Setup
                var memoryConfig = new MemoryModel
                {
                    MaxShortTermEntries = 20,
                    MaxLongTermEntries = 100,
                    ImportanceThreshold = 0.5,
                    CompressionFactor = 0.8
                };

                Directory.CreateDirectory(Path.Combine(testDir, "personality"));
                File.WriteAllText(Path.Combine(testDir, "personality", "identity.txt"), "## Identity\nAlissa is a catgirl.");
                File.WriteAllText(Path.Combine(testDir, "personality", "behaviour.txt"), "## Behavior\nBe playful.");
                File.WriteAllText(Path.Combine(testDir, "personality", "boundaries.txt"), "## Boundaries\nStay focused.");

                var memoryManager = new MemoryManager(testDir, memoryConfig);
                memoryManager.SaveUserProfile(new MemoryEntry("expertise", "C# expert", 1.0, true));
                memoryManager.SaveUserProfile(new MemoryEntry("hobby", "gaming", 0.8, false));

                var promptRules = new PromptRulesModel();
                var personalityRules = new PersonalityRulesModel();

                var promptBuilder = new PromptBuilder(testDir, memoryManager, promptRules, personalityRules);

                // Test query-aware retrieval with context
                var sessionMessages = new List<Message>
                {
                    new Message { Role = MessageRole.User, Content = "Help with C# architecture" }
                };

                var prompt = promptBuilder.BuildSystemPromptWithContext(sessionMessages, "Tell me about C# design patterns");
                var hasPrompt = !string.IsNullOrEmpty(prompt);

                Console.WriteLine(hasPrompt
                    ? "  ✓ Query-aware memory retrieval successfully integrated"
                    : "  ✗ Failed to build prompt with query-aware memory");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Test failed: {ex.Message}");
            }
            finally
            {
                try { Directory.Delete(testDir, true); } catch { }
            }
        }
    }

    /// <summary>
    /// Simple mock chat client for testing thought service.
    /// </summary>
    internal class MockChatClientForThoughts : IChatClient
    {
        public async IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userMessage)
        {
            yield return "Mock thought response";
            await Task.CompletedTask;
        }

        public async IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userMessage, Action<string>? onEmoji = null)
        {
            yield return "Mock thought response";
            await Task.CompletedTask;
        }
    }
}
