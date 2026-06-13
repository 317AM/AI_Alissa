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
    public class OllamaClient : IChatClient
    {
        private readonly string _modelName;
        private readonly HttpClient _httpClient;
        private readonly int _keepAliveMinutes;

        public OllamaClient(string modelName, int keepAliveMinutes)
        {
            _modelName = modelName;
            _keepAliveMinutes = keepAliveMinutes;
            _httpClient = new HttpClient
            {
                BaseAddress = new System.Uri("http://localhost:11434")
            };
        }

        public OllamaClient(string modelName, string basePath)
        {
            _modelName = modelName;
            var appConfig = ConfigService.LoadAll(basePath);
            _keepAliveMinutes = appConfig.Model.KeepAliveMinutes;
            _httpClient = new HttpClient
            {
                BaseAddress = new System.Uri("http://localhost:11434")
            };
        }

        public async IAsyncEnumerable<string> StreamAsync(
            string systemPrompt,
            string userInput,
            Action<string>? onEmoji = null)
        {
            string prompt = BuildPrompt(systemPrompt, userInput);
            object payload = CreatePayload(prompt);
            string jsonPayload = JsonSerializer.Serialize(payload);

            using (StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, "/api/generate") { Content = content })
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
                                        var (text, isDone) = ProcessStreamLine(line, onEmoji);

                                        if (!string.IsNullOrEmpty(text))
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
            string prompt = systemPrompt + "\n" + "User: " + userInput + "\n" + "Alissa:";
            return prompt;
        }

        private object CreatePayload(string prompt)
        {
            return new
            {
                model = _modelName,
                prompt = prompt,
                stream = true,
                keep_alive = $"{_keepAliveMinutes}m"
            };
        }

        private (string text, bool isDone) ProcessStreamLine(string line, Action<string>? onEmoji)
        {
            string resultText = string.Empty;
            bool resultIsDone = false;

            try
            {
                using (JsonDocument document = JsonDocument.Parse(line))
                {
                    bool hasResponse = document.RootElement.TryGetProperty("response", out JsonElement token);

                    if (hasResponse)
                    {
                        string text = token.GetString() ?? string.Empty;
                        EmojiUtils.ExtractEmojis(text, out string cleaned, out string emojis);

                        if (!string.IsNullOrEmpty(emojis))
                        {
                            onEmoji?.Invoke(emojis);
                        }

                        resultText = cleaned;
                    }

                    bool hasDone = document.RootElement.TryGetProperty("done", out JsonElement doneEl);

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

            return (resultText, resultIsDone);
        }
        public IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userInput)
        {
            return StreamAsync(systemPrompt, userInput, null);
        }

    }
}
