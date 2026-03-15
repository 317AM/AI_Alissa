using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Alissa.Core.Services
{
    public class MemoryManager : IMemoryManager
    {
        private readonly string _memoryDir;
        private readonly string _shortTermDir;
        private readonly string _longTermDir;
        private readonly string _embeddingsDir;

        private readonly MemoryScorer _scorer;
        private readonly MemoryCompressor _compressor;
        private readonly MemoryIndexer _indexer;

        public MemoryManager(string basePath, MemoryModel memoryConfig)
        {
            _memoryDir = Path.Combine(basePath, "memory");
            _shortTermDir = Path.Combine(_memoryDir, "short_term");
            _longTermDir = Path.Combine(_memoryDir, "long_term");
            _embeddingsDir = Path.Combine(_memoryDir, "embeddings");

            Directory.CreateDirectory(_shortTermDir);
            Directory.CreateDirectory(_longTermDir);
            Directory.CreateDirectory(_embeddingsDir);

            _scorer = new MemoryScorer(memoryConfig);
            _compressor = new MemoryCompressor(memoryConfig);
            _indexer = new MemoryIndexer(memoryConfig);
        }

        public void SaveSessionCache(List<Message> messages)
        {
            string filePath = Path.Combine(_shortTermDir, "session_cache.json");
            string json = JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public List<Message> LoadSessionCache()
        {
            string filePath = Path.Combine(_shortTermDir, "session_cache.json");
            if (!File.Exists(filePath))
                return new List<Message>();

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<Message>>(json) ?? new List<Message>();
            }
            catch
            {
                return new List<Message>();
            }
        }

        public void SaveFact(MemoryEntry entry)
        {
            string filePath = Path.Combine(_longTermDir, "facts.json");
            List<MemoryEntry> facts = LoadFacts();

            bool exists = facts.Any(e => e.Key == entry.Key && e.Value == entry.Value);
            if (!exists)
                facts.Add(entry);
            else
            {
                int idx = facts.FindIndex(e => e.Key == entry.Key && e.Value == entry.Value);
                if (idx >= 0)
                    facts[idx] = entry;
            }

            string json = JsonSerializer.Serialize(facts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public List<MemoryEntry> LoadFacts()
        {
            string filePath = Path.Combine(_longTermDir, "facts.json");
            if (!File.Exists(filePath))
                return new List<MemoryEntry>();

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<MemoryEntry>>(json) ?? new List<MemoryEntry>();
            }
            catch
            {
                return new List<MemoryEntry>();
            }
        }

        public void SaveUserProfile(MemoryEntry entry)
        {
            string filePath = Path.Combine(_longTermDir, "user_profile.json");
            List<MemoryEntry> profile = LoadUserProfile();

            var existing = profile.FirstOrDefault(e => e.Key == entry.Key);
            if (existing != null)
            {
                profile.Remove(existing);
            }
            profile.Add(entry);

            string json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public List<MemoryEntry> LoadUserProfile()
        {
            string filePath = Path.Combine(_longTermDir, "user_profile.json");
            if (!File.Exists(filePath))
                return new List<MemoryEntry>();

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<MemoryEntry>>(json) ?? new List<MemoryEntry>();
            }
            catch
            {
                return new List<MemoryEntry>();
            }
        }

        public void SaveSystemLearning(MemoryEntry entry)
        {
            string filePath = Path.Combine(_longTermDir, "system_learnings.json");
            List<MemoryEntry> learnings = LoadSystemLearnings();

            bool exists = learnings.Any(e => e.Key == entry.Key && e.Value == entry.Value);
            if (!exists)
                learnings.Add(entry);

            string json = JsonSerializer.Serialize(learnings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public List<MemoryEntry> LoadSystemLearnings()
        {
            string filePath = Path.Combine(_longTermDir, "system_learnings.json");
            if (!File.Exists(filePath))
                return new List<MemoryEntry>();

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<MemoryEntry>>(json) ?? new List<MemoryEntry>();
            }
            catch
            {
                return new List<MemoryEntry>();
            }
        }

        public void SaveConversationSummary(string summary)
        {
            string filePath = Path.Combine(_memoryDir, "conversation_summary.json");
            var summaryEntry = new
            {
                summary = summary,
                timestamp = DateTime.Now
            };
            string json = JsonSerializer.Serialize(summaryEntry, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public void SaveMemory(MemoryEntry entry)
        {
            SaveFact(entry);
        }

        public List<MemoryEntry> LoadMemory(string key = "")
        {
            var facts = LoadFacts();
            var profile = LoadUserProfile();
            var learnings = LoadSystemLearnings();

            var all = facts.Concat(profile).Concat(learnings).ToList();

            if (!string.IsNullOrWhiteSpace(key))
                return all.Where(e => e.Key == key).ToList();

            return all;
        }

        public List<MemoryEntry> LoadTopMemories(int count = 10)
        {
            var allMemory = LoadMemory();
            return _scorer.ScoreAndRank(allMemory, count);
        }

        public List<MemoryEntry> LoadContextMemory(int maxEntries, bool includeCore = true)
        {
            var allMemory = LoadMemory();
            return _scorer.GetContextMemory(allMemory, maxEntries, includeCore);
        }

        public void SummarizeMemory()
        {
            var allMemory = LoadMemory();
            _compressor.CompressMemory(allMemory);

            var grouped = allMemory.GroupBy(e => e.Key);
            foreach (var g in grouped)
            {
                foreach (var entry in g)
                {
                    SaveFact(entry);
                }
            }
        }

        public void DeleteMemory(string key)
        {
            var facts = LoadFacts().Where(e => e.Key != key).ToList();
            var profile = LoadUserProfile().Where(e => e.Key != key).ToList();
            var learnings = LoadSystemLearnings().Where(e => e.Key != key).ToList();

            string factsPath = Path.Combine(_longTermDir, "facts.json");
            string profilePath = Path.Combine(_longTermDir, "user_profile.json");
            string learningsPath = Path.Combine(_longTermDir, "system_learnings.json");

            File.WriteAllText(factsPath, JsonSerializer.Serialize(facts, new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(profilePath, JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(learningsPath, JsonSerializer.Serialize(learnings, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
