using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Utils;

namespace Alissa.Core.Services
{
    /// <summary>
    /// StyleTTS2 TTS service that calls the HTTP sidecar at StyleTts2BaseUrl.
    /// Synthesizes text via POST /synthesize, applies pitch shifting, and returns WAV bytes.
    /// </summary>
    public class StyleTts2TextToSpeechService : ITextToSpeechService
    {
        private readonly SpeechConfig _config;
        private readonly HttpClient _httpClient;

        public StyleTts2TextToSpeechService(SpeechConfig config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public async Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default)
        {
            try
            {
                string cleanText = StripForSpeech(text);

                if (string.IsNullOrWhiteSpace(cleanText))
                {
                    return Array.Empty<byte>();
                }

                // POST to /synthesize
                var request = new
                {
                    text = cleanText,
                    speed = 1.2,
                    alpha = 0.3,
                    beta = 0.7,
                    diffusion_steps = 8,
                    embedding_scale = 1.2
                };

                string jsonRequest = JsonSerializer.Serialize(request);
                HttpContent content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                string synthesizeUrl = $"{_config.StyleTts2BaseUrl}/synthesize";
                HttpResponseMessage response = await _httpClient.PostAsync(synthesizeUrl, content, ct);

                response.EnsureSuccessStatusCode();

                byte[] wavBytes = await response.Content.ReadAsByteArrayAsync(ct);

                // Apply pitch shift
                byte[] pitchedWav = WavPitchShifter.ShiftPitch(wavBytes, _config.PiperPitchSemitones);

                return pitchedWav;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"StyleTTS2 sidecar error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"StyleTTS2 synthesis failed: {ex.Message}", ex);
            }
        }

        public async Task SynthesizeToFileAsync(string text, string outputPath, CancellationToken ct = default)
        {
            byte[] wavBytes = await SynthesizeAsync(text, ct);
            await File.WriteAllBytesAsync(outputPath, wavBytes, ct);
        }

        /// <summary>
        /// Strip text for speech: remove URLs, clean up whitespace.
        /// </summary>
        private string StripForSpeech(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            // Remove URLs
            text = System.Text.RegularExpressions.Regex.Replace(text, @"https?://\S+", string.Empty);

            // Clean up whitespace
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            text = text.Trim();

            return text;
        }
    }
}
