using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Builds system prompts with intelligent section injection and token budgeting.
/// Follows strict priority order when trimming due to token limits.
/// </summary>
public class PromptBuilder : IPromptBuilder
{
    private readonly string _basePath;
    private readonly IMemoryManager _memoryManager;
    private readonly PromptRulesModel _promptRules;
    private readonly PersonalityRulesModel _personalityRules;

    public PromptBuilder(string basePath, IMemoryManager memoryManager, PromptRulesModel? promptRules = null, PersonalityRulesModel? personalityRules = null)
    {
        _basePath = basePath;
        _memoryManager = memoryManager;
        _promptRules = promptRules ?? new PromptRulesModel();
        _personalityRules = personalityRules ?? new PersonalityRulesModel();
    }

    /// <summary>
    /// Builds the base system prompt with personality and identity sections only.
    /// </summary>
    public string BuildSystemPrompt()
    {
        var sections = new Dictionary<string, string>();

        sections["Identity"] = LoadPersonalityFile("identity.txt");
        sections["Behaviour"] = LoadPersonalityFile("behaviour.txt");
        sections["Boundaries"] = LoadPersonalityFile("boundaries.txt");

        return CombineSections(sections);
    }

    /// <summary>
    /// Builds the complete system prompt with memory, context, and persona fields.
    /// Implements token budgeting and priority-based trimming.
    /// </summary>
    public string BuildSystemPromptWithContext(List<Message> sessionMessages)
    {
        var sections = new Dictionary<string, string>();
        var sectionTokens = new Dictionary<string, int>();

        sections["Identity"] = LoadPersonalityFile("identity.txt");
        sectionTokens["Identity"] = EstimateTokens(sections["Identity"]);

        sections["Behaviour"] = LoadPersonalityFile("behaviour.txt");
        sectionTokens["Behaviour"] = EstimateTokens(sections["Behaviour"]);

        sections["Boundaries"] = LoadPersonalityFile("boundaries.txt");
        sectionTokens["Boundaries"] = EstimateTokens(sections["Boundaries"]);

        var userProfile = _memoryManager.LoadUserProfile();
        if (userProfile.Any())
        {
            sections["UserProfile"] = BuildUserProfileSection(userProfile);
            sectionTokens["UserProfile"] = EstimateTokens(sections["UserProfile"]);
        }

        var facts = _memoryManager.LoadTopMemories(_promptRules.MaxMemoryEntries);
        if (facts.Any())
        {
            sections["Facts"] = BuildFactsSection(facts);
            sectionTokens["Facts"] = EstimateTokens(sections["Facts"]);
        }

        if (sessionMessages.Any())
        {
            sections["RecentContext"] = BuildConversationContext(sessionMessages);
            sectionTokens["RecentContext"] = EstimateTokens(sections["RecentContext"]);
        }

        var skills = _memoryManager.LoadSkills();
        if (skills.Any())
        {
            sections["Skills"] = BuildSkillsSection(skills);
            sectionTokens["Skills"] = EstimateTokens(sections["Skills"]);
        }

        var learnings = _memoryManager.LoadSystemLearnings();
        if (learnings.Any())
        {
            sections["SystemLearnings"] = BuildLearningsSection(learnings);
            sectionTokens["SystemLearnings"] = EstimateTokens(sections["SystemLearnings"]);
        }

        if (_promptRules.IncludePersonaFields)
        {
            var personaSection = LoadPersonaFields();
            if (!string.IsNullOrEmpty(personaSection))
            {
                sections["PersonaFields"] = personaSection;
                sectionTokens["PersonaFields"] = EstimateTokens(sections["PersonaFields"]);
            }
        }

        ApplyTokenBudget(sections, sectionTokens);

        return CombineSections(sections);
    }

