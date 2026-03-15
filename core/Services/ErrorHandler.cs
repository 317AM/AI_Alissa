using Alissa.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alissa.Core.Services
{
    public static class ErrorHandler
    {
        public static ErrorResult Handle(
            Exception ex,
            string basePath,
            bool verbose)
        {
            try
            {
                string dir = Path.Combine(basePath, "logs", "errors");
                Directory.CreateDirectory(dir);

                string path = Path.Combine(dir,
                    $"error_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

                File.WriteAllText(path, ex.ToString());
            }
            catch { }

            if (verbose)
                Console.WriteLine(ex.ToString());

            return new ErrorResult
            {
                IsFatal = true
            };
        }
    }
}
