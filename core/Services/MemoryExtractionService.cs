using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Extracts structured memory from conversation summaries.
    /// Uses flexible JSON extraction with graceful fallback.
    /// </summary>
    public class MemoryExtractionService
    {
        // Constants
        private const string USER_PROFILE_KEY = "user_profile";
        private const string FACTS_KEY = "facts";
        private const string SKILLS_KEY = "skills";
        private const string LEARNINGS_KEY = "system_learnings";
        private const string EXTRACTION_PROMPT = @"Based on the following conversation summary, extract structured memory in JSON format.
Return ONLY valid JSON, no other text.

Extract into these categories:
- user_profile: User information (name, preferences, goals, background)
- facts: General knowledge and facts discussed
- skills: Capabilities and techniques mentioned
- system_learnings: What you learned about how to help better, improvements needed

Example JSON structure:
{
  ""user_profile"": {
    ""name"": """",
    ""preference_style"": """",
    ""goal"": """"
  },
  ""facts"": {
    ""topic"": ""description""
  },
  ""skills"": {
    ""technique"": ""description""
  },
  ""system_learnings"": {
    ""improvement"": ""description""
  }
}

Conversation Summary:
";

        private readonly IChatClient _chatClient;
        private readonly IPromptBuilder _promptBuilder;

        public MemoryExtractionService(IChatClient chatClient, IPromptBuilder promptBuilder)
        {
            _chatClient = chatClient;
            _promptBuilder = promptBuilder;
        }

        /// <summary>
        /// Extracts memory from a conversation summary.
        /// Returns a result object even if extraction is incomplete.
        /// </summary>
        /// <param name="conversationSummary">Summary text to extract from</param>
        /// <returns>MemoryExtractionResult with whatever was successfully extracted</returns>
        public async Task<MemoryExtractionResult> ExtractMemoryAsync(string conversationSummary)
        {
            bool summaryIsValid = !string.IsNullOrWhiteSpace(conversationSummary);
            if (!summaryIsValid)
            {
                return new MemoryExtractionResult();
            }

            MemoryExtractionResult result = await AttemptExtraction(conversationSummary);
            return result;
        }

        private async Task<MemoryExtractionResult> AttemptExtraction(string conversationSummary)
        {
            string extractionPrompt = BuildExtractionPrompt(conversationSummary);
            string systemPrompt = _promptBuilder.BuildSystemPrompt();

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            await foreach (string token in _chatClient.StreamAsync(systemPrompt, extractionPrompt))
            {
                sb.Append(token);
            }

            string response = sb.ToString().Trim();
            MemoryExtractionResult extracted = ParseExtractionResponse(response);

            return extracted;
        }

        private static string BuildExtractionPrompt(string conversationSummary)
        {
            string fullPrompt = EXTRACTION_PROMPT + conversationSummary;
            return fullPrompt;
        }

        private static MemoryExtractionResult ParseExtractionResponse(string response)
        {
            MemoryExtractionResult result = new MemoryExtractionResult();

            bool responseIsValid = !string.IsNullOrWhiteSpace(response);
            if (!responseIsValid)
            {
                return result;
            }

            try
            {
                JsonElement json = JsonSerializer.Deserialize<JsonElement>(response);

                bool isObject = json.ValueKind == JsonValueKind.Object;
                if (!isObject)
                {
                    return result;
                }

                bool hasUserProfile = json.TryGetProperty(USER_PROFILE_KEY, out JsonElement userProfile) 
                    && userProfile.ValueKind == JsonValueKind.Object;
                if (hasUserProfile)
                {
                    result.UserProfile = ParseDictionary(userProfile);
                }

                bool hasFacts = json.TryGetProperty(FACTS_KEY, out JsonElement facts) 
                    && facts.ValueKind == JsonValueKind.Object;
                if (hasFacts)
                {
                    result.Facts = ParseDictionary(facts);
                }

                bool hasSkills = json.TryGetProperty(SKILLS_KEY, out JsonElement skills) 
                    && skills.ValueKind == JsonValueKind.Object;
                if (hasSkills)
                {
                    result.Skills = ParseDictionary(skills);
                }

                bool hasLearnings = json.TryGetProperty(LEARNINGS_KEY, out JsonElement learnings) 
                    && learnings.ValueKind == JsonValueKind.Object;
                if (hasLearnings)
                {
                    result.SystemLearnings = ParseDictionary(learnings);
                }
            }
            catch
            {
                // Result remains empty on parse failure
            }

            return result;
        }

        private static Dictionary<string, string> ParseDictionary(JsonElement element)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            bool isObject = element.ValueKind == JsonValueKind.Object;
            if (!isObject)
            {
                return dict;
            }

            foreach (JsonProperty property in element.EnumerateObject())
            {
                string key = property.Name;
                string value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    JsonValueKind.Object => JsonSerializer.Serialize(property.Value),
                    JsonValueKind.Array => JsonSerializer.Serialize(property.Value),
                    _ => property.Value.ToString()
                };

                bool valueIsNotEmpty = !string.IsNullOrWhiteSpace(value);
                if (valueIsNotEmpty)
                {
                    dict[key] = value;
                }
            }

            return dict;
        }
    }
}
