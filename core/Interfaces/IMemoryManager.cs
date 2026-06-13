using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using System.Collections.Generic;

namespace Alissa.Core.Interfaces
{
    public interface IMemoryManager
    {
        // Session cache (short-term)
        void SaveSessionCache(List<Message> messages);
        List<Message> LoadSessionCache();

        // Facts (long-term)
        void SaveFact(MemoryEntry entry);
        List<MemoryEntry> LoadFacts();

        // User profile (long-term)
        void SaveUserProfile(MemoryEntry entry);
        List<MemoryEntry> LoadUserProfile();

        // System learnings (long-term)
        void SaveSystemLearning(MemoryEntry entry);
        List<MemoryEntry> LoadSystemLearnings();

        // Conversation summaries
        void SaveConversationSummary(ConversationSummary summary);
        List<ConversationSummary> LoadConversationSummaries();

        // General memory operations
        void SaveMemory(MemoryEntry entry);
        List<MemoryEntry> LoadMemory(string key = "");
        List<MemoryEntry> GetRelevantMemory(int maxEntries);
        List<MemoryEntry> LoadTopMemories(int count = 10);
        List<MemoryEntry> LoadContextMemory(int maxEntries, bool includeCore = true);
        void DeleteMemory(string key);
        void SummarizeMemory();
        // Skills (long-term)
        void SaveSkill(MemoryEntry entry);
        List<MemoryEntry> LoadSkills();
    }
}

