using Alissa.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Alissa.Core.Services
{
    public class MemoryStore
    {
        // Constants
        private const string MEMORY_DIR = "memory";
        private const string SHORT_TERM_DIR = "short_term";
        private const string LONG_TERM_DIR = "long_term";
        private const string SESSION_CACHE_FILE = "session_cache.json";
        private const string FACTS_FILE = "facts.json";
        private const string USER_PROFILE_FILE = "user_profile.json";
        private const string SYSTEM_LEARNINGS_FILE = "system_learnings.json";
        private const string SKILLS_FILE = "skills.json";
        private const string CONVERSATION_SUMMARIES_FILE = "conversation_summaries.json";

        private readonly string _shortTermDir;
        private readonly string _longTermDir;

        public MemoryStore(string basePath)
        {
            string memoryDir = Path.Combine(basePath, MEMORY_DIR);
            _shortTermDir = Path.Combine(memoryDir, SHORT_TERM_DIR);
            _longTermDir = Path.Combine(memoryDir, LONG_TERM_DIR);

            Directory.CreateDirectory(_shortTermDir);
            Directory.CreateDirectory(_longTermDir);
        }

        // Session Cache
        public void SaveSessionCache(List<Message> messages)
        {
            string filePath = Path.Combine(_shortTermDir, SESSION_CACHE_FILE);
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(messages, options);
            File.WriteAllText(filePath, json);
        }

        public List<Message> LoadSessionCache()
        {
            string filePath = Path.Combine(_shortTermDir, SESSION_CACHE_FILE);
            bool exists = File.Exists(filePath);
            if (!exists)
            {
                return new List<Message>();
            }

            try
            {
                string json = File.ReadAllText(filePath);
                List<Message>? result = JsonSerializer.Deserialize<List<Message>>(json);
                return result ?? new List<Message>();
            }
            catch
            {
                return new List<Message>();
            }
        }

        // Facts
        public void SaveFacts(List<MemoryEntry> facts)
        {
            string filePath = Path.Combine(_longTermDir, FACTS_FILE);
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(facts, options);
            File.WriteAllText(filePath, json);
        }

        public List<MemoryEntry> LoadFacts()
        {
            string filePath = Path.Combine(_longTermDir, FACTS_FILE);
            return LoadMemoryFile(filePath);
        }

        // User Profile
        public void SaveUserProfile(List<MemoryEntry> profile)
        {
            string filePath = Path.Combine(_longTermDir, USER_PROFILE_FILE);
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(profile, options);
            File.WriteAllText(filePath, json);
        }

        public List<MemoryEntry> LoadUserProfile()
        {
            string filePath = Path.Combine(_longTermDir, USER_PROFILE_FILE);
            return LoadMemoryFile(filePath);
        }

        // System Learnings
        public void SaveSystemLearnings(List<MemoryEntry> learnings)
        {
            string filePath = Path.Combine(_longTermDir, SYSTEM_LEARNINGS_FILE);
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(learnings, options);
            File.WriteAllText(filePath, json);
        }

        public List<MemoryEntry> LoadSystemLearnings()
        {
            string filePath = Path.Combine(_longTermDir, SYSTEM_LEARNINGS_FILE);
            return LoadMemoryFile(filePath);
        }

        // Skills
        public void SaveSkills(List<MemoryEntry> skills)
        {
            string filePath = Path.Combine(_longTermDir, SKILLS_FILE);
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(skills, options);
            File.WriteAllText(filePath, json);
        }

        public List<MemoryEntry> LoadSkills()
        {
            string filePath = Path.Combine(_longTermDir, SKILLS_FILE);
            return LoadMemoryFile(filePath);
        }

        // Conversation Summaries
        public void SaveConversationSummaries(List<ConversationSummary> summaries)
        {
            string filePath = Path.Combine(_longTermDir, CONVERSATION_SUMMARIES_FILE);
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(summaries, options);
            File.WriteAllText(filePath, json);
        }

        public List<ConversationSummary> LoadConversationSummaries()
        {
            string filePath = Path.Combine(_longTermDir, CONVERSATION_SUMMARIES_FILE);
            bool exists = File.Exists(filePath);
            if (!exists)
            {
                return new List<ConversationSummary>();
            }

            try
            {
                string json = File.ReadAllText(filePath);
                List<ConversationSummary>? result = JsonSerializer.Deserialize<List<ConversationSummary>>(json);
                return result ?? new List<ConversationSummary>();
            }
            catch
            {
                return new List<ConversationSummary>();
            }
        }

        private List<MemoryEntry> LoadMemoryFile(string filePath)
        {
            bool exists = File.Exists(filePath);
            if (!exists)
            {
                return new List<MemoryEntry>();
            }

            try
            {
                string json = File.ReadAllText(filePath);
                List<MemoryEntry>? result = JsonSerializer.Deserialize<List<MemoryEntry>>(json);
                return result ?? new List<MemoryEntry>();
            }
            catch
            {
                return new List<MemoryEntry>();
            }
        }
    }
}
