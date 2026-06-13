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
            if (string.IsNullOrWhiteSpace(conversationText))
            {
                return new ConversationSummary();
            }

            var summary = new ConversationSummary
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
                var summaryTask = _summaryService.GenerateSummaryAsync(conversationText, 5);
                summary.Summary = summaryTask.Result;

                var highlightsTask = _summaryService.GenerateHighlightsAsync(conversationText, 5);
                summary.Highlights = highlightsTask.Result;

                var topicsTask = _summaryService.GenerateTopicsAsync(conversationText);
                summary.Topics = topicsTask.Result;
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, ".", true);
            }
        }

        private async Task ExtractMemory(ConversationSummary summary)
        {
            try
            {
                var extraction = await _extractionService.ExtractMemoryAsync(summary.Summary);

                summary.Extraction = extraction;
                summary.ExtractedUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, ".", true);
            }
        }

        private void StoreMemory(ConversationSummary summary)
        {
            if (summary.Extraction == null || !summary.Extraction.HasData)
            {
                return;
            }

            try
            {
                foreach (var kvp in summary.Extraction.UserProfile)
                {
                    _memoryManager.SaveUserProfile(new MemoryEntry(kvp.Key, kvp.Value, 0.9, false));
                }

                foreach (var kvp in summary.Extraction.Facts)
                {
                    _memoryManager.SaveFact(new MemoryEntry(kvp.Key, kvp.Value, 0.8, false));
                }

                foreach (var kvp in summary.Extraction.Skills)
                {
                    _memoryManager.SaveSkill(new MemoryEntry(kvp.Key, kvp.Value, 0.85, false));
                }

                foreach (var kvp in summary.Extraction.SystemLearnings)
                {
                    _memoryManager.SaveSystemLearning(new MemoryEntry(kvp.Key, kvp.Value, 0.95, false));
                }

                summary.IsProcessed = true;
                _memoryManager.SaveConversationSummary(summary);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, ".", true);
            }
        }

        private void StoreMediumTermMemory(ConversationSummary summary, string sessionId)
        {
            try
            {
                var mediumEntry = new MediumTermMemoryEntry
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
                ErrorHandler.Handle(ex, ".", false);
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
                ErrorHandler.Handle(ex, ".", false);
            }
        }

        private static double CalculateRelevance(ConversationSummary summary)
        {
            double relevance = 0.5;

            if (summary.Highlights.Count > 0)
            {
                relevance += 0.2;
            }

            if (summary.Topics.Count > 0)
            {
                relevance += 0.15;
            }

            if (summary.MessageCount > 10)
            {
                relevance += 0.15;
            }

            return Math.Min(1.0, relevance);
        }

        private static List<string> ExtractTags(ConversationSummary summary)
        {
            var tags = new List<string>();

            if (summary.Summary.Contains("code", StringComparison.OrdinalIgnoreCase))
            {
                tags.Add("coding");
            }

            if (summary.Summary.Contains("debug", StringComparison.OrdinalIgnoreCase))
            {
                tags.Add("debugging");
            }

            if (summary.Summary.Contains("design", StringComparison.OrdinalIgnoreCase))
            {
                tags.Add("design");
            }

            if (summary.Summary.Contains("pattern", StringComparison.OrdinalIgnoreCase))
            {
                tags.Add("patterns");
            }

            tags.AddRange(summary.Topics);

            return tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
