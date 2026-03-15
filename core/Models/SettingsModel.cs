using System;
using System.Collections.Generic;
using System.Text;

namespace Alissa.Core.Models
{
    public class SettingsModel
    {
        public bool EnableSummaries { get; set; }
        public bool EnableEmojiLogging { get; set; }
        public bool AutoRepairOnStart { get; set; }
    }
}
