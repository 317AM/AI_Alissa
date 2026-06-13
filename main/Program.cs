using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Services;
using Alissa.Core.Utils;
using System.Text;

namespace Alissa.Main
{
    //=======================================================================================================================================//
    //  FILE HEADER — Alissa Main Program — BLOCK HEADER COMMENT
    // ------------------------------------------------------------------------------------------------------------------------------------ //
    //
    //  Serial Number:  41.1.2
    //  Program Name:   Alissa
    //  Project Name:   AI_Alissa
    //
    //  File:     Program.cs
    //  Location: D:/OneDrive - HTBLA Leonding/Projects/AI_Alissa/main/Program.cs
    //  Language: C# 14.0
    //  Dotnet:   .NET 10.0
    //  IDE used: Visual Studio Community 2026
    //
    //  Author:   Konnerth Daniel
    //  Date:     31st January 2026
    //
    // ------------------------------------------------------------------------------------------------------------------------------------ //
    //
    //  Instructions Location: NONE
    //
    //  Task Description:
    //      Main entry point for the Alissa AI chat application. Handles setup, chat loop, and shutdown.
    //
    //  Purpose:
    //      Launches the chat, manages session, and coordinates memory and prompt updates.
    //      Routes all communication through AlissaClient for proper architecture.
    //
    //  Input:
    //      User messages from the console.
    //
    //  Output:
    //      AI responses and logs to the console.
    //
    //  Logic Steps:
    //      1. Setup paths and config
    //      2. Dependency injection
    //      3. Main chat loop (via AlissaClient)
    //      4. Save conversation and shutdown
    //
    //  Limitations, Constraints and Assumptions:
    //      - Assumes config and memory files exist and are valid.
    //      - Console application only.
    //      - All model communication goes through AlissaClient.
    //
    //  Dependencies and Environment:
    //      C# 14.0, .NET 10.0, Console
    //
    //  Constants: NONE
    //
    //  Methods and Functions:
    //      Main() – Entry point
    //
    //  Architecture and Design:
    //      See README.md and docs/architecture.md
    //
    //  Notes:
    //      All printing is routed through TextPrinter.PrintText.
    //      All model calls go through AlissaClient.
    //=======================================================================================================================================//

    internal class Program
    {
        static async Task Main(string[] args)
        {
            string basePath = ResolveBasePath();
            var config = LoadConfiguration(basePath);
            bool configLoaded = config != null;

            if (configLoaded)
            {
                await RunChatLoop(basePath, config!);
            }
            else
            {
                Alissa.Core.Utils.TextHandler.PrintText("Failed to load configuration. Exiting.", true);
            }
        }

        private static string ResolveBasePath()
        {
            string basePath = AppContext.BaseDirectory;

            while (!Directory.Exists(Path.Combine(basePath, "config")))
            {
                basePath = Directory.GetParent(basePath)!.FullName;
            }

            Directory.CreateDirectory(Path.Combine(basePath, "logs", "summaries"));
            Directory.CreateDirectory(Path.Combine(basePath, "logs", "conversations"));

            return basePath;
        }

        private static AppConfig? LoadConfiguration(string basePath)
        {
            try
            {
                return ConfigService.LoadAll(basePath);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, basePath, true);
                Alissa.Core.Utils.TextHandler.PrintText("[Error loading config. Exiting.]", true);
                return null;
            }
        }

        private static async Task RunChatLoop(string basePath, AppConfig config)
        {
            IMemoryManager memoryManager = new MemoryManager(basePath, config.Memory);
            IChatClient chatClient = new OllamaClient(config.Model.ModelName, config.Model.KeepAliveMinutes);
            IPromptBuilder promptBuilder = new PromptBuilder(basePath, memoryManager, config.PromptRules, config.PersonalityRules);
            ISessionManager sessionManager = new SessionManager(basePath);

            AlissaClient alissa = new AlissaClient(chatClient, promptBuilder, memoryManager, sessionManager);

            Alissa.Core.Utils.TextHandler.PrintText($"\n🐱 Alissa is online using model '{config.Model.ModelName}'! Type 'exit' to quit.", true);

            var conversationLog = new List<string>();
            bool running = true;

            while (running)
            {
                Alissa.Core.Utils.TextHandler.NextLine();
                Alissa.Core.Utils.TextHandler.PrintText("You: ");
                string? userInput = TextHandler.ReadText();

                bool hasInput = !string.IsNullOrWhiteSpace(userInput);

                if (hasInput)
                {
                    bool isExitCommand = userInput!.Trim().ToLower() == "exit";

                    if (isExitCommand)
                    {
                        running = false;
                    }
                    else
                    {
                        Alissa.Core.Utils.TextHandler.NextLine();
                        await ProcessUserMessage(userInput, alissa, conversationLog, config, basePath, chatClient, promptBuilder);
                    }
                }
            }

            await FinalizeSession(conversationLog, alissa, config, basePath, chatClient, promptBuilder);
        }

        private static async Task ProcessUserMessage(
            string userInput,
            AlissaClient alissa,
            List<string> conversationLog,
            AppConfig config,
            string basePath,
            IChatClient chatClient,
            IPromptBuilder promptBuilder)
        {
            try
            {
                Alissa.Core.Utils.TextHandler.PrintText("Alissa: ");

                var fullReply = new StringBuilder();

                await foreach (var token in alissa.StreamAsync(userInput))
                {
                    Alissa.Core.Utils.TextHandler.PrintText(token);
                    fullReply.Append(token);
                }

                Alissa.Core.Utils.TextHandler.PrintText("\n");

                conversationLog.Add($"User: {userInput}");
                conversationLog.Add($"Alissa: {fullReply.ToString()}");
            }
            catch (HttpRequestException ex)
            {
                ErrorHandler.Handle(ex, basePath, true);
                Alissa.Core.Utils.TextHandler.PrintText("[Ollama is not running]");
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, basePath, true);
                Alissa.Core.Utils.TextHandler.PrintText("[An error occurred during chat]");
            }
        }

        private static async Task FinalizeSession(
            List<string> conversationLog,
            AlissaClient alissa,
            AppConfig config,
            string basePath,
            IChatClient chatClient,
            IPromptBuilder promptBuilder)
        {
            try
            {
                if (conversationLog.Count > 0)
                {
                    await SaveConversation.SaveConversationAsync(
                        conversationLog,
                        alissa,
                        config,
                        basePath,
                        chatClient,
                        promptBuilder);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, basePath, true);
            }

            Alissa.Core.Utils.TextHandler.NextLine();
            Alissa.Core.Utils.TextHandler.PrintText("\n🐱 Goodbye!\n", true);
        }
    }
}
