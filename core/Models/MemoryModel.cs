using System;
using System.Collections.Generic;
using System.Text;

namespace Alissa.Core.Models
{
    public class MemoryModel
    {
        public int MaxShortTermEntries { get; set; }
        public int MaxLongTermEntries { get; set; }
        public double ImportanceThreshold { get; set; }
        public double CompressionFactor { get; set; }
    }
}
