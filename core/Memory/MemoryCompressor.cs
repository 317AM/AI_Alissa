using Alissa.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alissa.Core.Memory
{
    public class MemoryCompressor
    {
        private readonly MemoryModel _memoryConfig;

        public MemoryCompressor(MemoryModel memoryConfig)
        {
            _memoryConfig = memoryConfig;
        }

        public void CompressMemory(List<MemoryEntry> entries)
        {
            foreach (var entry in entries.Where(e => !e.IsCoreMemory))
            {
                if ((DateTime.Now - entry.Timestamp).TotalDays > 7 && entry.Relevance < 0.3)
                {
                    entry.Relevance *= _memoryConfig.CompressionFactor;
                }
            }
        }

        public List<MemoryEntry> EnforceCapacity(List<MemoryEntry> entries)
        {
            var core = entries.Where(e => e.IsCoreMemory).ToList();
            var ephemeral = entries
                .Where(e => !e.IsCoreMemory)
                .OrderByDescending(e => e.Relevance)
                .ThenByDescending(e => e.Timestamp)
                .ToList();

            int coreCount = core.Count;
            int availableCapacity = Math.Max(1, _memoryConfig.MaxLongTermEntries - coreCount);

            var kept = ephemeral.Take(availableCapacity).ToList();
            return core.Concat(kept).OrderByDescending(e => e.Timestamp).ToList();
        }
    }
}
