using Alissa.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alissa.Core.Memory
{
    public class MemoryScorer
    {
        private const double RECENCY_DIVISOR = 1.0;
        private const int BOOST_BASE = 1;

        private readonly MemoryModel _memoryConfig;

        public MemoryScorer(MemoryModel memoryConfig)
        {
            _memoryConfig = memoryConfig;
        }

        public List<MemoryEntry> ScoreAndRank(List<MemoryEntry> entries, int count)
        {
            List<MemoryEntry> results = entries
                .Where(e => e.Relevance >= _memoryConfig.ImportanceThreshold)
                .OrderByDescending(m => CalculateScore(m))
                .ThenByDescending(m => m.Timestamp)
                .Take(count)
                .ToList();

            return results;
        }

        public List<MemoryEntry> GetContextMemory(List<MemoryEntry> allMemory, int maxEntries, bool includeCore = true)
        {
            List<MemoryEntry> coreMemory = allMemory.Where(m => m.IsCoreMemory).ToList();
            List<MemoryEntry> ephemeralMemory = allMemory
                .Where(m => !m.IsCoreMemory && m.Relevance >= _memoryConfig.ImportanceThreshold)
                .OrderByDescending(m => CalculateScore(m))
                .ThenByDescending(m => m.Timestamp)
                .Take(maxEntries)
                .ToList();

            List<MemoryEntry> result;
            if (includeCore)
            {
                result = coreMemory.Concat(ephemeralMemory).ToList();
            }
            else
            {
                result = ephemeralMemory;
            }

            return result;
        }

        public List<MemoryEntry> FilterByThreshold(List<MemoryEntry> entries)
        {
            List<MemoryEntry> results = entries.Where(e => e.Relevance >= _memoryConfig.ImportanceThreshold).ToList();
            return results;
        }

        private double CalculateScore(MemoryEntry entry)
        {
            double relevance = entry.Relevance;
            double daysSince = (DateTime.Now - entry.Timestamp).TotalDays;
            double recencyBoost = RECENCY_DIVISOR + (RECENCY_DIVISOR / (BOOST_BASE + daysSince));
            double score = relevance * recencyBoost;

            return score;
        }
    }
}
