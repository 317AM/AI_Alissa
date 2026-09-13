using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Orchestrates the complete memory pipeline:
    /// Conversation → Summary → Extraction → Storage → Indexing
    /// 
    /// This service coordinates between summary generation, extraction, and storage
    /// to create a clean separation of concerns.
    /// </summary>
    public class MemoryPipeline
    {
        // Constants
        private const double USER_PROFILE_WEIGHT = 0.9;
        private const double FACTS_WEIGHT = 0.8;
        private const double SKILLS_WEIGHT = 0.85;
        private const double LEARNINGS_WEIGHT = 0.95;
        private const double BASE_RELEVANCE = 0.5;
        private const double HIGHLIGHTS_BOOST = 0.2;
        private const double TOPICS_BOOST = 0.15;
        private const double MESSAGE_BOOST = 0.15;
        private const int MESSAGE_COUNT_THRESHOLD = 10;
        private const double MAX_RELEVANCE = 1.0;
        private const string CODE_KEYWORD = "code";
        private const string DEBUG_KEYWORD = "debug";
        private const string DESIGN_KEYWORD = "design";
        private const string PATTERN_KEYWORD = "pattern";
        private const string CODE_TAG = "coding";
        private const string DEBUG_TAG = "debugging";
        private const string DESIGN_TAG = "design";
        private const string PATTERNS_TAG = "patterns";
        private const int SUMMARY_LINE_COUNT = 5;
        private const string DOT_PATH = ".";

        private readonly SummaryGenerationService _summaryService;
        private readonly MemoryExtractionService _extractionService;
        private readonly IMemoryManager _memoryManager;
        private readonly MediumTermMemoryService _mediumTermService;
        private readonly MemoryIndexBuilder _indexBuilder;

        public MemoryPipeline(
            SummaryGenerationService summaryService,
            MemoryExtractionService extractionService,
            IMemoryManager memoryManager,
            MediumTermMemoryService mediumTermService,
            MemoryIndexBuilder indexBuilder)
        {
            _summaryService = summaryService;
            _extractionService = extractionService;
            _memoryManager = memoryManager;
            _mediumTermService = mediumTermService;
            _indexBuilder = indexBuilder;
        }

        /// <summary>
        /// Processes a complete conversation through the memory pipeline.
        /// </summary>
        /// <param name="conversationText">Full conversation text</param>
        /// <param name="sessionId">Identifier for this session</param>
        /// <returns>Summary result with extraction data</returns>
        public async Task<ConversationSummary> ProcessConversationAsync(string conversationText, string sessionId)
        {
            bool isEmpty = string.IsNullOrWhiteSpace(conversationText);
            if (isEmpty)
            {
                return new ConversationSummary();
            }

            ConversationSummary summary = new ConversationSummary
            {
                Id = sessionId
            };

            GenerateSummary(conversationText, summary);

            await ExtractMemory(summary);

            StoreMemory(summary);

            StoreMediumTermMemory(summary, sessionId);

            RebuildIndex();

            return summary;
        }

        private void GenerateSummary(string conversationText, ConversationSummary summary)
        {
            try
            {
                Task<string> summaryTask = _summaryService.GenerateSummaryAsync(conversationText, SUMMARY_LINE_COUNT);
                summary.Summary = summaryTask.Result;

                Task<List<string>> highlightsTask = _summaryService.GenerateHighlightsAsync(conversationText, SUMMARY_LINE_COUNT);
                summary.Highlights = highlightsTask.Result;

                Task<List<string>> topicsTask = _summaryService.GenerateTopicsAsync(conversationText);
                summary.Topics = topicsTask.Result;
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, DOT_PATH, true);
            }
        }

        private async Task ExtractMemory(ConversationSummary summary)
        {
            try
            {
                MemoryExtractionResult extraction = await _extractionService.ExtractMemoryAsync(summary.Summary);

                summary.Extraction = extraction;
                summary.ExtractedUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, DOT_PATH, true);
            }
        }

        private void StoreMemory(ConversationSummary summary)
        {
            bool hasNoExtraction = summary.Extraction == null || !summary.Extraction.HasData;
            if (hasNoExtraction)
            {
                return;
            }

            try
            {
                for (int i = 0; i < summary.Extraction.UserProfile.Count; i++)
                {
                    KeyValuePair<string, string> kvp = summary.Extraction.UserProfile.ElementAt(i);
                    MemoryEntry userEntry = new MemoryEntry(kvp.Key, kvp.Value, USER_PROFILE_WEIGHT, false);
                    _memoryManager.SaveUserProfile(userEntry);
                }

                for (int i = 0; i < summary.Extraction.Facts.Count; i++)
                {
                    KeyValuePair<string, string> kvp = summary.Extraction.Facts.ElementAt(i);
                    MemoryEntry factEntry = new MemoryEntry(kvp.Key, kvp.Value, FACTS_WEIGHT, false);
                    _memoryManager.SaveFact(factEntry);
                }

                for (int i = 0; i < summary.Extraction.Skills.Count; i++)
                {
                    KeyValuePair<string, string> kvp = summary.Extraction.Skills.ElementAt(i);
                    MemoryEntry skillEntry = new MemoryEntry(kvp.Key, kvp.Value, SKILLS_WEIGHT, false);
                    _memoryManager.SaveSkill(skillEntry);
                }

                for (int i = 0; i < summary.Extraction.SystemLearnings.Count; i++)
                {
                    KeyValuePair<string, string> kvp = summary.Extraction.SystemLearnings.ElementAt(i);
                    MemoryEntry learningEntry = new MemoryEntry(kvp.Key, kvp.Value, LEARNINGS_WEIGHT, false);
                    _memoryManager.SaveSystemLearning(learningEntry);
                }

                summary.IsProcessed = true;
                _memoryManager.SaveConversationSummary(summary);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, DOT_PATH, true);
            }
        }

        private void StoreMediumTermMemory(ConversationSummary summary, string sessionId)
        {
            try
            {
                MediumTermMemoryEntry mediumEntry = new MediumTermMemoryEntry
                {
                    SessionId = sessionId,
                    Summary = summary.Summary,
                    MessageCount = summary.MessageCount,
                    Topics = summary.Topics,
                    Highlights = summary.Highlights,
                    RelevanceScore = CalculateRelevance(summary),
                    Tags = ExtractTags(summary)
                };

                _mediumTermService.SaveEntry(mediumEntry);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, DOT_PATH, false);
            }
        }

        private void RebuildIndex()
        {
            try
            {
                _indexBuilder.BuildIndex();
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, DOT_PATH, false);
            }
        }

        private static double CalculateRelevance(ConversationSummary summary)
        {
            double relevance = BASE_RELEVANCE;

            bool hasHighlights = summary.Highlights.Count > 0;
            if (hasHighlights)
            {
                relevance += HIGHLIGHTS_BOOST;
            }

            bool hasTopics = summary.Topics.Count > 0;
            if (hasTopics)
            {
                relevance += TOPICS_BOOST;
            }

            bool hasEnoughMessages = summary.MessageCount > MESSAGE_COUNT_THRESHOLD;
            if (hasEnoughMessages)
            {
                relevance += MESSAGE_BOOST;
            }

            double cappedRelevance = Math.Min(MAX_RELEVANCE, relevance);
            return cappedRelevance;
        }

        private static List<string> ExtractTags(ConversationSummary summary)
        {
            List<string> tags = new List<string>();

            bool hasCodeContent = summary.Summary.Contains(CODE_KEYWORD, StringComparison.OrdinalIgnoreCase);
            if (hasCodeContent)
            {
                tags.Add(CODE_TAG);
            }

            bool hasDebugContent = summary.Summary.Contains(DEBUG_KEYWORD, StringComparison.OrdinalIgnoreCase);
            if (hasDebugContent)
            {
                tags.Add(DEBUG_TAG);
            }

            bool hasDesignContent = summary.Summary.Contains(DESIGN_KEYWORD, StringComparison.OrdinalIgnoreCase);
            if (hasDesignContent)
            {
                tags.Add(DESIGN_TAG);
            }

            bool hasPatternContent = summary.Summary.Contains(PATTERN_KEYWORD, StringComparison.OrdinalIgnoreCase);
            if (hasPatternContent)
            {
                tags.Add(PATTERNS_TAG);
            }

            tags.AddRange(summary.Topics);

            List<string> result = tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return result;
        }
    }
}
