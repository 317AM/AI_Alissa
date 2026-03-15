using System;
using System.Collections.Generic;
using System.Text;

namespace Alissa.Core.Models
{
    public class LimitsModel
    {
        public int MaxConversationLength { get; set; }
        public int SummaryDivisionFactor { get; set; }
        public int MaxMessageLength { get; set; }
    }
}
