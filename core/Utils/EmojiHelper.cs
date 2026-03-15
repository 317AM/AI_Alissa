using System.Text;

namespace Alissa.Core.Utils
{
    public static class EmojiUtils
    {
        public static void ExtractEmojis(string input, out string cleanedText, out string emojis)
        {
            if (string.IsNullOrEmpty(input))
            {
                cleanedText = string.Empty;
                emojis = string.Empty;
                return;
            }

            var textBuilder = new StringBuilder(input.Length);
            var emojiBuilder = new StringBuilder();

            int i = 0;
            while (i < input.Length)
            {
                if (!Rune.TryGetRuneAt(input, i, out var rune))
                {
                    textBuilder.Append(input[i]);
                    i++;
                }
                else
                {
                    int runeLength = rune.Utf16SequenceLength;

                    if (IsEmojiRune(rune))
                    {
                        emojiBuilder.Append(input.Substring(i, runeLength));
                    }
                    else
                    {
                        textBuilder.Append(input.Substring(i, runeLength));
                    }

                    i += runeLength;
                }
            }

            cleanedText = textBuilder.ToString();
            emojis = emojiBuilder.ToString();
        }

        private static bool IsEmojiRune(Rune r)
        {
            int v = r.Value;

            return
                (v >= 0x1F600 && v <= 0x1F64F) || // Emoticons
                (v >= 0x1F300 && v <= 0x1F5FF) || // Misc Symbols and Pictographs
                (v >= 0x1F680 && v <= 0x1F6FF) || // Transport & Map
                (v >= 0x2600 && v <= 0x26FF) || // Misc symbols
                (v >= 0x2700 && v <= 0x27BF) || // Dingbats
                (v >= 0x1F900 && v <= 0x1F9FF) || // Supplemental Symbols and Pictographs
                (v >= 0x1FA70 && v <= 0x1FAFF) || // Symbols and Pictographs Extended-A
                (v >= 0x1F1E6 && v <= 0x1F1FF) || // Regional indicators (flags)
                (v >= 0x1F700 && v <= 0x1F77F);   // Additional block
        }
    }
}
