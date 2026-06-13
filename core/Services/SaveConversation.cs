using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Handles saving conversations and coordinating the memory pipeline.
/// Uses MemoryPipeline to ensure clean separation of concerns.
/// No direct summary generation - delegates to pipeline services.
/// </summary>
public static class SaveConversation
{
    public static async Task SaveConversationAsync(
        List<string> conversationLog,
        AlissaClient alissa,
        AppConfig config,
        string basePath,
        IChatClient? chatClient = null,
        IPromptBuilder? promptBuilder = null)
    {
        try
        {
            string logsDir = Path.Combine(basePath, "logs", "conversations");
            Directory.CreateDirectory(logsDir);

            string collectedEmojis = alissa.CurrentSession.CollectedEmojis;
            string conversationPath = Path.Combine(logsDir, $"conversation_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

            File.WriteAllLines(conversationPath, conversationLog);
            File.AppendAllText(conversationPath, Environment.NewLine + Environment.NewLine + $"Emojis: {collectedEmojis}");

            Alissa.Core.Utils.TextHandler.PrintText($"Conversation saved: {conversationPath}");

            if (config.Settings.EnableSummaries && chatClient != null && promptBuilder != null)
            {
                await ProcessConversationMemoryAsync(conversationLog, alissa, config, basePath, chatClient, promptBuilder);
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.Handle(ex, basePath, verbose: true);
        }
    }

    private static async Task ProcessConversationMemoryAsync(
        List<string> conversationLog,
        AlissaClient alissa,
        AppConfig config,
        string basePath,
        IChatClient chatClient,
        IPromptBuilder promptBuilder)
    {
        try
        {
            string conversationText = string.Join("\n", conversationLog);

            var summaryService = new SummaryGenerationService(chatClient, promptBuilder);
            var extractionService = new MemoryExtractionService(chatClient, promptBuilder);
            var memoryManager = new MemoryManager(basePath, config.Memory);
            var mediumTermService = new MediumTermMemoryService(basePath, 50, config.PromptRules.IncludeMediumTermMemory);
            var indexBuilder = new MemoryIndexBuilder(basePath, config.IndexingRules, memoryManager);

            var pipeline = new MemoryPipeline(
                summaryService,
                extractionService,
                memoryManager,
                mediumTermService,
                indexBuilder);

            var sessionId = alissa.CurrentSession.SessionId;
            var summary = await pipeline.ProcessConversationAsync(conversationText, sessionId);

            Alissa.Core.Utils.TextHandler.PrintText("Conversation processed through memory pipeline.");

            if (!string.IsNullOrWhiteSpace(summary.Summary))
            {
                string summariesDir = Path.Combine(basePath, "logs", "summaries");
                Directory.CreateDirectory(summariesDir);
                var summaryPath = Path.Combine(summariesDir, $"summary_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
                File.WriteAllText(summaryPath, summary.Summary);
                Alissa.Core.Utils.TextHandler.PrintText($"Summary saved: {summaryPath}");
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.Handle(ex, basePath, false);
        }
    }
}

