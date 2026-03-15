using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Alissa.Main
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // ===============================
            // Base Paths
            // ===============================
            string basePath = AppContext.BaseDirectory;

            while (!Directory.Exists(Path.Combine(basePath, "config")))
            {
                basePath = Directory.GetParent(basePath)!.FullName;
            }

            string summariesDir = Path.Combine(basePath, "logs", "summaries");
            Directory.CreateDirectory(summariesDir);
            string logsDir = Path.Combine(basePath, "logs", "conversations");
            Directory.CreateDirectory(logsDir);

            // ===============================
            // Load Model Config
            // ===============================
            AppConfig config;
            try
            {
                config = ConfigService.LoadAll(basePath);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, basePath, true);
                return;
            }

            string modelName = config.Model.ModelName;

            // ===============================
            // Dependency Injection Setup
            // ===============================
            IMemoryManager memoryManager = new MemoryManager(basePath, config.Memory);
            IPromptBuilder promptBuilder = new PromptBuilder(basePath, memoryManager);
            IChatClient chatClient = new OllamaClient(config.Model.ModelName, config.Model.KeepAliveMinutes);
            ISessionManager sessionManager = new SessionManager(basePath);

            AlissaClient alissa = new AlissaClient(chatClient, promptBuilder, memoryManager, sessionManager);

            Console.WriteLine($"Alissa is online using model '{modelName}'! Type 'exit' to quit.\n");
            Console.WriteLine();

            // ===============================
            // Main Loop
            // ===============================
            bool running = true;
            List<string> conversationLog = new List<string>();
            var emojiCollector = new StringBuilder();
            var sessionMessages = memoryManager.LoadSessionCache();

            while (running)
            {
                Console.Write("You: ");
                string? userInput = Console.ReadLine();
                Console.WriteLine();

                if (!(string.IsNullOrWhiteSpace(userInput)))
                {
                    if (userInput.Trim().ToLower() == "exit")
                    {
                        running = false;
                    }

                    if (running)
                    {
                        try
                        {
                            Console.Write("Alissa: ");

                            StringBuilder fullReply = new StringBuilder();

                            await foreach (var token in ((OllamaClient)chatClient).StreamAsync(
                                promptBuilder.BuildSystemPrompt(),
                                userInput,
                                emojis =>
                                {
                                    emojiCollector.Append(emojis);
                                    alissa.CurrentSession.LatestEmoji = emojis;
                                    alissa.CurrentSession.CollectedEmojis += emojis;
                                }))
                            {
                                Console.Write(token);
                                fullReply.Append(token);
                            }


                            Console.WriteLine();
                            Console.WriteLine();

                            conversationLog.Add($"User: {userInput}");
                            conversationLog.Add($"Alissa: {fullReply}");
                        }
                        catch (HttpRequestException ex)
                        {
                            ErrorHandler.Handle(ex, basePath, true);
                            Console.WriteLine("[Ollama is not running]");
                            Console.WriteLine(ex.Message);
                        }
                        catch (Exception ex)
                        {
                            ErrorHandler.Handle(ex, basePath, true);
                            Console.WriteLine("[An error occurred during chat]");
                        }
                    }

                }
            }

            // ===============================
            // Save Conversation
            // ===============================
            try
            {
                await SaveConversation.SaveConversationAsync(conversationLog, alissa, config, basePath);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, basePath, true);
            }

            // ===============================
            // End of Program
            // ===============================
            Console.WriteLine();
            Console.WriteLine("Goodbye!");
            Console.WriteLine();
        }
    }
}
