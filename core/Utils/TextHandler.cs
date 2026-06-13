//=======================================================================================================================//
//  CLASS: TextHandler
// -------------------------------------------------------------------------------------------------------------------- //
//
//  Responsibility:
//      Provides utilities for console input/output handling with consistent formatting.
//
//  Role in System:
//      Used by Program.cs and other classes for consistent text I/O operations.
//
//  State:
//      None (static class)
//
//  Behavior:
//      - PrintText: Prints text to console
//      - NextLine: Prints empty line
//      - ReadText: Reads multi-line input from console
//      - ReadLine: Reads single line input
//
//  Notes:
//      Can be extended for logging or file output in the future.
//
//=======================================================================================================================//
namespace Alissa.Core.Utils
{
    public static class TextHandler
    {
        /// <summary>
        /// Prints text to console without newline.
        /// </summary>
        public static void PrintText(string text)
        {
            PrintText(text, false);
        }

        /// <summary>
        /// Prints text to console with optional newline.
        /// </summary>
        public static void PrintText(string text, bool appendNewline)
        {
            if (appendNewline)
            {
                Console.WriteLine(text);
            }
            else
            {
                Console.Write(text);
            }
        }

        /// <summary>
        /// Prints an empty line to console.
        /// </summary>
        public static void NextLine()
        {
            Console.WriteLine();
        }

        /// <summary>
        /// Reads a single line from console input.
        /// </summary>
        public static string ReadLine()
        {
            string? input = Console.ReadLine();
            return input ?? string.Empty;
        }

        /// <summary>
        /// Reads multi-line text from console until empty line is encountered.
        /// </summary>
        public static string ReadText()
        {
            var input = new System.Text.StringBuilder();
            bool isReadingInput = true;

            while (isReadingInput)
            {
                string? currentLine = Console.ReadLine();
                bool hasContent = !string.IsNullOrEmpty(currentLine);

                if (hasContent)
                {
                    input.Append(currentLine);
                    input.Append("\n");
                }
                else
                {
                    isReadingInput = false;
                }
            }

            string result = input.ToString().TrimEnd('\n');
            return result;
        }

        /// <summary>
        /// Trims and sanitizes user input for processing.
        /// </summary>
        public static string SanitizeInput(string input)
        {
            bool hasText = !string.IsNullOrWhiteSpace(input);
            return hasText ? input.Trim() : string.Empty;
        }

        /// <summary>
        /// Checks if input is a valid command (starts with special character).
        /// </summary>
        public static bool IsCommand(string input)
        {
            bool hasContent = !string.IsNullOrWhiteSpace(input);
            bool startsWithSlash = hasContent && input.TrimStart().StartsWith("/");
            return startsWithSlash;
        }
    }
}
