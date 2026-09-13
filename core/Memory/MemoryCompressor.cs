using Alissa.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alissa.Core.Memory
{
    public class MemoryCompressor
    {
        private const double SEVEN_DAYS = 7.0;
        private const double RELEVANCE_THRESHOLD = 0.3;
        private const int MIN_CAPACITY = 1;

        private readonly MemoryModel _memoryConfig;

        public MemoryCompressor(MemoryModel memoryConfig)
        {
            _memoryConfig = memoryConfig;
        }

        public void CompressMemory(List<MemoryEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                MemoryEntry entry = entries[i];
                bool isNotCore = !entry.IsCoreMemory;
                if (!isNotCore)
                {
                    continue;
                }

                double daysSinceUpdate = (DateTime.Now - entry.Timestamp).TotalDays;
                bool isOldAndLowRelevance = daysSinceUpdate > SEVEN_DAYS && entry.Relevance < RELEVANCE_THRESHOLD;
                if (isOldAndLowRelevance)
                {
                    entry.Relevance *= _memoryConfig.CompressionFactor;
                }
            }
        }

        public List<MemoryEntry> EnforceCapacity(List<MemoryEntry> entries)
        {
            List<MemoryEntry> core = new List<MemoryEntry>();
            List<MemoryEntry> ephemeral = new List<MemoryEntry>();

            for (int i = 0; i < entries.Count; i++)
            {
                MemoryEntry entry = entries[i];
                if (entry.IsCoreMemory)
                {
                    core.Add(entry);
                }
                else
                {
                    ephemeral.Add(entry);
                }
            }

            List<MemoryEntry> sortedEphemeral = ephemeral
                .OrderByDescending(e => e.Relevance)
                .ThenByDescending(e => e.Timestamp)
                .ToList();

            int coreCount = core.Count;
            int availableCapacity = Math.Max(MIN_CAPACITY, _memoryConfig.MaxLongTermEntries - coreCount);

            List<MemoryEntry> kept = sortedEphemeral.Take(availableCapacity).ToList();
            List<MemoryEntry> result = core.Concat(kept).OrderByDescending(e => e.Timestamp).ToList();

            return result;
        }
    }
}
