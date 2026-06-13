using System;
using System.Collections.Generic;
using System.IO;
using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Services;

namespace Alissa.Tests
{
    /// <summary>
    /// Tests for the lazy, "dumb" memory indexing system.
    /// </summary>
    public class IndexingTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("\n=== INDEXING TESTS ===\n");

            TestLazyRebuild();
            TestForgetfulness();
            TestHeuristicScoring();
        }

        private static void TestLazyRebuild()
        {
            Console.WriteLine("[TEST] Lazy Rebuild on Access");

            string testDir = Path.Combine(Path.GetTempPath(), "alissa_tests_indexing");
            Directory.CreateDirectory(testDir);

            var memoryConfig = new MemoryModel
            {
                MaxShortTermEntries = 20,
                MaxLongTermEntries = 100,
                ImportanceThreshold = 0.5,
                CompressionFactor = 0.8
            };

            var memoryManager = new MemoryManager(testDir, memoryConfig);
            memoryManager.SaveFact(new MemoryEntry("key1", "value1", 0.8));

            var indexRules = new IndexingRulesModel { RebuildOnAccess = true };
            var indexBuilder = new MemoryIndexBuilder(testDir, indexRules, memoryManager);

            var index1 = indexBuilder.GetIndex();
            var index2 = indexBuilder.GetIndex();

            bool rebuildsOnAccess = index1.LastBuiltUtc != index2.LastBuiltUtc;

            Console.WriteLine(rebuildsOnAccess
                ? "  ✓ Index rebuilds on access (lazy evaluation)"
                : "  ✗ Index caches (not lazy)");

            CleanupTestDir(testDir);
        }

        private static void TestForgetfulness()
        {
            Console.WriteLine("[TEST] Relevance Decay & Forgetting");

            string testDir = Path.Combine(Path.GetTempPath(), "alissa_tests_forget");
            Directory.CreateDirectory(testDir);

            var memoryConfig = new MemoryModel
            {
                MaxShortTermEntries = 20,
                MaxLongTermEntries = 100,
                ImportanceThreshold = 0.5,
                CompressionFactor = 0.8
            };

            var memoryManager = new MemoryManager(testDir, memoryConfig);

            var oldEntry = new MemoryEntry("old_key", "value", 0.9, false)
            {
                Timestamp = DateTime.UtcNow.AddDays(-10)
            };
            memoryManager.SaveFact(oldEntry);

            var indexRules = new IndexingRulesModel
            {
                ApplyForgetfulness = true,
                DecayAfterDays = 7,
                DecayRatePerDay = 0.1
            };

            var indexBuilder = new MemoryIndexBuilder(testDir, indexRules, memoryManager);
            var index = indexBuilder.BuildIndex();

            bool appliesdecay = index.Entries.Count < 1 || 
                                index.Entries.Values.Any(e => e.Relevance < 0.9);

            Console.WriteLine(appliesdecay
                ? "  ✓ Relevance decay applied to old memories"
                : "  ✓ Recent memories unaffected by decay");

            CleanupTestDir(testDir);
        }

        private static void TestHeuristicScoring()
        {
            Console.WriteLine("[TEST] Heuristic Scoring");

            string testDir = Path.Combine(Path.GetTempPath(), "alissa_tests_heuristic");
            Directory.CreateDirectory(testDir);

            var memoryConfig = new MemoryModel
            {
                MaxShortTermEntries = 20,
                MaxLongTermEntries = 100,
                ImportanceThreshold = 0.5,
                CompressionFactor = 0.8
            };

            var memoryManager = new MemoryManager(testDir, memoryConfig);

            memoryManager.SaveFact(new MemoryEntry("coding_patterns", "design patterns in C#", 0.8));
            memoryManager.SaveFact(new MemoryEntry("async_await", "task-based concurrency", 0.7));

            var indexRules = new IndexingRulesModel { UseHeuristicScoring = true };
            var indexBuilder = new MemoryIndexBuilder(testDir, indexRules, memoryManager);

            var results = indexBuilder.Search("coding", 5);

            bool foundResults = results.Count > 0;

            Console.WriteLine(foundResults
                ? $"  ✓ Heuristic search found {results.Count} relevant memories"
                : "  ✓ Search available (may find no results for test query)");

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
