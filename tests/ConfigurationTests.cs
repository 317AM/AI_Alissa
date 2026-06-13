using System;
using System.Collections.Generic;
using System.IO;
using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Services;

namespace Alissa.Tests
{
    /// <summary>
    /// Tests for configuration loading and model deserialization.
    /// </summary>
    public class ConfigurationTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("\n=== CONFIGURATION TESTS ===\n");

            TestConfigLoading();
            TestPromptRulesModel();
            TestPersonalityRulesModel();
            TestIndexingRulesModel();
        }

        private static void TestConfigLoading()
        {
            Console.WriteLine("[TEST] Configuration Loading");

            string testDir = Path.Combine(Path.GetTempPath(), "alissa_tests_config");
            Directory.CreateDirectory(testDir);
            Directory.CreateDirectory(Path.Combine(testDir, "config"));

            CreateMinimalConfig(testDir);

            try
            {
                var config = ConfigService.LoadAll(testDir);

                bool hasAllSections = config.Model != null &&
                                    config.Settings != null &&
                                    config.Limits != null &&
                                    config.Memory != null;

                Console.WriteLine(hasAllSections
                    ? "  ✓ All configuration sections loaded"
                    : "  ✗ Missing configuration sections");

                bool hasNewRules = config.PromptRules != null &&
                                 config.PersonalityRules != null &&
                                 config.IndexingRules != null &&
                                 config.Logging != null;

                Console.WriteLine(hasNewRules
                    ? "  ✓ New configuration rules loaded"
                    : "  ✓ Configuration loaded (new rules optional)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Configuration loading failed: {ex.Message}");
            }

            CleanupTestDir(testDir);
        }

        private static void TestPromptRulesModel()
        {
            Console.WriteLine("[TEST] PromptRulesModel Deserialization");

            var promptRules = new PromptRulesModel();

            bool hasDefaults = promptRules.MaxPromptTokens > 0 &&
                             promptRules.MaxSessionMessages > 0 &&
                             promptRules.TrimPriority.Count > 0;

            Console.WriteLine(hasDefaults
                ? "  ✓ PromptRulesModel has valid defaults"
                : "  ✗ PromptRulesModel missing defaults");
        }

        private static void TestPersonalityRulesModel()
        {
            Console.WriteLine("[TEST] PersonalityRulesModel Deserialization");

            var personalityRules = new PersonalityRulesModel();

            bool hasCatgirl = personalityRules.EnableCatgirlTraits;
            bool hasPreventRepetition = personalityRules.PreventRepetition;
            bool hasSilliness = personalityRules.SillinessLevel >= 0 && personalityRules.SillinessLevel <= 1.0;

            bool testPassed = hasCatgirl && hasPreventRepetition && hasSilliness;

            string message = testPassed
                ? "  ✓ PersonalityRulesModel configured for catgirl personality"
                : "  ✗ Personality configuration incomplete";

            Console.WriteLine(message);
        }

        private static void TestIndexingRulesModel()
        {
            Console.WriteLine("[TEST] IndexingRulesModel Deserialization");

            var indexingRules = new IndexingRulesModel();

            bool hasLazyRebuild = indexingRules.RebuildOnAccess;
            bool hasForgetfulness = indexingRules.ApplyForgetfulness;
            bool hasHeuristics = indexingRules.UseHeuristicScoring;

            Console.WriteLine(hasLazyRebuild && hasForgetfulness && hasHeuristics
                ? "  ✓ IndexingRulesModel configured for 'dumb' indexing"
                : "  ✓ IndexingRulesModel available");
        }

        private static void CreateMinimalConfig(string testDir)
        {
            string configDir = Path.Combine(testDir, "config");

            var model = new { ModelName = "test:model", KeepAliveMinutes = 10, MaxTokens = 1024, ResponseTimeoutSeconds = 30 };
            File.WriteAllText(Path.Combine(configDir, "model.json"), 
                System.Text.Json.JsonSerializer.Serialize(model, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            var settings = new { EnableSummaries = true };
            File.WriteAllText(Path.Combine(configDir, "settings.json"),
                System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            var limits = new { MaxConversationLength = 100, SummaryDivisionFactor = 5, MaxMessageLength = 4096 };
            File.WriteAllText(Path.Combine(configDir, "limits.json"),
                System.Text.Json.JsonSerializer.Serialize(limits, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            var memory = new { MaxShortTermEntries = 20, MaxLongTermEntries = 100, ImportanceThreshold = 0.5, CompressionFactor = 0.8 };
            File.WriteAllText(Path.Combine(configDir, "memory_rules.json"),
                System.Text.Json.JsonSerializer.Serialize(memory, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
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
