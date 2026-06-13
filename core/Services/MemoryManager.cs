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
            var facts = _store.LoadFacts();
            if (!facts.Any(f => f.Key == entry.Key && f.Value == entry.Value))
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
            var profile = _store.LoadUserProfile();
            var existing = profile.FirstOrDefault(p => p.Key == entry.Key);
            if (existing != null)
                profile.Remove(existing);
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
            var learnings = _store.LoadSystemLearnings();
            if (!learnings.Any(l => l.Key == entry.Key && l.Value == entry.Value))
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
            var summaries = _store.LoadConversationSummaries();
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
            var skills = _store.LoadSkills();
            bool exists = skills.Any(e => e.Key == entry.Key && e.Value == entry.Value);
            if (!exists)
                skills.Add(entry);
            _store.SaveSkills(skills);
        }

        public List<MemoryEntry> LoadSkills()
        {
            return _store.LoadSkills();
        }

        // Memory Retrieval Operations (with scoring/compression)
        public List<MemoryEntry> LoadMemory(string key = "")
        {
            var facts = _store.LoadFacts();
            var profile = _store.LoadUserProfile();
            var learnings = _store.LoadSystemLearnings();
            var skills = _store.LoadSkills();

            var all = facts.Concat(profile).Concat(learnings).Concat(skills).ToList();

            if (!string.IsNullOrWhiteSpace(key))
                return all.Where(e => e.Key == key).ToList();

            return all;
        }

        public List<MemoryEntry> GetRelevantMemory(int maxEntries)
        {
            var all = LoadMemory();
            var compressed = _compressor.EnforceCapacity(all);
            return _scorer.ScoreAndRank(compressed, maxEntries);
        }

        public List<MemoryEntry> LoadTopMemories(int count = 10)
        {
            return GetRelevantMemory(count);
        }

        public List<MemoryEntry> LoadContextMemory(int maxEntries, bool includeCore = true)
        {
            var allMemory = LoadMemory();
            return _scorer.GetContextMemory(allMemory, maxEntries, includeCore);
        }

        public void SummarizeMemory()
        {
            var all = LoadMemory();
            _compressor.CompressMemory(all);

            var facts = all.Where(e => e.Key != null).ToList();
            _store.SaveFacts(facts);
        }

        public void DeleteMemory(string key)
        {
            var facts = _store.LoadFacts().Where(e => e.Key != key).ToList();
            var profile = _store.LoadUserProfile().Where(e => e.Key != key).ToList();
            var learnings = _store.LoadSystemLearnings().Where(e => e.Key != key).ToList();

            _store.SaveFacts(facts);
            _store.SaveUserProfile(profile);
            _store.SaveSystemLearnings(learnings);
        }
    }
}
