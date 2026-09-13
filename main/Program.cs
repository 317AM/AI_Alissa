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

    public class Program
    {
        // ══════════════════════════════════════════════════════════════════════════════════════════════════════════════
        // Constants
        // ══════════════════════════════════════════════════════════════════════════════════════════════════════════════

        private const string CONFIG_DIRECTORY = "config";
        private const string LOGS_DIRECTORY = "logs";
        private const string SUMMARIES_SUBDIRECTORY = "summaries";
        private const string CONVERSATIONS_SUBDIRECTORY = "conversations";
        private const string ERROR_CONFIG_MESSAGE = "Failed to load configuration. Exiting.";
        private const string STATUS_ONLINE = "🐱 Alissa is online using model '{0}'! {1}";
        private const string STATUS_HUB_ROUTING = "Routing through Hub 317.";
        private const string STATUS_CONSOLE_HINT = "Type 'exit' to quit.";
        private const string PROMPT_INPUT = "You: ";
        private const string EXIT_COMMAND = "exit";
        private const string STATUS_GOODBYE = "\n🐱 Goodbye!\n";
        private const string STATUS_OLLAMA_OFFLINE = "[Ollama is not running]";
        private const string STATUS_CHAT_ERROR = "[An error occurred during chat]";
        private const string COMMAND_SLASH_PREFIX = "/";
        private const string COMMAND_READ = "read";
        private const string COMMAND_ANALYZE = "analyze";
        private const string COMMAND_MODIFY = "modify";
        private const string COMMAND_BACKUP = "backup";
        private const string COMMAND_VOICE = "voice";
        private const string COMMAND_ARG_KEY = "arg";
        private const string COMMAND_USAGE_READ = "Usage: /read <filepath>";
        private const string COMMAND_USAGE_ANALYZE = "Usage: /analyze <filepath>";
        private const string COMMAND_USAGE_MODIFY = "Usage: /modify <task description>";
        private const string COMMAND_USAGE_VOICE = "Usage: /voice <filepath>";
        private const string COMMAND_RESULT_BACKUP = "Backup created (if a current file is being tracked)";
        private const string COMMAND_MODIFY_STUB = "Modification request: {0} (requires code context)";
        private const int CONTENT_PREVIEW_LENGTH = 500;
        private const string CONTENT_ELLIPSIS = "...";
        private const int MEDIUM_TERM_MEMORY_MAX_ENTRIES = 50;
        private const string CONVERSATION_LOG_USER_PREFIX = "User: ";
        private const string CONVERSATION_LOG_ALISSA_PREFIX = "Alissa: ";
        private const string CONVERSATION_LOG_COMMAND_PREFIX = "Command Result: ";
        private const string LOG_EMPTY_RESPONSE = "";

        // ══════════════════════════════════════════════════════════════════════════════════════════════════════════════
        // Methods
        // ══════════════════════════════════════════════════════════════════════════════════════════════════════════════

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
                TextManager.Status(ERROR_CONFIG_MESSAGE);
                return;
            }

            // Resolve relative paths in speech config against project root
            config.Speech.ResolveAgainst(basePath);

            await RunChatLoop(basePath, config);
        }

        private static string ResolveBasePath()
        {
            string basePath = AppContext.BaseDirectory;

            while (!Directory.Exists(Path.Combine(basePath, CONFIG_DIRECTORY)))
            {
                basePath = Directory.GetParent(basePath)!.FullName;
            }

            string logsPath = Path.Combine(basePath, LOGS_DIRECTORY);
            string summariesPath = Path.Combine(logsPath, SUMMARIES_SUBDIRECTORY);
            string conversationsPath = Path.Combine(logsPath, CONVERSATIONS_SUBDIRECTORY);

            Directory.CreateDirectory(summariesPath);
            Directory.CreateDirectory(conversationsPath);

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
                TextManager.Status(ERROR_CONFIG_MESSAGE);
            }

            return config;
        }

        private static async Task RegisterBuiltInCommands(
            ICommandService commandService,
            ICodeIntegrationService codeIntegrationService,
            string basePath,
            IPromptBuilder promptBuilder,
            ISpeechToTextService? speechToTextService = null)
        {
            // /read <filepath> — read and display a code file
            await commandService.RegisterCommandAsync(COMMAND_READ, async (Dictionary<string, object> parameters) =>
            {
                string result = LOG_EMPTY_RESPONSE;
                bool hasArg = parameters.ContainsKey(COMMAND_ARG_KEY);

                if (hasArg)
                {
                    string filePath = (string)parameters[COMMAND_ARG_KEY];
                    string content = await codeIntegrationService.ReadCodeFileAsync(filePath);
                    result = content.Length > CONTENT_PREVIEW_LENGTH
                        ? content.Substring(0, CONTENT_PREVIEW_LENGTH) + CONTENT_ELLIPSIS
                        : content;
                }
                else
                {
                    result = COMMAND_USAGE_READ;
                }

                return await Task.FromResult(result);
            });

            // /analyze <filepath> — analyze a code file
            await commandService.RegisterCommandAsync(COMMAND_ANALYZE, async (Dictionary<string, object> parameters) =>
            {
                string result = LOG_EMPTY_RESPONSE;
                bool hasArg = parameters.ContainsKey(COMMAND_ARG_KEY);

                if (hasArg)
                {
                    string filePath = (string)parameters[COMMAND_ARG_KEY];
                    result = await codeIntegrationService.AnalyzeCodeAsync(filePath);
                }
                else
                {
                    result = COMMAND_USAGE_ANALYZE;
                }

                return result;
            });

            // /modify <task> — generate code modifications
            await commandService.RegisterCommandAsync(COMMAND_MODIFY, async (Dictionary<string, object> parameters) =>
            {
                string result = LOG_EMPTY_RESPONSE;
                bool hasArg = parameters.ContainsKey(COMMAND_ARG_KEY);

                if (hasArg)
                {
                    string task = (string)parameters[COMMAND_ARG_KEY];
                    result = string.Format(COMMAND_MODIFY_STUB, task);
                }
                else
                {
                    result = COMMAND_USAGE_MODIFY;
                }

                return result;
            });

            // /backup — create a backup of the current file
            await commandService.RegisterCommandAsync(COMMAND_BACKUP, async (Dictionary<string, object> parameters) =>
            {
                return await Task.FromResult(COMMAND_RESULT_BACKUP);
            });

            // /voice <filepath> — transcribe audio file (requires STT enabled)
            // Transcribe the file and feed transcript to StreamAsync so she answers
            await commandService.RegisterCommandAsync(COMMAND_VOICE, async (Dictionary<string, object> parameters) =>
            {
                string result = LOG_EMPTY_RESPONSE;
                bool hasSpeechToText = speechToTextService != null;
                bool hasArg = parameters.ContainsKey(COMMAND_ARG_KEY);

                if (!hasSpeechToText)
                {
                    result = "Speech-to-text is not enabled in configuration.";
                }
                else if (!hasArg)
                {
                    result = COMMAND_USAGE_VOICE;
                }
                else
                {
                    try
                    {
                        string filePath = (string)parameters[COMMAND_ARG_KEY];
                        string transcription = await speechToTextService!.TranscribeAsync(filePath);
                        result = transcription;

                        // Note: The transcript will be processed further in ProcessUserMessage
                        // if this was called as a slash command. For now, just return the text.
                    }
                    catch (Exception ex)
                    {
                        result = $"Error transcribing audio: {ex.Message}";
                    }
                }

                return result;
            });

            // /talk — record from microphone until Enter is pressed (console only)
            await commandService.RegisterCommandAsync("talk", async (Dictionary<string, object> parameters) =>
            {
                string result = LOG_EMPTY_RESPONSE;
                bool hasSpeechToText = speechToTextService != null;
                bool isConsoleMode = !TextManager.IsHubMode;

                if (!hasSpeechToText)
                {
                    result = "Speech-to-text is not enabled in configuration.";
                }
                else if (!isConsoleMode)
                {
                    result = "The /talk command is only available in console mode (use Hub audio input for web mode).";
                }
                else
                {
                    try
                    {
                        bool hasMicRecorder = _micRecorder != null;
                        if (!hasMicRecorder)
                        {
                            result = "Microphone recorder not initialized.";
                        }
                        else
                        {
                            byte[] wavBytes = await _micRecorder!.RecordUntilEnterAsync();
                            bool recordingEmpty = wavBytes.Length == 0;
                            if (recordingEmpty)
                            {
                                result = LOG_EMPTY_RESPONSE; // Short recording, discard
                            }
                            else
                            {
                                string transcript = await speechToTextService!.TranscribeAsync(wavBytes, "wav");
                                result = transcript;

                                // Note: The transcript will be processed further if this is called as a command
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result = $"Error recording audio: {ex.Message}";
                    }
                }

                return result;
            });

            // /mic — alias for /talk
            await commandService.RegisterCommandAsync("mic", async (Dictionary<string, object> parameters) =>
            {
                // Delegate to /talk
                return await commandService.ExecuteCommandAsync("talk", parameters);
            });
        }

        private static ConsoleAudioPlayer? _audioPlayer;
        private static MicrophoneRecorder? _micRecorder;

        private static async Task RunChatLoop(string basePath, AppConfig config)
        {
            IMemoryManager memoryManager = new MemoryManager(basePath, config.Memory);
            IChatClient chatClient = new OllamaClient(
                config.Model.ModelName,
                config.Model.KeepAliveMinutes,
                config.Model.MaxTokens,
                config.Model.Temperature,
                config.Model.EnableThinking);

            IUserContextService userContextService = new UserContextService(basePath);

            MediumTermMemoryService mediumTermMemoryService = new MediumTermMemoryService(
                basePath,
                maxEntries: MEDIUM_TERM_MEMORY_MAX_ENTRIES,
                enabled: config.PromptRules.IncludeMediumTermMemory);

            IThoughtService thoughtService = new ThoughtService(basePath, chatClient);

            IPromptBuilder promptBuilder = new PromptBuilder(
                basePath,
                memoryManager,
                config.PromptRules,
                config.PersonalityRules,
                mediumTermMemoryService,
                thoughtService,
                userContextService);

            ISessionManager sessionManager = new SessionManager(basePath);

            ICodeIntegrationService codeIntegrationService = new CodeIntegrationService(basePath);
            ICommandService commandService = new CommandService();

            // Instantiate speech services if enabled
            IProcessRunner processRunner = new ProcessRunner();

            ISpeechToTextService? speechToTextService = null;
            bool sttEnabled = config.Speech.SttEnabled;
            if (sttEnabled)
            {
                try
                {
                    // Verify paths are resolved
                    bool binaryExists = File.Exists(config.Speech.WhisperBinaryPath);
                    bool modelExists = File.Exists(config.Speech.WhisperModelPath);

                    if (!binaryExists)
                    {
                        TextManager.Status($"[STT Error] Whisper binary not found at: {config.Speech.WhisperBinaryPath}");
                    }
                    else if (!modelExists)
                    {
                        TextManager.Status($"[STT Error] Whisper model not found at: {config.Speech.WhisperModelPath}");
                    }
                    else
                    {
                        speechToTextService = new WhisperSpeechToTextService(processRunner, config.Speech);
                    }
                }
                catch (Exception ex)
                {
                    TextManager.Status($"[STT Error] Failed to initialize: {ex.Message}");
                }
            }

            ITextToSpeechService? textToSpeechService = null;
            bool ttsEnabled = config.Speech.TtsEnabled;

            if (ttsEnabled)
            {
                // Detect TTS engine availability
                bool piperAvailable = File.Exists(config.Speech.PiperBinaryPath) && File.Exists(config.Speech.PiperVoiceModelPath);
                bool styletts2Available = File.Exists(config.Speech.StyleTts2PythonExe)
                    && File.Exists(config.Speech.StyleTts2ServerScript)
                    && File.Exists(config.Speech.StyleTts2CheckpointPath)
                    && File.Exists(config.Speech.StyleTts2ConfigPath);

                // Print availability status
                string piperStatus = piperAvailable ? "yes" : "no";
                string styletts2Status = styletts2Available ? "yes" : "no";
                TextManager.Status($"TTS detected: piper={piperStatus} styletts2={styletts2Status}");

                if (!piperAvailable)
                {
                    TextManager.Status($"[TTS Error] Piper binary not found at: {config.Speech.PiperBinaryPath}");
                }

                if (!styletts2Available && styletts2Available == false)
                {
                    if (!File.Exists(config.Speech.StyleTts2PythonExe))
                    {
                        TextManager.Status($"[TTS Info] StyleTTS2 Python not found at: {config.Speech.StyleTts2PythonExe}");
                    }

                    if (!File.Exists(config.Speech.StyleTts2ServerScript))
                    {
                        TextManager.Status($"[TTS Info] StyleTTS2 server script not found at: {config.Speech.StyleTts2ServerScript}");
                    }

                    if (!File.Exists(config.Speech.StyleTts2CheckpointPath))
                    {
                        TextManager.Status($"[TTS Info] StyleTTS2 checkpoint not found at: {config.Speech.StyleTts2CheckpointPath}");
                    }

                    if (!File.Exists(config.Speech.StyleTts2ConfigPath))
                    {
                        TextManager.Status($"[TTS Info] StyleTTS2 config not found at: {config.Speech.StyleTts2ConfigPath}");
                    }
                }

                // Determine which engine to use
                string selectedEngine = PickTtsEngine(piperAvailable, styletts2Available, TextManager.IsHubMode);

                if (string.IsNullOrEmpty(selectedEngine))
                {
                    TextManager.Status("TTS off (no engines found)");
                }
                else if (selectedEngine.Equals("styletts2", StringComparison.OrdinalIgnoreCase))
                {
                    // Try to use StyleTTS2 with fallback to Piper
                    textToSpeechService = await InitializeStyleTts2Async(config, basePath, piperAvailable, processRunner);

                    if (textToSpeechService == null)
                    {
                        // Fallback to Piper if StyleTTS2 failed
                        if (piperAvailable)
                        {
                            textToSpeechService = InitializePiper(processRunner, config);
                        }
                    }
                    else
                    {
                        TextManager.Status("TTS: styletts2");
                    }
                }
                else
                {
                    // Use Piper
                    if (piperAvailable)
                    {
                        try
                        {
                            textToSpeechService = new PiperTextToSpeechService(processRunner, config.Speech);
                            TextManager.Status("TTS: piper");
                        }
                        catch (Exception ex)
                        {
                            TextManager.Status($"[TTS Error] Piper initialization failed: {ex.Message}");
                        }
                    }
                }
            }

            // Register built-in commands
            await RegisterBuiltInCommands(commandService, codeIntegrationService, basePath, promptBuilder, speechToTextService);

            AlissaClient alissa = new AlissaClient(
                chatClient,
                promptBuilder,
                memoryManager,
                sessionManager,
                thoughtService,
                speechToTextService,
                textToSpeechService);

            // Hook up audio output to TextManager (Hub317 mode sends audio as base64 chunks)
            bool hasTts = textToSpeechService != null;
            if (hasTts)
            {
                _audioPlayer = new ConsoleAudioPlayer(config.Speech.TempDirectory);
                alissa.OnAudioReady += (audioBytes) =>
                {
                    // Always send to Hub for Hub mode compatibility
                    TextManager.SendAudioChunk(audioBytes);

                    // Also play locally in Console mode if TTS enabled
                    bool isConsoleMode = !TextManager.IsHubMode;
                    if (isConsoleMode)
                    {
                        _audioPlayer?.PlayWav(audioBytes);
                    }
                };
            }

            // Initialize microphone recorder for /talk command
            bool shouldUseMic = sttEnabled && !TextManager.IsHubMode;
            if (shouldUseMic)
            {
                _micRecorder = new MicrophoneRecorder();
            }

            // Start API host if enabled or if --api-port environment variable is set
            Task apiHostTask = ApiHostFactory.CreateAndStartApiHostAsync(
                config,
                basePath,
                codeIntegrationService,
                commandService);

            string onlineBusyStatus = string.Format(STATUS_ONLINE, config.Model.ModelName, TextManager.IsHubMode ? STATUS_HUB_ROUTING : STATUS_CONSOLE_HINT);
            TextManager.Status($"\n{onlineBusyStatus}");

            // Print voice status if enabled
            if (ttsEnabled)
            {
                string pitchLabel = config.Speech.PiperPitchSemitones > 0
                    ? $"+{config.Speech.PiperPitchSemitones}"
                    : config.Speech.PiperPitchSemitones.ToString();
                TextManager.Status($"TTS: piper amy, pitch {pitchLabel} st");
            }

            if (sttEnabled)
            {
                string stHint = TextManager.IsHubMode
                    ? "hub can send audio"
                    : "console: type /talk to record";
                TextManager.Status($"STT: whisper  |  {stHint}");
            }

            List<string> conversationLog = new List<string>();
            bool running = true;

            // Run chat loop and API host concurrently
            Task chatLoopTask = RunChatLoopAsync(
                running,
                basePath,
                userContextService,
                alissa,
                commandService,
                conversationLog);

            // Wait for both tasks (but the API host runs indefinitely until shutdown)
            Task completedTask = await Task.WhenAny(chatLoopTask, apiHostTask);
            await chatLoopTask;

            await FinalizeSession(
                conversationLog,
                alissa,
                config,
                basePath,
                chatClient,
                promptBuilder,
                mediumTermMemoryService);
        }

        private static async Task RunChatLoopAsync(
            bool running,
            string basePath,
            IUserContextService userContextService,
            AlissaClient alissa,
            ICommandService commandService,
            List<string> conversationLog)
        {
            bool isRunning = true;

            while (isRunning)
            {
                if (!TextManager.IsHubMode)
                {
                    TextManager.NextLine();
                    TextManager.PrintText(PROMPT_INPUT);
                }

                string? userInput = await TextManager.ReadInputAsync();

                // In Hub mode, handle audio input if present
                bool hasAudioWav = TextManager.IsHubMode && TextManager.LastMessageAudioWav != null;
                if (hasAudioWav)
                {
                    await ProcessHubAudioInput(
                        TextManager.LastMessageAudioWav!,
                        alissa,
                        userContextService,
                        commandService,
                        conversationLog,
                        basePath);
                    continue; // Skip text processing
                }

                // In Hub mode, set manual user if provided
                if (TextManager.IsHubMode && TextManager.LastUserName != null)
                {
                    await userContextService.SetManualUserAsync(TextManager.LastUserName);
                }

                if (!string.IsNullOrWhiteSpace(userInput))
                {
                    bool isExitCommand =
                        !TextManager.IsHubMode &&
                        string.Equals(
                            userInput.Trim(),
                            EXIT_COMMAND,
                            StringComparison.OrdinalIgnoreCase);

                    if (isExitCommand)
                    {
                        isRunning = false;
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
                            commandService,
                            conversationLog,
                            basePath,
                            userContextService);
                    }
                }
            }
        }

        /// <summary>
        /// Process audio input from Hub 317.
        /// Transcribe the WAV, send transcript to Hub, then stream response.
        /// </summary>
        private static async Task ProcessHubAudioInput(
            byte[] wavBytes,
            AlissaClient alissa,
            IUserContextService userContextService,
            ICommandService commandService,
            List<string> conversationLog,
            string basePath)
        {
            try
            {
                bool hasSTT = alissa.SpeechToTextService != null;
                if (!hasSTT)
                {
                    TextManager.Status("[Hub] Audio received but STT is not enabled.");
                    return;
                }

                // Transcribe the audio
                string transcript = await alissa.SpeechToTextService!.TranscribeAsync(wavBytes, "wav");

                bool transcriptEmpty = string.IsNullOrWhiteSpace(transcript);
                if (transcriptEmpty)
                {
                    TextManager.Status("[Hub] Transcription was empty.");
                    return;
                }

                // Send transcript notice back to Hub
                TextManager.PrintText($"You: {transcript}");
                TextManager.NextLine();

                // Process as normal user message
                await ProcessUserMessage(
                    transcript,
                    alissa,
                    commandService,
                    conversationLog,
                    basePath,
                    userContextService);
            }
            catch (Exception ex)
            {
                TextManager.Status($"[Hub Audio] Error: {ex.Message}");
            }
        }

        private static async Task ProcessUserMessage(
            string userInput,
            AlissaClient alissa,
            ICommandService commandService,
            List<string> conversationLog,
            string basePath,
            IUserContextService userContextService)
        {
            try
            {
                bool isCommand = userInput.StartsWith(COMMAND_SLASH_PREFIX, StringComparison.OrdinalIgnoreCase);
                string result = LOG_EMPTY_RESPONSE;

                if (isCommand)
                {
                    // Parse command format: "/command arg1 arg2 ..."
                    string commandContent = userInput.Substring(1);
                    string[] parts = commandContent.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

                    string commandName = parts.Length > 0 ? parts[0].ToLowerInvariant() : LOG_EMPTY_RESPONSE;
                    string commandArg = parts.Length > 1 ? parts[1] : LOG_EMPTY_RESPONSE;

                    Dictionary<string, object> parameters = new Dictionary<string, object>();
                    if (!string.IsNullOrEmpty(commandArg))
                    {
                        parameters[COMMAND_ARG_KEY] = commandArg;
                    }

                    result = await commandService.ExecuteCommandAsync(commandName, parameters);

                    // Special handling for /voice and /talk — feed transcript through StreamAsync for response
                    bool isVoiceCommand = string.Equals(commandName, COMMAND_VOICE, StringComparison.OrdinalIgnoreCase);
                    bool isTalkCommand = string.Equals(commandName, "talk", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(commandName, "mic", StringComparison.OrdinalIgnoreCase);
                    bool isTranscriptResult = !string.IsNullOrWhiteSpace(result) && (isVoiceCommand || isTalkCommand);

                    if (isTranscriptResult)
                    {
                        // Print the transcript
                        TextManager.SendComplete($"You: {result}");
                        conversationLog.Add(CONVERSATION_LOG_USER_PREFIX + result);

                        // Get current user before streaming
                        string currentUser = await userContextService.GetCurrentUserAsync();

                        // Set current user on prompt builder
                        PromptBuilder? promptBuilderTyped = alissa.PromptBuilder as PromptBuilder;
                        if (promptBuilderTyped != null)
                        {
                            promptBuilderTyped.SetCurrentUser(currentUser);
                        }

                        // Stream response through Alissa
                        TextManager.BeginResponse();
                        StringBuilder fullReply = new StringBuilder();

                        await foreach (string token in alissa.StreamAsync(result))
                        {
                            TextManager.PrintToken(token);
                            fullReply.Append(token);
                        }

                        TextManager.EndResponse();
                        conversationLog.Add(CONVERSATION_LOG_ALISSA_PREFIX + fullReply.ToString());
                    }
                    else
                    {
                        // Regular command result
                        TextManager.SendComplete(result);
                        conversationLog.Add(CONVERSATION_LOG_USER_PREFIX + userInput);
                        conversationLog.Add(CONVERSATION_LOG_COMMAND_PREFIX + result);
                    }
                }
                else
                {
                    // Get current user before streaming
                    string currentUser = await userContextService.GetCurrentUserAsync();

                    // Set current user on prompt builder
                    PromptBuilder? promptBuilderTyped = alissa.PromptBuilder as PromptBuilder;
                    if (promptBuilderTyped != null)
                    {
                        promptBuilderTyped.SetCurrentUser(currentUser);
                    }

                    // Regular chat message - stream response
                    TextManager.BeginResponse();

                    StringBuilder fullReply = new StringBuilder();

                    await foreach (string token in alissa.StreamAsync(userInput))
                    {
                        TextManager.PrintToken(token);
                        fullReply.Append(token);
                    }

                    TextManager.EndResponse();

                    conversationLog.Add(CONVERSATION_LOG_USER_PREFIX + userInput);
                    conversationLog.Add(CONVERSATION_LOG_ALISSA_PREFIX + fullReply.ToString());
                }
            }
            catch (HttpRequestException ex)
            {
                ErrorHandler.Handle(ex, basePath, true);
                TextManager.Status(STATUS_OLLAMA_OFFLINE);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, basePath, true);
                TextManager.Status(STATUS_CHAT_ERROR);
            }
        }

        private static async Task FinalizeSession(
            List<string> conversationLog,
            AlissaClient alissa,
            AppConfig config,
            string basePath,
            IChatClient chatClient,
            IPromptBuilder promptBuilder,
            MediumTermMemoryService mediumTermMemoryService)
        {
            try
            {
                int conversationLogCount = conversationLog.Count;
                if (conversationLogCount > 0)
                {
                    await SaveConversation.SaveConversationAsync(
                        conversationLog,
                        alissa,
                        config,
                        basePath,
                        chatClient,
                        promptBuilder,
                        mediumTermMemoryService);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, basePath, true);
            }

            TextManager.Blank();
            TextManager.Status(STATUS_GOODBYE);
        }

        /// <summary>
        /// Pick TTS engine: prompt user if console mode and more than one available.
        /// Hub mode uses config.Speech.TtsEngine or first available.
        /// Returns engine name ("piper", "styletts2") or empty string if none available.
        /// </summary>
        private static string PickTtsEngine(bool piperAvailable, bool styletts2Available, bool hubMode)
        {
            if (!piperAvailable && !styletts2Available)
            {
                return string.Empty;
            }

            if (!hubMode)
            {
                bool multipleEngines = piperAvailable && styletts2Available;
                if (multipleEngines)
                {
                    // Console picker
                    TextManager.Blank();
                    TextManager.SendComplete("TTS engine:");
                    TextManager.SendComplete("  [1] piper");
                    TextManager.SendComplete("  [2] styletts2");
                    TextManager.SendComplete("Choice (Enter = 1): ");

                    string? input = Console.ReadLine();
                    bool isSecond = input != null && input.Trim().Equals("2");
                    if (isSecond && styletts2Available)
                    {
                        return "styletts2";
                    }

                    return "piper";
                }
            }

            // Single engine or hub mode: return first available
            if (piperAvailable)
            {
                return "piper";
            }

            if (styletts2Available)
            {
                return "styletts2";
            }

            return string.Empty;
        }

        /// <summary>
        /// Initialize StyleTTS2 sidecar and service. Returns null on failure; caller falls back to Piper.
        /// </summary>
        private static async Task<ITextToSpeechService?> InitializeStyleTts2Async(
            AppConfig config,
            string basePath,
            bool piperAvailable,
            IProcessRunner processRunner)
        {
            try
            {
                StyleTts2SidecarHost sidecarHost = new StyleTts2SidecarHost(config.Speech, basePath);

                bool autoStart = config.Speech.StyleTts2AutoStart;
                if (autoStart)
                {
                    bool started = await sidecarHost.StartAsync(msg => TextManager.Status(msg));
                    if (!started)
                    {
                        TextManager.Status("[StyleTTS2] Sidecar failed to start, falling back to Piper");
                        sidecarHost.Dispose();
                        return null;
                    }
                }

                // Create service
                HttpClient httpClient = new HttpClient();
                StyleTts2TextToSpeechService service = new StyleTts2TextToSpeechService(config.Speech, httpClient);

                TextManager.Status("TTS: styletts2");
                return service;
            }
            catch (Exception ex)
            {
                TextManager.Status($"[StyleTTS2] Initialization failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Initialize Piper TTS service.
        /// </summary>
        private static ITextToSpeechService? InitializePiper(IProcessRunner processRunner, AppConfig config)
        {
            try
            {
                return new PiperTextToSpeechService(processRunner, config.Speech);
            }
            catch (Exception ex)
            {
                TextManager.Status($"[TTS Error] Piper initialization failed: {ex.Message}");
                return null;
            }
        }
    }
}