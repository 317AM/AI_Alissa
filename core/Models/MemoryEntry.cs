using System;

namespace Alissa.Core.Models
{
    public class MemoryEntry
    {
        public string Key { get; set; } = string.Empty;       
        public string Value { get; set; } = string.Empty;    
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public double Relevance { get; set; } = 1.0;        
        public bool IsCoreMemory { get; set; } = false;      

        public MemoryEntry() { }

        public MemoryEntry(string key, string value, double relevance = 1.0, bool isCore = false)
        {
            Key = key;
            Value = value;
            Relevance = relevance;
            IsCoreMemory = isCore;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{Key}: {Value} (Relevance={Relevance:F2}, Core={IsCoreMemory}, {Timestamp:yyyy-MM-dd HH:mm:ss})";
        }

    }
}
