using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public static class SaveConversation
{
    public static async Task SaveConversationAsync(
        List<string> conversationLog,
        AlissaClient alissa,
        AppConfig config,
        string basePath)
    {
        try
        {
            string logsDir = Path.Combine(basePath, "logs", "conversations");
            string summariesDir = Path.Combine(basePath, "logs", "summaries");

            Directory.CreateDirectory(logsDir);
            Directory.CreateDirectory(summariesDir);

            string collectedEmojis = alissa.CurrentSession.CollectedEmojis;

            int conversationLength = conversationLog.Count;
            int factor = config.Limits.SummaryDivisionFactor;

            int lineErrorLow = Math.Max(1, (conversationLength / factor) - 1);
            int lineErrorHigh = Math.Max(1, (conversationLength / factor) + 1);
            if (lineErrorLow == 0) { lineErrorLow++; lineErrorHigh++; }
            if (lineErrorHigh == lineErrorLow) { lineErrorHigh++; }

            string conversationPath = Path.Combine(logsDir, $"conversation_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
            File.WriteAllLines(conversationPath, conversationLog);

            // Extract and save conversation summary as memory
            if (config.Settings.EnableSummaries)
            {
                string summaryPrompt = $"Summarize this conversation in {lineErrorLow} to {lineErrorHigh} lines:";
                string summary = await alissa.SendAsync(summaryPrompt + "\n\n" + string.Join("\n", conversationLog));

                string summaryPath = Path.Combine(summariesDir, $"summary_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
                File.WriteAllText(summaryPath, summary);

                // Save conversation summary to memory root
                var memoryManager = new MemoryManager(basePath, config.Memory);
                memoryManager.SaveConversationSummary(summary);

                // Also save as a fact entry for context
                var summaryFact = new MemoryEntry(
                    key: "conversation_summary",
                    value: summary,
                    relevance: 0.8,
                    isCore: false
                );
                memoryManager.SaveFact(summaryFact);
            }
            
            File.AppendAllText(conversationPath, Environment.NewLine + Environment.NewLine + $"Emojis: {collectedEmojis}");
            Console.WriteLine($"Conversation saved: {conversationPath}");
            if (config.Settings.EnableSummaries)
                Console.WriteLine($"Summary saved: {Path.Combine(summariesDir, $"summary_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt")}");
        }
        catch (Exception ex)
        {
            ErrorHandler.Handle(ex, basePath, verbose: true);
        }
    }
}
