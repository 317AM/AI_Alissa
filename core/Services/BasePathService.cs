using System;
using System.Collections.Generic;
using System.Text;

namespace Alissa.Core.Services
{
    public static class PathService
    {
        // Constants
        private const string CONFIG_DIR = "config";

        public static string ResolveBasePath()
        {
            string basePath = AppContext.BaseDirectory;

            bool hasConfig = Directory.Exists(Path.Combine(basePath, CONFIG_DIR));
            while (!hasConfig)
            {
                DirectoryInfo? parentDir = Directory.GetParent(basePath);
                if (parentDir == null)
                {
                    break;
                }

                basePath = parentDir.FullName;
                hasConfig = Directory.Exists(Path.Combine(basePath, CONFIG_DIR));
            }

            return basePath;
        }
    }
}
