using Alissa.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alissa.Core.Memory
{
    public class MemoryScorer
    {
        private readonly MemoryModel _memoryConfig;

        public MemoryScorer(MemoryModel memoryConfig)
        {
            _memoryConfig = memoryConfig;
        }

        public List<MemoryEntry> ScoreAndRank(List<MemoryEntry> entries, int count)
        {
            return entries
                .Where(e => e.Relevance >= _memoryConfig.ImportanceThreshold)
                .OrderByDescending(m => CalculateScore(m))
                .ThenByDescending(m => m.Timestamp)
                .Take(count)
                .ToList();
        }

        public List<MemoryEntry> GetContextMemory(List<MemoryEntry> allMemory, int maxEntries, bool includeCore = true)
        {
            var coreMemory = allMemory.Where(m => m.IsCoreMemory).ToList();
            var ephemeralMemory = allMemory
                .Where(m => !m.IsCoreMemory && m.Relevance >= _memoryConfig.ImportanceThreshold)
                .OrderByDescending(m => CalculateScore(m))
                .ThenByDescending(m => m.Timestamp)
                .Take(maxEntries)
                .ToList();

            return includeCore ? coreMemory.Concat(ephemeralMemory).ToList() : ephemeralMemory;
        }

        public List<MemoryEntry> FilterByThreshold(List<MemoryEntry> entries)
        {
            return entries.Where(e => e.Relevance >= _memoryConfig.ImportanceThreshold).ToList();
        }

        private double CalculateScore(MemoryEntry entry)
        {
            double relevance = entry.Relevance;
            double recencyBoost = 1.0 + (1.0 / (1.0 + (DateTime.Now - entry.Timestamp).TotalDays));
            return relevance * recencyBoost;
        }
    }
}
