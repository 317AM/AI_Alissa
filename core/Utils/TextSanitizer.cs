using System;
using System.Text.RegularExpressions;

namespace Alissa.Core.Utils
{
    /// <summary>
    /// Utility for sanitizing text before speech synthesis.
    /// Removes markdown, code blocks, and emoji that don't synthesize well.
    /// </summary>
    public static class TextSanitizer
    {
        // Constants for regex patterns
        private const string CODE_BLOCK_PATTERN = @"```[\s\S]*?```";
        private const string INLINE_CODE_PATTERN = @"`[^`]*`";
        private const string BOLD_PATTERN = @"\*\*([^\*]+)\*\*";
        private const string BOLD_REPLACEMENT = "$1";
        private const string ITALIC_PATTERN = @"\*([^\*]+)\*";
        private const string ITALIC_REPLACEMENT = "$1";
        private const string STRONG_PATTERN = @"__([^_]+)__";
        private const string STRONG_REPLACEMENT = "$1";
        private const string EM_PATTERN = @"_([^_]+)_";
        private const string EM_REPLACEMENT = "$1";
        private const string LINK_PATTERN = @"\[([^\]]+)\]\([^\)]+\)";
        private const string LINK_REPLACEMENT = "$1";
        private const string HEADING_PATTERN = @"^#+\s+";
        private const string SPECIAL_CHAR_PATTERN = @"[^\w\s\.\,\!\?\-\:\;\'\""\/]";
        private const string WHITESPACE_PATTERN = @"\s+";
        private const string WHITESPACE_REPLACEMENT = " ";

        /// <summary>
        /// Strips formatting, code blocks, and emoji from text for speech synthesis.
        /// </summary>
        /// <param name="text">Raw text (potentially with markdown, code, emoji)</param>
        /// <returns>Clean text suitable for TTS</returns>
        public static string StripForSpeech(string text)
        {
            bool textIsEmpty = string.IsNullOrWhiteSpace(text);
            if (textIsEmpty)
            {
                return string.Empty;
            }

            string result = text;

            // Remove code blocks (triple backticks)
            result = Regex.Replace(result, CODE_BLOCK_PATTERN, string.Empty, RegexOptions.Multiline);

            // Remove inline code (single backticks)
            result = Regex.Replace(result, INLINE_CODE_PATTERN, string.Empty);

            // Remove markdown bold/italic (**text**, *text*, __text__, _text_)
            result = Regex.Replace(result, BOLD_PATTERN, BOLD_REPLACEMENT);
            result = Regex.Replace(result, ITALIC_PATTERN, ITALIC_REPLACEMENT);
            result = Regex.Replace(result, STRONG_PATTERN, STRONG_REPLACEMENT);
            result = Regex.Replace(result, EM_PATTERN, EM_REPLACEMENT);

            // Remove markdown links [text](url)
            result = Regex.Replace(result, LINK_PATTERN, LINK_REPLACEMENT);

            // Remove markdown headings (#, ##, etc.)
            result = Regex.Replace(result, HEADING_PATTERN, string.Empty, RegexOptions.Multiline);

            // Remove emoji and other special Unicode characters (rough approximation)
            // Keeps basic punctuation and alphanumerics
            result = Regex.Replace(result, SPECIAL_CHAR_PATTERN, string.Empty);

            // Collapse multiple spaces
            result = Regex.Replace(result, WHITESPACE_PATTERN, WHITESPACE_REPLACEMENT);

            // Trim
            result = result.Trim();

            return result;
        }
    }
}
