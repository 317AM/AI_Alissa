using Alissa.Core.Models;
using System.Collections.Generic;

namespace Alissa.Core.Memory
{
    public class MemoryIndexer
    {
        private readonly MemoryModel _memoryConfig;

        public MemoryIndexer(MemoryModel memoryConfig)
        {
            _memoryConfig = memoryConfig;
        }

        public Dictionary<string, List<MemoryEntry>> BuildIndex(List<MemoryEntry> entries)
        {
            var index = new Dictionary<string, List<MemoryEntry>>();

            foreach (var entry in entries)
            {
                if (!index.ContainsKey(entry.Key))
                {
                    index[entry.Key] = new List<MemoryEntry>();
                }
                index[entry.Key].Add(entry);
            }

            return index;
        }
    }
}
