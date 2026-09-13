using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Utils;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Alissa.Core.Services
{
    public class OllamaClient : IChatClient, IThinkingCapable
    {
        // Constants
        private const string OLLAMA_BASE_URL = "http://localhost:11434";
        private const string API_GENERATE_ENDPOINT = "/api/generate";
        private const string JSON_MEDIA_TYPE = "application/json";
        private const string NEWLINE = "\n";
        private const string USER_PREFIX = "User: ";
        private const string ALISSA_PREFIX = "Alissa:";
        private const string RESPONSE_KEY = "response";
        private const string THINKING_KEY = "thinking";
        private const string DONE_KEY = "done";
        private const string KEEP_ALIVE_FORMAT = "{0}m";
        private const int DEFAULT_KEEP_ALIVE_MINUTES = 5;
        private const string THINK_TAG_OPEN = "<think>";
        private const string THINK_TAG_CLOSE = "</think>";

        private readonly string _modelName;
        private readonly HttpClient _httpClient;
        private readonly int _keepAliveMinutes;
        private readonly int _maxTokens;
        private readonly double _temperature;
        private readonly bool _enableThinking;
        private string _lastThinkingText = string.Empty;

        public OllamaClient(string modelName, int keepAliveMinutes)
        {
            _modelName = modelName;
            _keepAliveMinutes = keepAliveMinutes;
            _maxTokens = 4096;
            _temperature = 0.7;
            _enableThinking = true;
            Uri baseUri = new Uri(OLLAMA_BASE_URL);
            _httpClient = new HttpClient
            {
                BaseAddress = baseUri
            };
        }

        public OllamaClient(string modelName, string basePath)
        {
            _modelName = modelName;
            AppConfig appConfig = ConfigService.LoadAll(basePath);
            _keepAliveMinutes = appConfig.Model.KeepAliveMinutes;
            _maxTokens = appConfig.Model.MaxTokens;
            _temperature = appConfig.Model.Temperature;
            _enableThinking = appConfig.Model.EnableThinking;
            Uri baseUri = new Uri(OLLAMA_BASE_URL);
            _httpClient = new HttpClient
            {
                BaseAddress = baseUri
            };
        }

        public OllamaClient(string modelName, int keepAliveMinutes, int maxTokens, double temperature, bool enableThinking)
        {
            _modelName = modelName;
            _keepAliveMinutes = keepAliveMinutes;
            _maxTokens = maxTokens;
            _temperature = temperature;
            _enableThinking = enableThinking;
            Uri baseUri = new Uri(OLLAMA_BASE_URL);
            _httpClient = new HttpClient
            {
                BaseAddress = baseUri
            };
        }

        public async IAsyncEnumerable<string> StreamAsync(
            string systemPrompt,
            string userInput,
            Action<string>? onEmoji = null)
        {
            _lastThinkingText = string.Empty;

            string prompt = BuildPrompt(systemPrompt, userInput);
            object payload = CreatePayload(prompt);
            string jsonPayload = JsonSerializer.Serialize(payload);

            using (StringContent content = new StringContent(jsonPayload, Encoding.UTF8, JSON_MEDIA_TYPE))
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, API_GENERATE_ENDPOINT) { Content = content })
                {
                    using (HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        using (Stream stream = await response.Content.ReadAsStreamAsync())
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                bool isProcessing = true;

                                while (isProcessing)
                                {
                                    string? line = await reader.ReadLineAsync();
                                    bool hasLine = !string.IsNullOrWhiteSpace(line);

                                    if (hasLine)
                                    {
                                        (string text, string thinking, bool isDone) = ProcessStreamLine(line, onEmoji);

                                        bool hasThinking = !string.IsNullOrEmpty(thinking);
                                        if (hasThinking)
                                        {
                                            _lastThinkingText += thinking;
                                        }

                                        bool hasText = !string.IsNullOrEmpty(text);
                                        if (hasText)
                                        {
                                            yield return text;
                                        }

                                        if (isDone)
                                        {
                                            isProcessing = false;
                                        }
                                    }
                                    else
                                    {
                                        isProcessing = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private string BuildPrompt(string systemPrompt, string userInput)
        {
            string prompt = systemPrompt + NEWLINE + USER_PREFIX + userInput + NEWLINE + ALISSA_PREFIX;
            return prompt;
        }

        private object CreatePayload(string prompt)
        {
            string keepAlive = string.Format(KEEP_ALIVE_FORMAT, _keepAliveMinutes);

            object options = new
            {
                num_predict = _maxTokens,
                temperature = _temperature
            };

            object thinkOptions = _enableThinking 
                ? new { num_predict = _maxTokens, temperature = _temperature, think = true }
                : (object)new { num_predict = _maxTokens, temperature = _temperature };

            object payload = new
            {
                model = _modelName,
                prompt = prompt,
                stream = true,
                keep_alive = keepAlive,
                options = thinkOptions
            };

            return payload;
        }

        private (string text, string thinking, bool isDone) ProcessStreamLine(string line, Action<string>? onEmoji)
        {
            string resultText = string.Empty;
            string resultThinking = string.Empty;
            bool resultIsDone = false;

            try
            {
                using (JsonDocument document = JsonDocument.Parse(line))
                {
                    // Check for native thinking field first (Task 2a)
                    bool hasNativeThinking = document.RootElement.TryGetProperty(THINKING_KEY, out JsonElement thinkingEl);
                    if (hasNativeThinking)
                    {
                        string nativeThinking = thinkingEl.GetString() ?? string.Empty;
                        bool thinkingIsNonEmpty = !string.IsNullOrEmpty(nativeThinking);
                        if (thinkingIsNonEmpty)
                        {
                            resultThinking = nativeThinking;
                        }
                    }

                    // Process response field (Task 2c)
                    bool hasResponse = document.RootElement.TryGetProperty(RESPONSE_KEY, out JsonElement token);
                    if (hasResponse)
                    {
                        string text = token.GetString() ?? string.Empty;

                        // Task 2b: Strip inline think tags
                        (string cleanedResponse, string inlineThinking) = StripThinkTags(text);

                        bool hasInlineThinking = !string.IsNullOrEmpty(inlineThinking);
                        if (hasInlineThinking && string.IsNullOrEmpty(resultThinking))
                        {
                            resultThinking = inlineThinking;
                        }

                        EmojiUtils.ExtractEmojis(cleanedResponse, out string cleaned, out string emojis);

                        bool hasEmojis = !string.IsNullOrEmpty(emojis);
                        if (hasEmojis)
                        {
                            onEmoji?.Invoke(emojis);
                        }

                        resultText = cleaned;
                    }

                    // Check for done flag
                    bool hasDone = document.RootElement.TryGetProperty(DONE_KEY, out JsonElement doneEl);
                    if (hasDone)
                    {
                        resultIsDone = doneEl.GetBoolean();
                    }
                }
            }
            catch
            {
                // Silently handle JSON parsing errors
            }

            return (resultText, resultThinking, resultIsDone);
        }

        /// <summary>
        /// Strips inline think tags from response text.
        /// Returns cleaned response and extracted thinking content.
        /// </summary>
        private (string cleanedResponse, string thinkingContent) StripThinkTags(string text)
        {
            string cleanedResponse = text;
            string thinkingContent = string.Empty;

            bool hasThinkTags = text.Contains(THINK_TAG_OPEN, System.StringComparison.OrdinalIgnoreCase);
            {
                if (hasThinkTags)
                {
                    int openIndex = text.IndexOf(THINK_TAG_OPEN, System.StringComparison.OrdinalIgnoreCase);
                    int closeIndex = text.IndexOf(THINK_TAG_CLOSE, System.StringComparison.OrdinalIgnoreCase);

                    bool indicesAreValid = openIndex >= 0 && closeIndex > openIndex;
                    {
                        if (indicesAreValid)
                        {
                            int contentStart = openIndex + THINK_TAG_OPEN.Length;
                            int contentLength = closeIndex - contentStart;
                            thinkingContent = text.Substring(contentStart, contentLength).Trim();

                            string before = text.Substring(0, openIndex);
                            int afterStart = closeIndex + THINK_TAG_CLOSE.Length;
                            string after = afterStart < text.Length ? text.Substring(afterStart) : string.Empty;

                            cleanedResponse = (before + after).Trim();
                        }
                    }
                }
            }

            return (cleanedResponse, thinkingContent);
        }

        public IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userInput)
        {
            return StreamAsync(systemPrompt, userInput, null);
        }

        /// <summary>
        /// Gets the last captured thinking/reasoning text from the model.
        /// </summary>
        public string LastThinkingText 
        { 
            get { return _lastThinkingText; }
        }

        /// <summary>
        /// Clears the captured thinking buffer for the next exchange.
        /// </summary>
        public void ClearThinkingBuffer()
        {
            _lastThinkingText = string.Empty;
        }
    }
}

