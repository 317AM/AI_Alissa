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
            Dictionary<string, List<MemoryEntry>> index = new Dictionary<string, List<MemoryEntry>>();

            for (int i = 0; i < entries.Count; i++)
            {
                MemoryEntry entry = entries[i];
                bool hasKey = index.ContainsKey(entry.Key);
                if (!hasKey)
                {
                    index[entry.Key] = new List<MemoryEntry>();
                }
                index[entry.Key].Add(entry);
            }

            return index;
        }
    }
}
