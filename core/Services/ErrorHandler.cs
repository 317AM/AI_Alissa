using Alissa.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alissa.Core.Services
{
    public static class ErrorHandler
    {
        // Constants
        private const string LOGS_DIR = "logs";
        private const string ERRORS_DIR = "errors";
        private const string ERROR_PREFIX = "error_";
        private const string TIMESTAMP_FORMAT = "yyyy-MM-dd_HH-mm-ss";
        private const string TEXT_EXTENSION = ".txt";

        public static ErrorResult Handle(
                Exception ex,
                string? basePath,
                bool verbose)
        {
            bool errorLogged = false;

            if (!string.IsNullOrWhiteSpace(basePath))
            {
                try
                {
                    string dir = Path.Combine(basePath, LOGS_DIR, ERRORS_DIR);
                    Directory.CreateDirectory(dir);

                    string path = Path.Combine(dir, ERROR_PREFIX + DateTime.Now.ToString(TIMESTAMP_FORMAT) + TEXT_EXTENSION);

                    File.WriteAllText(path, ex.ToString());
                    errorLogged = true;
                }
                catch
                {
                    errorLogged = false;
                }
            }

            if (verbose)
            {
                Console.WriteLine(ex.ToString());
            }

            ErrorResult result = new ErrorResult
            {
                IsFatal = true
            };
            return result;
        }
    }
}
