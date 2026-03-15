using System;
using System.Collections.Generic;
using System.Text;

namespace Alissa.Core.Models
{
    public class AppConfig
    {
        public ConfigModel Model { get; set; } = null!;
        public SettingsModel Settings { get; set; } = null!;
        public LimitsModel Limits { get; set; } = null!;
        public MemoryModel Memory { get; set; } = null!;
    }
}
