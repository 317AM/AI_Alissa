//=======================================================================================================================//
//  CLASS: MemoryLearningHelper
// -------------------------------------------------------------------------------------------------------------------- //
//
//  Responsibility:
//      Provides static methods for extracting and saving user/AI memory patterns from chat messages.
//
//  Role in System:
//      Used by Program.cs to update memory based on message content.
//
//  State:
//      None (static class)
//
//  Behavior:
//      - ExtractAndSaveUserMemory: Detects and saves user memory patterns
//      - ExtractAndSaveAIMemory: Detects and saves AI memory patterns
//
//  Notes:
//      No side effects except memory updates via IMemoryManager.
//
//=======================================================================================================================//
using Alissa.Core.Interfaces;
using Alissa.Core.Models;

namespace Alissa.Core.Utils
{
    public static class MemoryLearningHelper
    {
        // -------------------------------------------------------------------------------------------------------------------- //
        //  METHOD: ExtractAndSaveUserMemory
        // -------------------------------------------------------------------------------------------------------------------- //
        //  Purpose: Extracts user memory patterns and saves them to memory manager.
        //  Parameters: input - user message; memoryManager - memory manager instance
        //  Returns: void
        // -------------------------------------------------------------------------------------------------------------------- //
        public static void ExtractAndSaveUserMemory(string input, IMemoryManager memoryManager)
        {
            bool hasNamePattern = input.Contains("my name is", System.StringComparison.OrdinalIgnoreCase);
            {
                if (hasNamePattern)
                {
                    int index = input.IndexOf("my name is", System.StringComparison.OrdinalIgnoreCase);
                    bool indexIsValid = index != -1;
                    {
                        if (indexIsValid)
                        {
                            var name = input.Substring(index + "my name is".Length).Trim();
                            memoryManager.SaveUserProfile(new MemoryEntry("user_name", name, 1.0, true));
                        }
                    }
                }
            }

            bool hasPreferencePattern = input.Contains("i prefer", System.StringComparison.OrdinalIgnoreCase);
            {
                if (hasPreferencePattern)
                {
                    int index = input.IndexOf("i prefer", System.StringComparison.OrdinalIgnoreCase);
                    bool indexIsValid = index != -1;
                    {
                        if (indexIsValid)
                        {
                            var pref = input.Substring(index + "i prefer".Length).Trim();
                            memoryManager.SaveUserProfile(new MemoryEntry("user_preference", pref, 0.9, false));
                        }
                    }
                }
            }

            bool hasRememberPattern = input.Contains("remember that", System.StringComparison.OrdinalIgnoreCase);
            {
                if (hasRememberPattern)
                {
                    int index = input.IndexOf("remember that", System.StringComparison.OrdinalIgnoreCase);
                    bool indexIsValid = index != -1;
                    {
                        if (indexIsValid)
                        {
                            var fact = input.Substring(index + "remember that".Length).Trim();
                            memoryManager.SaveFact(new MemoryEntry("user_fact", fact, 0.8, false));
                        }
                    }
                }
            }
        }

        // -------------------------------------------------------------------------------------------------------------------- //
        //  METHOD: ExtractAndSaveAIMemory
        // -------------------------------------------------------------------------------------------------------------------- //
        //  Purpose: Extracts AI memory patterns and saves them to memory manager.
        //  Parameters: input - AI message; memoryManager - memory manager instance
        //  Returns: void
        // -------------------------------------------------------------------------------------------------------------------- //
        public static void ExtractAndSaveAIMemory(string input, IMemoryManager memoryManager)
        {
            bool hasPreferencePattern = input.Contains("i prefer", System.StringComparison.OrdinalIgnoreCase);
            {
                if (hasPreferencePattern)
                {
                    int index = input.IndexOf("i prefer", System.StringComparison.OrdinalIgnoreCase);
                    bool indexIsValid = index != -1;
                    {
                        if (indexIsValid)
                        {
                            var skill = input.Substring(index + "i prefer".Length).Trim();
                            memoryManager.SaveSkill(new MemoryEntry("ai_skill", skill, 0.9, false));
                        }
                    }
                }
            }

            bool hasNamePattern = input.Contains("my name is", System.StringComparison.OrdinalIgnoreCase);
            {
                if (hasNamePattern)
                {
                    int index = input.IndexOf("my name is", System.StringComparison.OrdinalIgnoreCase);
                    bool indexIsValid = index != -1;
                    {
                        if (indexIsValid)
                        {
                            var aiName = input.Substring(index + "my name is".Length).Trim();
                            memoryManager.SaveSystemLearning(new MemoryEntry("ai_name", aiName, 1.0, true));
                        }
                    }
                }
            }

            bool hasRememberPattern = input.Contains("remember that", System.StringComparison.OrdinalIgnoreCase);
            {
                if (hasRememberPattern)
                {
                    int index = input.IndexOf("remember that", System.StringComparison.OrdinalIgnoreCase);
                    bool indexIsValid = index != -1;
                    {
                        if (indexIsValid)
                        {
                            var aiFact = input.Substring(index + "remember that".Length).Trim();
                            memoryManager.SaveSystemLearning(new MemoryEntry("ai_fact", aiFact, 0.8, false));
                        }
                    }
                }
            }
        }
    }
}
