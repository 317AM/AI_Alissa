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
            string prompt =
                systemPrompt + "\n" +
                "User: " + userInput + "\n" +
                "Alissa:";

            object payload = new
            {
                model = _modelName,
                prompt = prompt,
                stream = true,
                keep_alive = $"{_keepAliveMinutes}m"
            };

            string jsonPayload = JsonSerializer.Serialize(payload);
            using StringContent content =
                new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/generate")
            {
                Content = content
            };

            using HttpResponseMessage response =
                await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            using Stream stream = await response.Content.ReadAsStreamAsync();
            using StreamReader reader = new StreamReader(stream);

            bool done = false;
            string? line = await reader.ReadLineAsync();

            while (line != null && !done)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    using JsonDocument document = JsonDocument.Parse(line);

                    if (document.RootElement.TryGetProperty("response", out JsonElement token))
                    {
                        string text = token.GetString() ?? string.Empty;

                        EmojiUtils.ExtractEmojis(text, out string cleaned, out string emojis);

                        if (!string.IsNullOrEmpty(emojis))
                        {
                            onEmoji?.Invoke(emojis);
                        }

                        if (!string.IsNullOrEmpty(cleaned))
                        {
                            yield return cleaned;
                        }
                    }

                    if (document.RootElement.TryGetProperty("done", out JsonElement doneEl)
                        && doneEl.GetBoolean())
                    {
                        done = true;
                    }
                }
                line = await reader.ReadLineAsync();
            }
        }
        public IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userInput)
        {
            return StreamAsync(systemPrompt, userInput, null);
        }

    }
}
