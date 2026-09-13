using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Services;

/// <summary>
/// Handles saving conversations and coordinating the memory pipeline.
/// Uses MemoryPipeline to ensure clean separation of concerns.
/// No direct summary generation - delegates to pipeline services.
/// </summary>
public static class SaveConversation
{
    // Constants
    private const string LOGS_DIR_NAME = "logs";
    private const string CONVERSATIONS_DIR_NAME = "conversations";
    private const string CONVERSATION_PREFIX = "conversation_";
    private const string FILE_EXTENSION = ".txt";
    private const string TIMESTAMP_FORMAT = "yyyy-MM-dd_HH-mm-ss";
    private const string EMOJIS_PREFIX = "Emojis: ";
    private const string SUMMARIES_DIR_NAME = "summaries";
    private const string SUMMARY_PREFIX = "summary_";
    private const string SAVED_MESSAGE = "Conversation saved: ";
    private const string PROCESSED_MESSAGE = "Conversation processed through memory pipeline.";
    private const string SUMMARY_SAVED_MESSAGE = "Summary saved: ";
    private const string NEWLINE = "\n";
    private const int MEDIUM_TERM_SERVICE_DEFAULT_CAPACITY = 50;

    public static async Task SaveConversationAsync(
        List<string> conversationLog,
        AlissaClient alissa,
        AppConfig config,
        string basePath,
        IChatClient? chatClient = null,
        IPromptBuilder? promptBuilder = null,
        MediumTermMemoryService? mediumTermMemoryService = null)
    {
        try
        {
            string logsDir = Path.Combine(basePath, LOGS_DIR_NAME, CONVERSATIONS_DIR_NAME);
            Directory.CreateDirectory(logsDir);

            string collectedEmojis = alissa.CurrentSession.CollectedEmojis;
            string conversationPath = Path.Combine(logsDir, CONVERSATION_PREFIX + DateTime.Now.ToString(TIMESTAMP_FORMAT) + FILE_EXTENSION);

            File.WriteAllLines(conversationPath, conversationLog);
            File.AppendAllText(conversationPath, Environment.NewLine + Environment.NewLine + EMOJIS_PREFIX + collectedEmojis);

            Alissa.Core.Utils.TextManager.Status(SAVED_MESSAGE + conversationPath);

            bool shouldProcessMemory = config.Settings.EnableSummaries && chatClient != null && promptBuilder != null;
            if (shouldProcessMemory)
            {
                await ProcessConversationMemoryAsync(conversationLog, alissa, config, basePath, chatClient, promptBuilder, mediumTermMemoryService);
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
        IPromptBuilder promptBuilder,
        MediumTermMemoryService? injectedMediumTermService = null)
    {
        try
        {
            string conversationText = string.Join(NEWLINE, conversationLog);

            SummaryGenerationService summaryService = new SummaryGenerationService(chatClient, promptBuilder);
            MemoryExtractionService extractionService = new MemoryExtractionService(chatClient, promptBuilder);
            MemoryManager memoryManager = new MemoryManager(basePath, config.Memory);
            MediumTermMemoryService mediumTermService = injectedMediumTermService ?? new MediumTermMemoryService(basePath, MEDIUM_TERM_SERVICE_DEFAULT_CAPACITY, config.PromptRules.IncludeMediumTermMemory);
            MemoryIndexBuilder indexBuilder = new MemoryIndexBuilder(basePath, config.IndexingRules, memoryManager);

            MemoryPipeline pipeline = new MemoryPipeline(
                summaryService,
                extractionService,
                memoryManager,
                mediumTermService,
                indexBuilder);

            string sessionId = alissa.CurrentSession.SessionId;
            ConversationSummary summary = await pipeline.ProcessConversationAsync(conversationText, sessionId);

            Alissa.Core.Utils.TextManager.Status(PROCESSED_MESSAGE);

            bool hasSummary = !string.IsNullOrWhiteSpace(summary.Summary);
            if (hasSummary)
            {
                string summariesDir = Path.Combine(basePath, LOGS_DIR_NAME, SUMMARIES_DIR_NAME);
                Directory.CreateDirectory(summariesDir);
                string summaryPath = Path.Combine(summariesDir, SUMMARY_PREFIX + DateTime.Now.ToString(TIMESTAMP_FORMAT) + FILE_EXTENSION);
                File.WriteAllText(summaryPath, summary.Summary);
                Alissa.Core.Utils.TextManager.Status(SUMMARY_SAVED_MESSAGE + summaryPath);
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.Handle(ex, basePath, false);
        }
    }
}

