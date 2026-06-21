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
    //  Serial Number:  4.1.2
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
    //      User messages from the console or Hub.
    //
    //  Output:
    //      AI responses and logs through TextManager.
    //
    //  Logic Steps:
    //      1. Setup paths and configuration
    //      2. Configure TextManager
    //      3. Initialize services
    //      4. Run main chat loop
    //      5. Save conversation and shutdown
    //
    //  Limitations, Constraints and Assumptions:
    //      - Assumes config and memory files exist and are valid.
    //      - Console and Hub modes supported.
    //      - All model communication goes through AlissaClient.
    //
    //  Dependencies and Environment:
    //      C# 14.0, .NET 10.0, Console, Hub 317
    //
    //  Constants: NONE
    //
    //  Methods and Functions:
    //      Main()                - Application entry point
    //      ResolveBasePath()     - Locates project root
    //      LoadConfiguration()   - Loads application configuration
    //      RunChatLoop()         - Executes chat session
    //      ProcessUserMessage()  - Handles a user request
    //      FinalizeSession()     - Saves session and shuts down
    //
    //  Architecture and Design:
    //      Program
    //          -> AlissaClient
    //              -> PromptBuilder
    //              -> MemoryManager
    //              -> SessionManager
    //              -> OllamaClient
    //
    //  Notes:
    //      All output is routed through TextManager.
    //      All model interaction is routed through AlissaClient.
    //=======================================================================================================================================//

    internal class Program
    {
        static async Task Main(string[] args)
        {
            bool hubMode = TextManager.TryConfigureFromArgs(args);

            if (!hubMode)
            {
                TextManager.Configure(OutputMode.Console);
            }

            string basePath = ResolveBasePath();
            AppConfig? config = LoadConfiguration(basePath);

            if (config is null)
            {
                TextManager.Status("Failed to load configuration. Exiting.");
                return;
            }

            await RunChatLoop(basePath, config);
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
            AppConfig? config = null;

            try
            {
                config = ConfigService.LoadAll(basePath);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, basePath, true);
                TextManager.Status("[Error loading config. Exiting.]");
            }

            return config;
        }

        private static async Task RunChatLoop(string basePath, AppConfig config)
        {
            IMemoryManager memoryManager = new MemoryManager(basePath, config.Memory);
            IChatClient chatClient = new OllamaClient(
                config.Model.ModelName,
                config.Model.KeepAliveMinutes);

            IPromptBuilder promptBuilder = new PromptBuilder(
                basePath,
                memoryManager,
                config.PromptRules,
                config.PersonalityRules);

            ISessionManager sessionManager = new SessionManager(basePath);

            AlissaClient alissa = new AlissaClient(
                chatClient,
                promptBuilder,
                memoryManager,
                sessionManager);

            TextManager.Status(
                $"\n🐱 Alissa is online using model '{config.Model.ModelName}'! " +
                (TextManager.IsHubMode
                    ? "Routing through Hub 317."
                    : "Type 'exit' to quit."));

            List<string> conversationLog = [];
            bool running = true;

            while (running)
            {
                if (!TextManager.IsHubMode)
                {
                    TextManager.NextLine();
                    TextManager.PrintText("You: ");
                }

                string? userInput = await TextManager.ReadInputAsync();

                if (!string.IsNullOrWhiteSpace(userInput))
                {
                    bool isExitCommand =
                        !TextManager.IsHubMode &&
                        string.Equals(
                            userInput.Trim(),
                            "exit",
                            StringComparison.OrdinalIgnoreCase);

                    if (isExitCommand)
                    {
                        running = false;
                    }
                    else
                    {
                        if (!TextManager.IsHubMode)
                        {
                            TextManager.NextLine();
                        }

                        await ProcessUserMessage(
                            userInput,
                            alissa,
                            conversationLog,
                            basePath);
                    }
                }
            }

            await FinalizeSession(
                conversationLog,
                alissa,
                config,
                basePath,
                chatClient,
                promptBuilder);
        }

        private static async Task ProcessUserMessage(
            string userInput,
            AlissaClient alissa,
            List<string> conversationLog,
            string basePath)
        {
            try
            {
                TextManager.BeginResponse();

                StringBuilder fullReply = new();

                await foreach (string token in alissa.StreamAsync(userInput))
                {
                    TextManager.PrintToken(token);
                    fullReply.Append(token);
                }

                TextManager.EndResponse();

                conversationLog.Add($"User: {userInput}");
                conversationLog.Add($"Alissa: {fullReply}");
            }
            catch (HttpRequestException ex)
            {
                ErrorHandler.Handle(ex, basePath, true);
                TextManager.Status("[Ollama is not running]");
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, basePath, true);
                TextManager.Status("[An error occurred during chat]");
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

            TextManager.Blank();
            TextManager.Status("\n🐱 Goodbye!\n");
        }
    }
}