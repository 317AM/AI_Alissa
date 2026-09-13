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
        // Constants
        private const string MY_NAME_IS = "my name is";
        private const string I_PREFER = "i prefer";
        private const string REMEMBER_THAT = "remember that";
        private const string USER_NAME_KEY = "user_name";
        private const double USER_NAME_RELEVANCE = 1.0;
        private const string USER_PREFERENCE_KEY = "user_preference";
        private const double USER_PREFERENCE_RELEVANCE = 0.9;
        private const string USER_FACT_KEY = "user_fact";
        private const double USER_FACT_RELEVANCE = 0.8;
        private const string AI_SKILL_KEY = "ai_skill";
        private const double AI_SKILL_RELEVANCE = 0.9;
        private const string AI_NAME_KEY = "ai_name";
        private const double AI_NAME_RELEVANCE = 1.0;
        private const string AI_FACT_KEY = "ai_fact";
        private const double AI_FACT_RELEVANCE = 0.8;
        private const int NOT_FOUND = -1;

        // -------------------------------------------------------------------------------------------------------------------- //
        //  METHOD: ExtractAndSaveUserMemory
        // -------------------------------------------------------------------------------------------------------------------- //
        //  Purpose: Extracts user memory patterns and saves them to memory manager.
        //  Parameters: input - user message; memoryManager - memory manager instance
        //  Returns: void
        // -------------------------------------------------------------------------------------------------------------------- //
        public static void ExtractAndSaveUserMemory(string input, IMemoryManager memoryManager)
        {
            bool hasNamePattern = input.Contains(MY_NAME_IS, System.StringComparison.OrdinalIgnoreCase);
            if (hasNamePattern)
            {
                int index = input.IndexOf(MY_NAME_IS, System.StringComparison.OrdinalIgnoreCase);
                bool indexIsValid = index != NOT_FOUND;
                if (indexIsValid)
                {
                    string name = input.Substring(index + MY_NAME_IS.Length).Trim();
                    memoryManager.SaveUserProfile(new MemoryEntry(USER_NAME_KEY, name, USER_NAME_RELEVANCE, isCore: true));
                }
            }

            bool hasPreferencePattern = input.Contains(I_PREFER, System.StringComparison.OrdinalIgnoreCase);
            if (hasPreferencePattern)
            {
                int index = input.IndexOf(I_PREFER, System.StringComparison.OrdinalIgnoreCase);
                bool indexIsValid = index != NOT_FOUND;
                if (indexIsValid)
                {
                    string pref = input.Substring(index + I_PREFER.Length).Trim();
                    memoryManager.SaveUserProfile(new MemoryEntry(USER_PREFERENCE_KEY, pref, USER_PREFERENCE_RELEVANCE, isCore: false));
                }
            }

            bool hasRememberPattern = input.Contains(REMEMBER_THAT, System.StringComparison.OrdinalIgnoreCase);
            if (hasRememberPattern)
            {
                int index = input.IndexOf(REMEMBER_THAT, System.StringComparison.OrdinalIgnoreCase);
                bool indexIsValid = index != NOT_FOUND;
                if (indexIsValid)
                {
                    string fact = input.Substring(index + REMEMBER_THAT.Length).Trim();
                    memoryManager.SaveFact(new MemoryEntry(USER_FACT_KEY, fact, USER_FACT_RELEVANCE, isCore: false));
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
            bool hasPreferencePattern = input.Contains(I_PREFER, System.StringComparison.OrdinalIgnoreCase);
            if (hasPreferencePattern)
            {
                int index = input.IndexOf(I_PREFER, System.StringComparison.OrdinalIgnoreCase);
                bool indexIsValid = index != NOT_FOUND;
                if (indexIsValid)
                {
                    string skill = input.Substring(index + I_PREFER.Length).Trim();
                    memoryManager.SaveSkill(new MemoryEntry(AI_SKILL_KEY, skill, AI_SKILL_RELEVANCE, isCore: false));
                }
            }

            bool hasNamePattern = input.Contains(MY_NAME_IS, System.StringComparison.OrdinalIgnoreCase);
            if (hasNamePattern)
            {
                int index = input.IndexOf(MY_NAME_IS, System.StringComparison.OrdinalIgnoreCase);
                bool indexIsValid = index != NOT_FOUND;
                if (indexIsValid)
                {
                    string aiName = input.Substring(index + MY_NAME_IS.Length).Trim();
                    memoryManager.SaveSystemLearning(new MemoryEntry(AI_NAME_KEY, aiName, AI_NAME_RELEVANCE, isCore: true));
                }
            }

            bool hasRememberPattern = input.Contains(REMEMBER_THAT, System.StringComparison.OrdinalIgnoreCase);
            if (hasRememberPattern)
            {
                int index = input.IndexOf(REMEMBER_THAT, System.StringComparison.OrdinalIgnoreCase);
                bool indexIsValid = index != NOT_FOUND;
                if (indexIsValid)
                {
                    string aiFact = input.Substring(index + REMEMBER_THAT.Length).Trim();
                    memoryManager.SaveSystemLearning(new MemoryEntry(AI_FACT_KEY, aiFact, AI_FACT_RELEVANCE, isCore: false));
                }
            }
        }
    }
}
