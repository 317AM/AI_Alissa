using Alissa.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Alissa.Core.Services
{
    public class MemoryStore
    {
        private readonly string _shortTermDir;
        private readonly string _longTermDir;

        public MemoryStore(string basePath)
        {
            string memoryDir = Path.Combine(basePath, "memory");
            _shortTermDir = Path.Combine(memoryDir, "short_term");
            _longTermDir = Path.Combine(memoryDir, "long_term");

            Directory.CreateDirectory(_shortTermDir);
            Directory.CreateDirectory(_longTermDir);
        }

        // Session Cache
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

        // Facts
        public void SaveFacts(List<MemoryEntry> facts)
        {
            string filePath = Path.Combine(_longTermDir, "facts.json");
            string json = JsonSerializer.Serialize(facts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public List<MemoryEntry> LoadFacts()
        {
            string filePath = Path.Combine(_longTermDir, "facts.json");
            return LoadMemoryFile(filePath);
        }

        // User Profile
        public void SaveUserProfile(List<MemoryEntry> profile)
        {
            string filePath = Path.Combine(_longTermDir, "user_profile.json");
            string json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public List<MemoryEntry> LoadUserProfile()
        {
            string filePath = Path.Combine(_longTermDir, "user_profile.json");
            return LoadMemoryFile(filePath);
        }

        // System Learnings
        public void SaveSystemLearnings(List<MemoryEntry> learnings)
        {
            string filePath = Path.Combine(_longTermDir, "system_learnings.json");
            string json = JsonSerializer.Serialize(learnings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public List<MemoryEntry> LoadSystemLearnings()
        {
            string filePath = Path.Combine(_longTermDir, "system_learnings.json");
            return LoadMemoryFile(filePath);
        }

        // Skills
        public void SaveSkills(List<MemoryEntry> skills)
        {
            string filePath = Path.Combine(_longTermDir, "skills.json");
            string json = JsonSerializer.Serialize(skills, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public List<MemoryEntry> LoadSkills()
        {
            string filePath = Path.Combine(_longTermDir, "skills.json");
            return LoadMemoryFile(filePath);
        }

        // Conversation Summaries
        public void SaveConversationSummaries(List<ConversationSummary> summaries)
        {
            string filePath = Path.Combine(_longTermDir, "conversation_summaries.json");
            string json = JsonSerializer.Serialize(summaries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public List<ConversationSummary> LoadConversationSummaries()
        {
            string filePath = Path.Combine(_longTermDir, "conversation_summaries.json");
            if (!File.Exists(filePath))
                return new List<ConversationSummary>();

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<ConversationSummary>>(json) ?? new List<ConversationSummary>();
            }
            catch
            {
                return new List<ConversationSummary>();
            }
        }

        private List<MemoryEntry> LoadMemoryFile(string filePath)
        {
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
    }
}
