using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Utils;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Text-to-speech service using Piper binary.
    /// Converts text to audio bytes (WAV format).
    /// Strips markdown, code blocks, and emoji before synthesis.
    /// </summary>
    public class PiperTextToSpeechService : ITextToSpeechService
    {
        private readonly IProcessRunner _processRunner;
        private readonly SpeechConfig _config;

        public PiperTextToSpeechService(IProcessRunner processRunner, SpeechConfig config)
        {
            bool invalidRunner = processRunner == null;
            {
                if (invalidRunner)
                {
                    throw new ArgumentNullException(nameof(processRunner));
                }
            }

            bool invalidConfig = config == null;
            {
                if (invalidConfig)
                {
                    throw new ArgumentNullException(nameof(config));
                }
            }

            _processRunner = processRunner;
            _config = config;
        }

        /// <summary>
        /// Synthesizes audio from text and returns it as byte array.
        /// </summary>
        public async Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
        {
            bool textEmpty = string.IsNullOrWhiteSpace(text);
            {
                if (textEmpty)
                {
                    return Array.Empty<byte>();
                }
            }

            string cleanText = TextSanitizer.StripForSpeech(text);

            bool cleanTextEmpty = string.IsNullOrWhiteSpace(cleanText);
            {
                if (cleanTextEmpty)
                {
                    return Array.Empty<byte>();
                }
            }

            byte[] result = await SynthesizeInternalAsync(cleanText, cancellationToken).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Synthesizes audio from text and writes it to a file.
        /// </summary>
        public async Task SynthesizeToFileAsync(string text, string outputPath, CancellationToken cancellationToken = default)
        {
            bool textEmpty = string.IsNullOrWhiteSpace(text);
            {
                if (textEmpty)
                {
                    return;
                }
            }

            bool outputPathEmpty = string.IsNullOrWhiteSpace(outputPath);
            {
                if (outputPathEmpty)
                {
                    throw new ArgumentException("Output path cannot be empty", nameof(outputPath));
                }
            }

            byte[] audioData = await SynthesizeAsync(text, cancellationToken).ConfigureAwait(false);

            bool audioDataEmpty = audioData.Length == 0;
            {
                if (!audioDataEmpty)
                {
                    string directory = Path.GetDirectoryName(outputPath);
                    bool dirEmpty = string.IsNullOrEmpty(directory);
                    {
                        if (!dirEmpty)
                        {
                            Directory.CreateDirectory(directory);
                        }
                    }

                    await File.WriteAllBytesAsync(outputPath, audioData, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<byte[]> SynthesizeInternalAsync(string cleanText, CancellationToken cancellationToken)
        {
            bool binaryPathEmpty = string.IsNullOrWhiteSpace(_config.PiperBinaryPath);
            {
                if (binaryPathEmpty)
                {
                    throw new InvalidOperationException("Piper binary path is not configured");
                }
            }

            bool modelPathEmpty = string.IsNullOrWhiteSpace(_config.PiperVoiceModelPath);
            {
                if (modelPathEmpty)
                {
                    throw new InvalidOperationException("Piper voice model path is not configured");
                }
            }

            bool binaryExists = File.Exists(_config.PiperBinaryPath);
            {
                if (!binaryExists)
                {
                    throw new FileNotFoundException($"Piper binary not found: {_config.PiperBinaryPath}");
                }
            }

            bool modelExists = File.Exists(_config.PiperVoiceModelPath);
            {
                if (!modelExists)
                {
                    throw new FileNotFoundException($"Piper model not found: {_config.PiperVoiceModelPath}");
                }
            }

            // Create temp directory if needed
            bool tempDirEmpty = string.IsNullOrWhiteSpace(_config.TempDirectory);
            {
                if (!tempDirEmpty)
                {
                    Directory.CreateDirectory(_config.TempDirectory);
                }
            }

            string tempOutputFile = Path.Combine(_config.TempDirectory, $"piper_{Guid.NewGuid()}.wav");
            byte[] result = Array.Empty<byte>();

            try
            {
                // Piper CLI on Windows: piper.exe --model "<onnx>" --output_file "<temp.wav>" --length_scale <scale>
                // Text goes to stdin as UTF-8
                // Never use /dev/stdin or -t flag on Windows
                string quotedModelPath = $"\"{_config.PiperVoiceModelPath}\"";
                string quotedOutputPath = $"\"{tempOutputFile}\"";
                string arguments = $"--model {quotedModelPath} --output_file {quotedOutputPath} --length_scale {_config.PiperLengthScale}";

                byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(cleanText);

                int timeoutMs = _config.ProcessTimeoutSeconds * 1000;
                (byte[] output, int exitCode) = await _processRunner.RunBinaryAsync(
                    _config.PiperBinaryPath,
                    arguments,
                    textBytes,
                    timeoutMs,
                    cancellationToken).ConfigureAwait(false);

                bool exitCodeSuccess = exitCode == 0;
                {
                    if (!exitCodeSuccess)
                    {
                        throw new InvalidOperationException($"Piper process failed with exit code {exitCode}");
                    }
                }

                // Read the synthesized WAV from temp file
                bool tempFileExists = File.Exists(tempOutputFile);
                {
                    if (tempFileExists)
                    {
                        result = await File.ReadAllBytesAsync(tempOutputFile, cancellationToken).ConfigureAwait(false);
                    }
                }

                // Apply pitch shifting if configured
                bool hasPitchShift = Math.Abs(_config.PiperPitchSemitones) > 0.001;
                if (hasPitchShift)
                {
                    result = WavPitchShifter.ShiftPitch(result, _config.PiperPitchSemitones);
                }
            }
            finally
            {
                // Clean up temp file
                bool tempFileExists = File.Exists(tempOutputFile);
                {
                    if (tempFileExists)
                    {
                        try
                        {
                            File.Delete(tempOutputFile);
                        }
                        catch
                        {
                            // Cleanup failure is not critical
                        }
                    }
                }
            }

            return result;
        }
    }
}