    private void ApplyTokenBudget(Dictionary<string, string> sections, Dictionary<string, int> sectionTokens)
    {
        int totalTokens = sectionTokens.Values.Sum();
        bool needsTrimming = totalTokens > _promptRules.MaxPromptTokens;

        if (!needsTrimming)
        {
            return;
        }

        var sectionsInTrimOrder = BuildTrimOrder(sections);

        foreach (var section in sectionsInTrimOrder)
        {
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
        var order = new List<string>();

        foreach (var priority in _promptRules.TrimPriority)
        {
            bool sectionExists = sections.ContainsKey(priority);

            if (sectionExists)
            {
                order.Add(priority);
            }
        }

        foreach (var key in sections.Keys.Where(k => !order.Contains(k)))
        {
            order.Add(key);
        }

        return order;
    }

    private string BuildUserProfileSection(List<MemoryEntry> profile)
    {
        bool hasProfile = profile.Any();

        if (!hasProfile)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("\n## User Profile");

        foreach (var entry in profile)
        {
            string value = RestoreLineBreaks(entry.Value);
            sb.AppendLine($"- {entry.Key}: {value}");
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

        var sb = new StringBuilder();
        sb.AppendLine("\n## Known Facts");

        foreach (var entry in facts.Take(_promptRules.MaxMemoryEntries))
        {
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

        var recentMessages = sessionMessages.TakeLast(_promptRules.MaxSessionMessages).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("\n## Recent Context");

        foreach (var msg in recentMessages)
        {
            string role = msg.Role == MessageRole.User ? "User" : "Alissa";
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

        var sb = new StringBuilder();
        sb.AppendLine("\n## Skills & Knowledge");

        foreach (var skill in skills)
        {
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

        var sb = new StringBuilder();
        sb.AppendLine("\n## System Learnings");

        foreach (var learning in learnings)
        {
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
            string personaPath = Path.Combine(_basePath, "config", "persona.json");
            bool personaFileExists = File.Exists(personaPath);

            if (!personaFileExists)
            {
                return string.Empty;
            }

            string json = File.ReadAllText(personaPath);
            var persona = System.Text.Json.JsonSerializer.Deserialize<PersonaModel>(json);

            bool personaIsValid = persona != null;

            if (!personaIsValid)
            {
                return string.Empty;
            }

            return BuildPersonaFieldsContent(persona);
        }
        catch
        {
            return string.Empty;
        }
    }

    private string BuildPersonaFieldsContent(PersonaModel persona)
    {
        var sb = new StringBuilder();

        bool hasUserName = !string.IsNullOrEmpty(persona.CurrentUser?.Name);

        if (hasUserName)
        {
            sb.AppendLine($"\n## User Context: {persona.CurrentUser!.Name}");
        }

        bool hasCodeName = !string.IsNullOrEmpty(persona.CurrentCode?.Name);

        if (hasCodeName)
        {
            sb.AppendLine($"## Current Code: {persona.CurrentCode!.Name} ({persona.CurrentCode.Language})");

            bool hasCodeTask = !string.IsNullOrEmpty(persona.CurrentCode.Task);

            if (hasCodeTask)
            {
                sb.AppendLine($"Task: {persona.CurrentCode.Task}");
            }
        }

        bool hasAppearance = !string.IsNullOrEmpty(persona.Appearance?.Description);

        if (hasAppearance)
        {
            sb.AppendLine($"## Appearance: {persona.Appearance!.Description}");
        }

        string result = sb.ToString();
        return result;
    }

    private string LoadPersonalityFile(string fileName)
    {
        try
        {
            string filePath = Path.Combine(_basePath, "personality", fileName);
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
        var sb = new StringBuilder();

        foreach (var section in sections.Values.Where(s => !string.IsNullOrEmpty(s)))
        {
            sb.AppendLine(section);
            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }

    private int EstimateTokens(string text)
    {
        bool textIsValid = !string.IsNullOrEmpty(text);

        if (!textIsValid)
        {
            return 0;
        }

        int lineCount = text.Split('\n').Length;
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

        string result = text.Replace("\\n", "\n").Replace("\\r", "\r");
        return result;
    }
}