using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Speech-to-text service using whisper.cpp binary.
    /// Transcribes audio files (and byte arrays converted to temp files) to text.
    /// </summary>
    public class WhisperSpeechToTextService : ISpeechToTextService
    {
        private readonly IProcessRunner _processRunner;
        private readonly SpeechConfig _config;

        public WhisperSpeechToTextService(IProcessRunner processRunner, SpeechConfig config)
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
        /// Transcribes audio from a file path.
        /// </summary>
        public async Task<string> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default)
        {
            bool filePathEmpty = string.IsNullOrWhiteSpace(audioFilePath);
            {
                if (filePathEmpty)
                {
                    throw new ArgumentException("Audio file path cannot be empty", nameof(audioFilePath));
                }
            }

            bool fileExists = File.Exists(audioFilePath);
            {
                if (!fileExists)
                {
                    throw new FileNotFoundException($"Audio file not found: {audioFilePath}");
                }
            }

            string result = await TranscribeFileInternalAsync(audioFilePath, cancellationToken).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Transcribes audio from a byte array (WAV format only).
        /// </summary>
        public async Task<string> TranscribeAsync(byte[] audioBytes, string format, CancellationToken cancellationToken = default)
        {
            bool audioNull = audioBytes == null;
            {
                if (audioNull)
                {
                    throw new ArgumentNullException(nameof(audioBytes));
                }
            }

            bool formatEmpty = string.IsNullOrWhiteSpace(format);
            {
                if (formatEmpty)
                {
                    throw new ArgumentException("Format cannot be empty", nameof(format));
                }
            }

            bool formatSupported = format.Equals("wav", StringComparison.OrdinalIgnoreCase);
            {
                if (!formatSupported)
                {
                    throw new NotSupportedException($"Audio format '{format}' is not supported. Only 'wav' is supported.");
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

            string tempFile = Path.Combine(_config.TempDirectory, $"temp_{Guid.NewGuid()}.wav");

            bool tempFileSuccess = false;
            {
                try
                {
                    await File.WriteAllBytesAsync(tempFile, audioBytes, cancellationToken).ConfigureAwait(false);
                    tempFileSuccess = true;

                    string result = await TranscribeFileInternalAsync(tempFile, cancellationToken).ConfigureAwait(false);
                    return result;
                }
                finally
                {
                    bool shouldCleanup = tempFileSuccess && File.Exists(tempFile);
                    {
                        if (shouldCleanup)
                        {
                            try
                            {
                                File.Delete(tempFile);
                            }
                            catch
                            {
                                // Cleanup failure is not critical
                            }
                        }
                    }
                }
            }
        }

        private async Task<string> TranscribeFileInternalAsync(string audioFilePath, CancellationToken cancellationToken)
        {
            bool binaryPathEmpty = string.IsNullOrWhiteSpace(_config.WhisperBinaryPath);
            {
                if (binaryPathEmpty)
                {
                    throw new InvalidOperationException("Whisper binary path is not configured");
                }
            }

            bool modelPathEmpty = string.IsNullOrWhiteSpace(_config.WhisperModelPath);
            {
                if (modelPathEmpty)
                {
                    throw new InvalidOperationException("Whisper model path is not configured");
                }
            }

            bool binaryExists = File.Exists(_config.WhisperBinaryPath);
            {
                if (!binaryExists)
                {
                    throw new FileNotFoundException($"Whisper binary not found: {_config.WhisperBinaryPath}");
                }
            }

            bool modelExists = File.Exists(_config.WhisperModelPath);
            {
                if (!modelExists)
                {
                    throw new FileNotFoundException($"Whisper model not found: {_config.WhisperModelPath}");
                }
            }

            // Quote paths for Windows
            string quotedAudioPath = $"\"{audioFilePath}\"";
            string quotedModelPath = $"\"{_config.WhisperModelPath}\"";
            string languageArg = $" -l {_config.Language}";
            // -nt suppresses timestamps; do not use -otxt (that writes a .txt sidecar file)
            string arguments = $"-m {quotedModelPath} -f {quotedAudioPath}{languageArg} -nt";

            int timeoutMs = _config.ProcessTimeoutSeconds * 1000;
            (string output, int exitCode) = await _processRunner.RunAsync(
                _config.WhisperBinaryPath,
                arguments,
                null,
                timeoutMs,
                cancellationToken).ConfigureAwait(false);

            bool exitCodeSuccess = exitCode == 0;
            {
                if (!exitCodeSuccess)
                {
                    throw new InvalidOperationException($"Whisper process failed with exit code {exitCode}. Output: {output}");
                }
            }

            // Parse the transcription from output
            string transcription = ParseTranscription(output);
            return transcription;
        }

        private static string ParseTranscription(string output)
        {
            bool outputEmpty = string.IsNullOrWhiteSpace(output);
            {
                if (outputEmpty)
                {
                    return string.Empty;
                }
            }

            // With -nt flag, Whisper outputs the transcription without timestamps
            // Filter out metadata lines and join actual content
            string[] lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            string result = string.Empty;
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string trimmedLine = line.Trim();
                    bool lineEmpty = string.IsNullOrEmpty(trimmedLine);
                    bool isMetadata = trimmedLine.StartsWith("[") || trimmedLine.StartsWith("Warning");

                    bool shouldAdd = !lineEmpty && !isMetadata;
                    {
                        if (shouldAdd)
                        {
                            bool resultAlreadySet = !string.IsNullOrEmpty(result);
                            {
                                if (resultAlreadySet)
                                {
                                    result += " ";
                                }
                            }
                            result += trimmedLine;
                        }
                    }
                }
            }

            return result.Trim();
        }
    }
}
