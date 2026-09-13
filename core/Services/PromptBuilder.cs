using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Builds system prompts with intelligent section injection and token budgeting.
    /// Follows strict priority order when trimming due to token limits.
    /// </summary>
    public class PromptBuilder : IPromptBuilder
    {
        // Constants
        private const string IDENTITY_FILE = "identity.txt";
        private const string BEHAVIOUR_FILE = "behaviour.txt";
        private const string BOUNDARIES_FILE = "boundaries.txt";
        private const string PERSONALITY_DIR = "personality";
        private const string CONFIG_DIR = "config";
        private const string PERSONA_JSON = "persona.json";
        private const string IDENTITY_SECTION = "Identity";
        private const string BEHAVIOUR_SECTION = "Behaviour";
        private const string BOUNDARIES_SECTION = "Boundaries";
        private const string USER_PROFILE_SECTION = "UserProfile";
        private const string FACTS_SECTION = "Facts";
        private const string RECENT_CONTEXT_SECTION = "RecentContext";
        private const string MEDIUM_TERM_SECTION = "MediumTermMemory";
        private const string SKILLS_SECTION = "Skills";
        private const string INTERNAL_NOTES_SECTION = "InternalNotes";
        private const string LEARNINGS_SECTION = "SystemLearnings";
        private const string PERSONA_SECTION = "PersonaFields";
        private const string RECENT_SESSIONS_HEADER = "\n## Recent Sessions";
        private const string USER_PROFILE_HEADER = "\n## User Profile";
        private const string CURRENT_USER_PREFIX = "Current user: ";
        private const string KNOWN_FACTS_HEADER = "\n## Known Facts";
        private const string RECENT_CONTEXT_HEADER = "\n## Recent Context";
        private const string SKILLS_HEADER = "\n## Skills & Knowledge";
        private const string LEARNINGS_HEADER = "\n## System Learnings";
        private const string INTERNAL_NOTES_HEADER = "\n## Internal Notes";
        private const string USER_CONTEXT_HEADER = "\n## User Context: ";
        private const string CURRENT_CODE_HEADER = "\n## Current Code: ";
        private const string APPEARANCE_HEADER = "\n## Appearance: ";
        private const string TOPICS_SUFFIX = " (Topics: ";
        private const string TASK_PREFIX = "Task: ";
        private const char CLOSE_PAREN = ')';
        private const char COMMA_SPACE = ',';
        private const string ALISSA_ROLE = "Alissa";
        private const string USER_ROLE = "User";
        private const string DATE_FORMAT = "MMM dd";
        private const string LINE_BREAK_ESCAPED = "\\n";
        private const string CARRIAGE_RETURN_ESCAPED = "\\r";

        private readonly string _basePath;
        private readonly IMemoryManager _memoryManager;
        private readonly MediumTermMemoryService? _mediumTermMemoryService;
        private readonly IThoughtService? _thoughtService;
        private readonly IUserContextService? _userContextService;
        private readonly PromptRulesModel _promptRules;
        private readonly PersonalityRulesModel _personalityRules;
        private string _currentUserCache = string.Empty;

    public PromptBuilder(
        string basePath,
        IMemoryManager memoryManager,
        PromptRulesModel? promptRules = null,
        PersonalityRulesModel? personalityRules = null,
        MediumTermMemoryService? mediumTermMemoryService = null,
        IThoughtService? thoughtService = null,
        IUserContextService? userContextService = null)
    {
        _basePath = basePath;
        _memoryManager = memoryManager;
        _mediumTermMemoryService = mediumTermMemoryService;
        _thoughtService = thoughtService;
        _userContextService = userContextService;
        _promptRules = promptRules ?? new PromptRulesModel();
        _personalityRules = personalityRules ?? new PersonalityRulesModel();
        _currentUserCache = Environment.UserName ?? "User";
    }

    /// <summary>
    /// Sets the current user context for prompt injection.
    /// Call this before building prompts for a session.
    /// </summary>
    public void SetCurrentUser(string userName)
    {
        if (!string.IsNullOrWhiteSpace(userName))
        {
            _currentUserCache = userName;
        }
    }

    /// <summary>
    /// Builds the base system prompt with personality and identity sections only.
    /// </summary>
    public string BuildSystemPrompt()
    {
        Dictionary<string, string> sections = new Dictionary<string, string>();

        sections[IDENTITY_SECTION] = LoadPersonalityFile(IDENTITY_FILE);
        sections[BEHAVIOUR_SECTION] = LoadPersonalityFile(BEHAVIOUR_FILE);
        sections[BOUNDARIES_SECTION] = LoadPersonalityFile(BOUNDARIES_FILE);

        string result = CombineSections(sections);
        return result;
    }

    /// <summary>
    /// Builds the complete system prompt with memory, context, and persona fields.
    /// Implements token budgeting and priority-based trimming.
    /// Performs query-aware memory retrieval to surface relevant memories based on user input.
    /// </summary>
    public async Task<string> BuildSystemPromptWithContextAsync(List<Message> sessionMessages, string currentUserInput = "")
    {
        Dictionary<string, string> sections = new Dictionary<string, string>();
        Dictionary<string, int> sectionTokens = new Dictionary<string, int>();

        sections[IDENTITY_SECTION] = LoadPersonalityFile(IDENTITY_FILE);
        sectionTokens[IDENTITY_SECTION] = EstimateTokens(sections[IDENTITY_SECTION]);

        sections[BEHAVIOUR_SECTION] = LoadPersonalityFile(BEHAVIOUR_FILE);
        sectionTokens[BEHAVIOUR_SECTION] = EstimateTokens(sections[BEHAVIOUR_SECTION]);

        sections[BOUNDARIES_SECTION] = LoadPersonalityFile(BOUNDARIES_FILE);
        sectionTokens[BOUNDARIES_SECTION] = EstimateTokens(sections[BOUNDARIES_SECTION]);

        List<MemoryEntry> userProfile = _memoryManager.LoadUserProfile();
        bool hasUserProfile = userProfile.Any();
        if (hasUserProfile)
        {
            sections[USER_PROFILE_SECTION] = BuildUserProfileSection(userProfile);
            sectionTokens[USER_PROFILE_SECTION] = EstimateTokens(sections[USER_PROFILE_SECTION]);
        }

        List<MemoryEntry> facts = LoadTopMemoriesWithQueryAwareness(currentUserInput);
        bool hasFacts = facts.Any();
        if (hasFacts)
        {
            sections[FACTS_SECTION] = BuildFactsSection(facts);
            sectionTokens[FACTS_SECTION] = EstimateTokens(sections[FACTS_SECTION]);
        }

        bool hasMessages = sessionMessages.Any();
        if (hasMessages)
        {
            sections[RECENT_CONTEXT_SECTION] = BuildConversationContext(sessionMessages);
            sectionTokens[RECENT_CONTEXT_SECTION] = EstimateTokens(sections[RECENT_CONTEXT_SECTION]);
        }

        bool hasMediumTermService = _mediumTermMemoryService != null;
        bool shouldIncludeMediumTerm = _promptRules.IncludeMediumTermMemory && hasMediumTermService;
        if (shouldIncludeMediumTerm)
        {
            string mediumTermSection = BuildMediumTermMemorySection(_mediumTermMemoryService!);
            bool hasMediumTermContent = !string.IsNullOrEmpty(mediumTermSection);
            if (hasMediumTermContent)
            {
                sections[MEDIUM_TERM_SECTION] = mediumTermSection;
                sectionTokens[MEDIUM_TERM_SECTION] = EstimateTokens(mediumTermSection);
            }
        }

        List<MemoryEntry> skills = _memoryManager.LoadSkills();
        bool hasSkills = skills.Any();
        if (hasSkills)
        {
            sections[SKILLS_SECTION] = BuildSkillsSection(skills);
            sectionTokens[SKILLS_SECTION] = EstimateTokens(sections[SKILLS_SECTION]);
        }

        bool hasThoughtService = _thoughtService != null;
        if (hasThoughtService)
        {
            string internalNotesSection = await BuildInternalNotesSectionAsync(_thoughtService!, sessionMessages);
            bool hasInternalNotes = !string.IsNullOrEmpty(internalNotesSection);
            if (hasInternalNotes)
            {
                sections[INTERNAL_NOTES_SECTION] = internalNotesSection;
                sectionTokens[INTERNAL_NOTES_SECTION] = EstimateTokens(internalNotesSection);
            }
        }

        List<MemoryEntry> learnings = _memoryManager.LoadSystemLearnings();
        bool hasLearnings = learnings.Any();
        if (hasLearnings)
        {
            sections[LEARNINGS_SECTION] = BuildLearningsSection(learnings);
            sectionTokens[LEARNINGS_SECTION] = EstimateTokens(sections[LEARNINGS_SECTION]);
        }

        bool shouldIncludePersona = _promptRules.IncludePersonaFields;
        if (shouldIncludePersona)
        {
            string personaSection = LoadPersonaFields();
            bool hasPersonaContent = !string.IsNullOrEmpty(personaSection);
            if (hasPersonaContent)
            {
                sections[PERSONA_SECTION] = personaSection;
                sectionTokens[PERSONA_SECTION] = EstimateTokens(sections[PERSONA_SECTION]);
            }
        }

        ApplyTokenBudget(sections, sectionTokens);

        string result = CombineSections(sections);
        return result;
    }

    private List<MemoryEntry> LoadTopMemoriesWithQueryAwareness(string currentUserInput)
    {
        List<MemoryEntry> allFacts = _memoryManager.LoadTopMemories(_promptRules.MaxMemoryEntries);

        bool hasQuery = !string.IsNullOrWhiteSpace(currentUserInput);
        if (!hasQuery)
        {
            return allFacts;
        }

        try
        {
            MemoryIndexBuilder indexBuilder = new MemoryIndexBuilder(
                _basePath,
                new IndexingRulesModel(),
                _memoryManager);

            List<MemoryIndexEntry> searchResults = indexBuilder.Search(currentUserInput, maxResults: 5);
            List<MemoryEntry> searchResultEntries = new List<MemoryEntry>();

            for (int i = 0; i < searchResults.Count; i++)
            {
                MemoryIndexEntry indexEntry = searchResults[i];
                MemoryEntry memoryEntry = new MemoryEntry
                {
                    Key = indexEntry.Key,
                    Value = string.Empty,
                    Relevance = indexEntry.Relevance,
                    IsCoreMemory = indexEntry.IsCoreMemory,
                    Timestamp = indexEntry.Timestamp
                };
                searchResultEntries.Add(memoryEntry);
            }

            List<MemoryEntry> result;
            try
            {
                result = searchResultEntries
                    .Union(allFacts, new MemoryEntryComparer())
                    .OrderByDescending(e => e.Relevance)
                    .Take(_promptRules.MaxMemoryEntries)
                    .ToList();
            }
            catch
            {
                result = allFacts;
            }

            return result;
        }
        catch
        {
            return allFacts;
        }
    }

    private class MemoryEntryComparer : IEqualityComparer<MemoryEntry>
    {
        public bool Equals(MemoryEntry? x, MemoryEntry? y)
        {
            var xIsNull = x == null;
            var yIsNull = y == null;

            if (xIsNull && yIsNull)
            {
                return true;
            }

            var anyNull = xIsNull || yIsNull;
            if (anyNull)
            {
                return false;
            }

            return x!.Key == y!.Key;
        }

        public int GetHashCode(MemoryEntry obj)
        {
            return obj.Key.GetHashCode();
        }
    }

    private void ApplyTokenBudget(Dictionary<string, string> sections, Dictionary<string, int> sectionTokens)
    {
        int totalTokens = sectionTokens.Values.Sum();
        bool needsTrimming = totalTokens > _promptRules.MaxPromptTokens;

        if (!needsTrimming)
        {
            return;
        }

        List<string> sectionsInTrimOrder = BuildTrimOrder(sections);

        for (int i = 0; i < sectionsInTrimOrder.Count; i++)
        {
            string section = sectionsInTrimOrder[i];
            bool sectionExists = sectionTokens.TryGetValue(section, out int tokens);

            if (sectionExists)
            {
                sections.Remove(section);
                totalTokens -= tokens;

                bool isBelowLimit = totalTokens <= _promptRules.MaxPromptTokens;

                if (isBelowLimit)
                {
                    return;
                }
            }
        }
    }

    private List<string> BuildTrimOrder(Dictionary<string, string> sections)
    {
        List<string> order = new List<string>();

        for (int i = 0; i < _promptRules.TrimPriority.Count; i++)
        {
            string priority = _promptRules.TrimPriority[i];
            bool sectionExists = sections.ContainsKey(priority);

            if (sectionExists)
            {
                order.Add(priority);
            }
        }

        for (int i = 0; i < sections.Keys.Count; i++)
        {
            string key = sections.Keys.ElementAt(i);
            bool keyNotInOrder = !order.Contains(key);
            if (keyNotInOrder)
            {
                order.Add(key);
            }
        }

        return order;
    }

    private string BuildMediumTermMemorySection(MediumTermMemoryService mediumTermService)
    {
        string result = string.Empty;

        List<MediumTermMemoryEntry> relevantEntries = mediumTermService.GetRelevantEntries(maxCount: 5);
        bool hasEntries = relevantEntries.Any();

        if (hasEntries)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(RECENT_SESSIONS_HEADER);

            for (int i = 0; i < relevantEntries.Count; i++)
            {
                MediumTermMemoryEntry entry = relevantEntries[i];
                string topics = string.Join(", ", entry.Topics);
                sb.AppendLine($"- [{entry.Timestamp:MMM dd}] {entry.Summary} ({TOPICS_SUFFIX}{topics}{CLOSE_PAREN})");
            }

            result = sb.ToString();
        }

        return result;
    }

    private async Task<string> BuildInternalNotesSectionAsync(IThoughtService thoughtService, List<Message> sessionMessages)
    {
        string result = string.Empty;

        bool hasMessages = sessionMessages.Any();
        if (hasMessages)
        {
            Message lastMessage = sessionMessages.Last();
            bool isUserMessage = lastMessage.Role == MessageRole.User;

            if (isUserMessage)
            {
                List<string> relevantThoughts = await thoughtService.GetRelevantThoughtsAsync(lastMessage.Content);
                bool hasThoughts = relevantThoughts.Any();

                if (hasThoughts)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(INTERNAL_NOTES_HEADER);

                    int maxThoughts = Math.Min(3, relevantThoughts.Count);
                    for (int i = 0; i < maxThoughts; i++)
                    {
                        sb.AppendLine($"- {relevantThoughts[i]}");
                    }

                    result = sb.ToString();
                }
            }
        }

        return result;
    }

    private string BuildUserProfileSection(List<MemoryEntry> profile)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine(USER_PROFILE_HEADER);
        sb.AppendLine(CURRENT_USER_PREFIX + _currentUserCache);

        bool hasProfile = profile.Any();
        if (hasProfile)
        {
            for (int i = 0; i < profile.Count; i++)
            {
                MemoryEntry entry = profile[i];
                string value = RestoreLineBreaks(entry.Value);
                sb.AppendLine($"- {entry.Key}: {value}");
            }
        }

        return sb.ToString();
    }

    private string BuildFactsSection(List<MemoryEntry> facts)
    {
        bool hasFacts = facts.Any();

        if (!hasFacts)
        {
            return string.Empty;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(KNOWN_FACTS_HEADER);

        int maxFacts = Math.Min(_promptRules.MaxMemoryEntries, facts.Count);
        for (int i = 0; i < maxFacts; i++)
        {
            MemoryEntry entry = facts[i];
            string value = RestoreLineBreaks(entry.Value);
            sb.AppendLine($"- {entry.Key}: {value}");
        }

        return sb.ToString();
    }

    private string BuildConversationContext(List<Message> sessionMessages)
    {
        bool hasMessages = sessionMessages.Any();

        if (!hasMessages)
        {
            return string.Empty;
        }

        List<Message> recentMessages = sessionMessages.TakeLast(_promptRules.MaxSessionMessages).ToList();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(RECENT_CONTEXT_HEADER);

        for (int i = 0; i < recentMessages.Count; i++)
        {
            Message msg = recentMessages[i];
            string role = msg.Role == MessageRole.User ? USER_ROLE : ALISSA_ROLE;
            string content = RestoreLineBreaks(msg.Content);
            sb.AppendLine($"{role}: {content}");
        }

        return sb.ToString();
    }

    private string BuildSkillsSection(List<MemoryEntry> skills)
    {
        bool hasSkills = skills.Any();

        if (!hasSkills)
        {
            return string.Empty;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(SKILLS_HEADER);

        for (int i = 0; i < skills.Count; i++)
        {
            MemoryEntry skill = skills[i];
            string value = RestoreLineBreaks(skill.Value);
            sb.AppendLine($"- {skill.Key}: {value}");
        }

        return sb.ToString();
    }

    private string BuildLearningsSection(List<MemoryEntry> learnings)
    {
        bool hasLearnings = learnings.Any();

        if (!hasLearnings)
        {
            return string.Empty;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(LEARNINGS_HEADER);

        for (int i = 0; i < learnings.Count; i++)
        {
            MemoryEntry learning = learnings[i];
            string value = RestoreLineBreaks(learning.Value);
            sb.AppendLine($"- {learning.Key}: {value}");
        }

        return sb.ToString();
    }

    private string LoadPersonaFields()
    {
        bool shouldLoadPersona = _promptRules.IncludePersonaFields;

        if (!shouldLoadPersona)
        {
            return string.Empty;
        }

        try
        {
            string personaPath = Path.Combine(_basePath, CONFIG_DIR, PERSONA_JSON);
            bool personaFileExists = File.Exists(personaPath);

            if (!personaFileExists)
            {
                return string.Empty;
            }

            string json = File.ReadAllText(personaPath);
            PersonaModel? persona = System.Text.Json.JsonSerializer.Deserialize<PersonaModel>(json);

            bool personaIsValid = persona != null;

            if (!personaIsValid)
            {
                return string.Empty;
            }

            string result = BuildPersonaFieldsContent(persona);
            return result;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string BuildPersonaFieldsContent(PersonaModel persona)
    {
        StringBuilder sb = new StringBuilder();

        bool hasUserName = !string.IsNullOrEmpty(persona.CurrentUser?.Name);

        if (hasUserName)
        {
            sb.AppendLine(USER_CONTEXT_HEADER + persona.CurrentUser!.Name);
        }

        bool hasCodeName = !string.IsNullOrEmpty(persona.CurrentCode?.Name);

        if (hasCodeName)
        {
            sb.AppendLine($"{CURRENT_CODE_HEADER}{persona.CurrentCode!.Name} ({persona.CurrentCode.Language})");

            bool hasCodeTask = !string.IsNullOrEmpty(persona.CurrentCode.Task);

            if (hasCodeTask)
            {
                sb.AppendLine(TASK_PREFIX + persona.CurrentCode.Task);
            }
        }

        bool hasAppearance = !string.IsNullOrEmpty(persona.Appearance?.Description);

        if (hasAppearance)
        {
            sb.AppendLine(APPEARANCE_HEADER + persona.Appearance!.Description);
        }

        string result = sb.ToString();
        return result;
    }

    private string LoadPersonalityFile(string fileName)
    {
        try
        {
            string filePath = Path.Combine(_basePath, PERSONALITY_DIR, fileName);
            bool fileExists = File.Exists(filePath);

            if (!fileExists)
            {
                return string.Empty;
            }

            string content = File.ReadAllText(filePath).Trim();
            return content;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string CombineSections(Dictionary<string, string> sections)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < sections.Values.Count; i++)
        {
            string sectionValue = sections.Values.ElementAt(i);
            bool isNotEmpty = !string.IsNullOrEmpty(sectionValue);
            if (isNotEmpty)
            {
                sb.AppendLine(sectionValue);
                sb.AppendLine();
            }
        }

        string result = sb.ToString().Trim();
        return result;
    }

    private int EstimateTokens(string text)
    {
        bool textIsValid = !string.IsNullOrEmpty(text);

        if (!textIsValid)
        {
            return 0;
        }

        string[] lines = text.Split('\n');
        int lineCount = lines.Length;
        int tokenCount = lineCount * _promptRules.TokensPerLine;

        return tokenCount;
    }

    private static string RestoreLineBreaks(string text)
    {
        bool textIsValid = !string.IsNullOrEmpty(text);

        if (!textIsValid)
        {
            return string.Empty;
        }

        string result = text.Replace(LINE_BREAK_ESCAPED, "\n").Replace(CARRIAGE_RETURN_ESCAPED, "\r");
        return result;
    }
}}