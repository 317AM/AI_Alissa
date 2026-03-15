using System;
using System.Collections.Generic;
using System.Text;

namespace Alissa.Core.Services
{
    public static class RepairService
    {
        public static void EnsureStructure(string basePath)
        {
            string configDir = Path.Combine(basePath, "config");
            string logsDir = Path.Combine(basePath, "logs");

            Directory.CreateDirectory(configDir);
            Directory.CreateDirectory(Path.Combine(logsDir, "conversations"));
            Directory.CreateDirectory(Path.Combine(logsDir, "summaries"));
            Directory.CreateDirectory(Path.Combine(logsDir, "errors"));

            string[] requiredFiles =
            {
            "model.json",
            "settings.json",
            "limits.json"
        };

            foreach (var file in requiredFiles)
            {
                string path = Path.Combine(configDir, file);
                if (!File.Exists(path))
                    File.WriteAllText(path, " {}");
            }
        }
    }

}
