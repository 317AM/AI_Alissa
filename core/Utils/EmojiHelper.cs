using System.Text;

namespace Alissa.Core.Utils
{
    public static class EmojiUtils
    {
        // Constants - Emoji Unicode Ranges
        private const int EMOTICONS_START = 0x1F600;
        private const int EMOTICONS_END = 0x1F64F;
        private const int MISC_SYMBOLS_START = 0x1F300;
        private const int MISC_SYMBOLS_END = 0x1F5FF;
        private const int TRANSPORT_START = 0x1F680;
        private const int TRANSPORT_END = 0x1F6FF;
        private const int SYMBOLS_START = 0x2600;
        private const int SYMBOLS_END = 0x26FF;
        private const int DINGBATS_START = 0x2700;
        private const int DINGBATS_END = 0x27BF;
        private const int SUPPLEMENTAL_START = 0x1F900;
        private const int SUPPLEMENTAL_END = 0x1F9FF;
        private const int EXTENDED_A_START = 0x1FA70;
        private const int EXTENDED_A_END = 0x1FAFF;
        private const int REGIONAL_START = 0x1F1E6;
        private const int REGIONAL_END = 0x1F1FF;
        private const int ADDITIONAL_START = 0x1F700;
        private const int ADDITIONAL_END = 0x1F77F;

        public static void ExtractEmojis(string input, out string cleanedText, out string emojis)
        {
            bool inputIsEmpty = string.IsNullOrEmpty(input);
            if (inputIsEmpty)
            {
                cleanedText = string.Empty;
                emojis = string.Empty;
                return;
            }

            StringBuilder textBuilder = new StringBuilder(input.Length);
            StringBuilder emojiBuilder = new StringBuilder();

            int i = 0;
            while (i < input.Length)
            {
                bool runeFound = Rune.TryGetRuneAt(input, i, out Rune rune);
                if (!runeFound)
                {
                    textBuilder.Append(input[i]);
                    i++;
                }
                else
                {
                    int runeLength = rune.Utf16SequenceLength;
                    bool isEmoji = IsEmojiRune(rune);
                    if (isEmoji)
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

            bool emoticons = (v >= EMOTICONS_START && v <= EMOTICONS_END);
            if (emoticons)
            {
                return true;
            }

            bool miscSymbols = (v >= MISC_SYMBOLS_START && v <= MISC_SYMBOLS_END);
            if (miscSymbols)
            {
                return true;
            }

            bool transport = (v >= TRANSPORT_START && v <= TRANSPORT_END);
            if (transport)
            {
                return true;
            }

            bool symbols = (v >= SYMBOLS_START && v <= SYMBOLS_END);
            if (symbols)
            {
                return true;
            }

            bool dingbats = (v >= DINGBATS_START && v <= DINGBATS_END);
            if (dingbats)
            {
                return true;
            }

            bool supplemental = (v >= SUPPLEMENTAL_START && v <= SUPPLEMENTAL_END);
            if (supplemental)
            {
                return true;
            }

            bool extendedA = (v >= EXTENDED_A_START && v <= EXTENDED_A_END);
            if (extendedA)
            {
                return true;
            }

            bool regionalIndicators = (v >= REGIONAL_START && v <= REGIONAL_END);
            if (regionalIndicators)
            {
                return true;
            }

            bool additionalBlock = (v >= ADDITIONAL_START && v <= ADDITIONAL_END);
            if (additionalBlock)
            {
                return true;
            }

            return false;
        }
    }
}
