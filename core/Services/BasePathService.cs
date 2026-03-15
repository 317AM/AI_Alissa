using System;
using System.Collections.Generic;
using System.Text;

namespace Alissa.Core.Services
{
    public static class PathService
    {
        public static string ResolveBasePath()
        {
            string basePath = AppContext.BaseDirectory;

            while (!Directory.Exists(Path.Combine(basePath, "config")))
            {
                basePath = Directory.GetParent(basePath)!.FullName;
            }

            return basePath;
        }
    }
}
