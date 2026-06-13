using System.Text;

namespace Alissa.Core.Utils
{
    public static class EmojiUtils
    {
        public static void ExtractEmojis(string input, out string cleanedText, out string emojis)
        {
            bool inputIsEmpty = string.IsNullOrEmpty(input);
            {
                if (inputIsEmpty)
                {
                    cleanedText = string.Empty;
                    emojis = string.Empty;
                }
                else
                {
                    var textBuilder = new StringBuilder(input.Length);
                    var emojiBuilder = new StringBuilder();

                    int i = 0;
                    while (i < input.Length)
                    {
                        bool runeFound = Rune.TryGetRuneAt(input, i, out var rune);
                        {
                            if (!runeFound)
                            {
                                textBuilder.Append(input[i]);
                                i++;
                            }
                            else
                            {
                                int runeLength = rune.Utf16SequenceLength;

                                bool isEmoji = IsEmojiRune(rune);
                                {
                                    if (isEmoji)
                                    {
                                        emojiBuilder.Append(input.Substring(i, runeLength));
                                    }
                                    else
                                    {
                                        textBuilder.Append(input.Substring(i, runeLength));
                                    }
                                }

                                i += runeLength;
                            }
                        }
                    }

                    cleanedText = textBuilder.ToString();
                    emojis = emojiBuilder.ToString();
                }
            }
        }

        private static bool IsEmojiRune(Rune r)
        {
            int v = r.Value;

            bool isEmoji = false;
            {
                bool emoticons = (v >= 0x1F600 && v <= 0x1F64F);
                {
                    if (emoticons)
                    {
                        isEmoji = true;
                    }
                }

                bool miscSymbols = (v >= 0x1F300 && v <= 0x1F5FF);
                {
                    if (miscSymbols && !isEmoji)
                    {
                        isEmoji = true;
                    }
                }

                bool transport = (v >= 0x1F680 && v <= 0x1F6FF);
                {
                    if (transport && !isEmoji)
                    {
                        isEmoji = true;
                    }
                }

                bool symbols = (v >= 0x2600 && v <= 0x26FF);
                {
                    if (symbols && !isEmoji)
                    {
                        isEmoji = true;
                    }
                }

                bool dingbats = (v >= 0x2700 && v <= 0x27BF);
                {
                    if (dingbats && !isEmoji)
                    {
                        isEmoji = true;
                    }
                }

                bool supplemental = (v >= 0x1F900 && v <= 0x1F9FF);
                {
                    if (supplemental && !isEmoji)
                    {
                        isEmoji = true;
                    }
                }

                bool extendedA = (v >= 0x1FA70 && v <= 0x1FAFF);
                {
                    if (extendedA && !isEmoji)
                    {
                        isEmoji = true;
                    }
                }

                bool regionalIndicators = (v >= 0x1F1E6 && v <= 0x1F1FF);
                {
                    if (regionalIndicators && !isEmoji)
                    {
                        isEmoji = true;
                    }
                }

                bool additionalBlock = (v >= 0x1F700 && v <= 0x1F77F);
                {
                    if (additionalBlock && !isEmoji)
                    {
                        isEmoji = true;
                    }
                }
            }

            return isEmoji;
        }
    }
}
