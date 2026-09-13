using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Memory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alissa.Core.Services
{
    public class MemoryManager : IMemoryManager
    {
        // Constants
        private const int DEFAULT_MAX_ENTRIES = 10;

        private readonly MemoryStore _store;
        private readonly MemoryScorer _scorer;
        private readonly MemoryCompressor _compressor;
        private readonly MemoryIndexer _indexer;

        public MemoryManager(string basePath, MemoryModel memoryConfig)
        {
            _store = new MemoryStore(basePath);
            _scorer = new MemoryScorer(memoryConfig);
            _compressor = new MemoryCompressor(memoryConfig);
            _indexer = new MemoryIndexer(memoryConfig);
        }

        // Session Cache Operations
        public void SaveSessionCache(List<Message> messages)
        {
            _store.SaveSessionCache(messages);
        }

        public List<Message> LoadSessionCache()
        {
            return _store.LoadSessionCache();
        }

        // Fact Operations
        public void SaveMemory(MemoryEntry entry)
        {
            List<MemoryEntry> facts = _store.LoadFacts();
            bool exists = facts.Any(f => f.Key == entry.Key && f.Value == entry.Value);
            if (!exists)
            {
                facts.Add(entry);
            }
            _store.SaveFacts(facts);
        }

        public void SaveFact(MemoryEntry entry)
        {
            SaveMemory(entry);
        }

        public List<MemoryEntry> LoadFacts()
        {
            return _store.LoadFacts();
        }

        // User Profile Operations
        public void SaveUserProfile(MemoryEntry entry)
        {
            List<MemoryEntry> profile = _store.LoadUserProfile();
            MemoryEntry? existing = profile.FirstOrDefault(p => p.Key == entry.Key);
            if (existing != null)
            {
                profile.Remove(existing);
            }
            profile.Add(entry);
            _store.SaveUserProfile(profile);
        }

        public List<MemoryEntry> LoadUserProfile()
        {
            return _store.LoadUserProfile();
        }

        // System Learning Operations
        public void SaveSystemLearning(MemoryEntry entry)
        {
            List<MemoryEntry> learnings = _store.LoadSystemLearnings();
            bool exists = learnings.Any(l => l.Key == entry.Key && l.Value == entry.Value);
            if (!exists)
            {
                learnings.Add(entry);
            }
            _store.SaveSystemLearnings(learnings);
        }

        public List<MemoryEntry> LoadSystemLearnings()
        {
            return _store.LoadSystemLearnings();
        }

        // Conversation Summary Operations
        public void SaveConversationSummary(ConversationSummary summary)
        {
            List<ConversationSummary> summaries = _store.LoadConversationSummaries();
            summaries.Add(summary);
            _store.SaveConversationSummaries(summaries);
        }

        public List<ConversationSummary> LoadConversationSummaries()
        {
            return _store.LoadConversationSummaries();
        }

        // Skills Operations
        public void SaveSkill(MemoryEntry entry)
        {
            List<MemoryEntry> skills = _store.LoadSkills();
            bool exists = skills.Any(e => e.Key == entry.Key && e.Value == entry.Value);
            if (!exists)
            {
                skills.Add(entry);
            }
            _store.SaveSkills(skills);
        }

        public List<MemoryEntry> LoadSkills()
        {
            return _store.LoadSkills();
        }

        // Memory Retrieval Operations (with scoring/compression)
        public List<MemoryEntry> LoadMemory(string key = "")
        {
            List<MemoryEntry> facts = _store.LoadFacts();
            List<MemoryEntry> profile = _store.LoadUserProfile();
            List<MemoryEntry> learnings = _store.LoadSystemLearnings();
            List<MemoryEntry> skills = _store.LoadSkills();

            List<MemoryEntry> all = facts.Concat(profile).Concat(learnings).Concat(skills).ToList();

            bool hasKeyFilter = !string.IsNullOrWhiteSpace(key);
            if (hasKeyFilter)
            {
                List<MemoryEntry> filtered = all.Where(e => e.Key == key).ToList();
                return filtered;
            }

            return all;
        }

        public List<MemoryEntry> GetRelevantMemory(int maxEntries)
        {
            List<MemoryEntry> all = LoadMemory();
            List<MemoryEntry> compressed = _compressor.EnforceCapacity(all);
            List<MemoryEntry> result = _scorer.ScoreAndRank(compressed, maxEntries);
            return result;
        }

        public List<MemoryEntry> LoadTopMemories(int count = DEFAULT_MAX_ENTRIES)
        {
            return GetRelevantMemory(count);
        }

        public List<MemoryEntry> LoadContextMemory(int maxEntries, bool includeCore = true)
        {
            List<MemoryEntry> allMemory = LoadMemory();
            List<MemoryEntry> result = _scorer.GetContextMemory(allMemory, maxEntries, includeCore);
            return result;
        }

        public void SummarizeMemory()
        {
            List<MemoryEntry> all = LoadMemory();
            _compressor.CompressMemory(all);

            List<MemoryEntry> facts = all.Where(e => e.Key != null).ToList();
            _store.SaveFacts(facts);
        }

        public void DeleteMemory(string key)
        {
            List<MemoryEntry> facts = _store.LoadFacts().Where(e => e.Key != key).ToList();
            List<MemoryEntry> profile = _store.LoadUserProfile().Where(e => e.Key != key).ToList();
            List<MemoryEntry> learnings = _store.LoadSystemLearnings().Where(e => e.Key != key).ToList();

            _store.SaveFacts(facts);
            _store.SaveUserProfile(profile);
            _store.SaveSystemLearnings(learnings);
        }
    }
}
