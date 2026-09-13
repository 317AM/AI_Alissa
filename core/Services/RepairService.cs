using System;
using System.Collections.Generic;
using System.Text;

namespace Alissa.Core.Services
{
    public static class RepairService
    {
        // Constants
        private const string CONFIG_DIR = "config";
        private const string LOGS_DIR = "logs";
        private const string CONVERSATIONS_SUBDIR = "conversations";
        private const string SUMMARIES_SUBDIR = "summaries";
        private const string ERRORS_SUBDIR = "errors";
        private const string EMPTY_JSON = " {}";

        public static void EnsureStructure(string basePath)
        {
            string configDir = Path.Combine(basePath, CONFIG_DIR);
            string logsDir = Path.Combine(basePath, LOGS_DIR);

            Directory.CreateDirectory(configDir);
            Directory.CreateDirectory(Path.Combine(logsDir, CONVERSATIONS_SUBDIR));
            Directory.CreateDirectory(Path.Combine(logsDir, SUMMARIES_SUBDIR));
            Directory.CreateDirectory(Path.Combine(logsDir, ERRORS_SUBDIR));

            string[] requiredFiles = new string[]
            {
                "model.json",
                "settings.json",
                "limits.json"
            };

            for (int i = 0; i < requiredFiles.Length; i++)
            {
                string file = requiredFiles[i];
                string path = Path.Combine(configDir, file);
                bool fileExists = File.Exists(path);
                if (!fileExists)
                {
                    File.WriteAllText(path, EMPTY_JSON);
                }
            }
        }
    }
}
