using System;

namespace Alissa.Core.Models
{
    /// <summary>
    /// Configuration for speech-to-text and text-to-speech services.
    /// </summary>
    public class SpeechConfig
    {
        /// <summary>
        /// Enable speech-to-text (whisper.cpp).
        /// </summary>
        public bool SttEnabled { get; set; } = false;

        /// <summary>
        /// Path to whisper.cpp binary.
        /// </summary>
        public string WhisperBinaryPath { get; set; } = string.Empty;

        /// <summary>
        /// Path to whisper.cpp model file (GGML .bin format).
        /// </summary>
        public string WhisperModelPath { get; set; } = string.Empty;

        /// <summary>
        /// Language for speech recognition (e.g., "en", "de", "fr").
        /// </summary>
        public string Language { get; set; } = "en";

        /// <summary>
        /// Enable text-to-speech (Piper).
        /// </summary>
        public bool TtsEnabled { get; set; } = false;

        /// <summary>
        /// Text-to-speech engine name (e.g., "piper", "styletts2").
        /// </summary>
        public string TtsEngine { get; set; } = "piper";

        /// <summary>
        /// Path to piper binary.
        /// </summary>
        public string PiperBinaryPath { get; set; } = string.Empty;

        /// <summary>
        /// Path to piper voice model (.onnx).
        /// </summary>
        public string PiperVoiceModelPath { get; set; } = string.Empty;

        /// <summary>
        /// Piper length scale for speed adjustment (0.5 = fast, 1.0 = normal, 2.0 = slow).
        /// </summary>
        public double PiperLengthScale { get; set; } = 0.75;

        /// <summary>
        /// Pitch adjustment in semitones (positive = higher, negative = lower, 0 = unchanged).
        /// </summary>
        public double PiperPitchSemitones { get; set; } = 3.5;

        /// <summary>
        /// Audio output sample format (e.g., "wav").
        /// </summary>
        public string OutputSampleFormat { get; set; } = "wav";

        /// <summary>
        /// Temporary directory for audio files during processing.
        /// </summary>
        public string TempDirectory { get; set; } = "temp/audio";

        /// <summary>
        /// Process execution timeout in seconds.
        /// </summary>
        public int ProcessTimeoutSeconds { get; set; } = 120;

        /// <summary>
        /// Path to StyleTTS2 python.exe (outside repo, absolute path OK).
        /// </summary>
        public string StyleTts2PythonExe { get; set; } = string.Empty;

        /// <summary>
        /// Path to StyleTTS2 server.py script (relative to repo root).
        /// </summary>
        public string StyleTts2ServerScript { get; set; } = string.Empty;

        /// <summary>
        /// Path to StyleTTS2 repo clone (contains models.py, etc.).
        /// </summary>
        public string StyleTts2RepoPath { get; set; } = string.Empty;

        /// <summary>
        /// Path to StyleTTS2 checkpoint (.pth file).
        /// </summary>
        public string StyleTts2CheckpointPath { get; set; } = string.Empty;

        /// <summary>
        /// Path to StyleTTS2 config.yml.
        /// </summary>
        public string StyleTts2ConfigPath { get; set; } = string.Empty;

        /// <summary>
        /// Base URL for StyleTTS2 HTTP sidecar.
        /// </summary>
        public string StyleTts2BaseUrl { get; set; } = "http://127.0.0.1:8020";

        /// <summary>
        /// Automatically start StyleTTS2 sidecar if files exist.
        /// </summary>
        public bool StyleTts2AutoStart { get; set; } = true;

        /// <summary>
        /// Startup timeout in seconds for StyleTTS2 sidecar health check.
        /// </summary>
        public int StyleTts2StartupTimeoutSeconds { get; set; } = 180;

        /// <summary>
        /// Path to espeak-ng library for StyleTTS2 phonemizer.
        /// </summary>
        public string EspeakLibraryPath { get; set; } = "C:\\Program Files\\eSpeak NG\\libespeak-ng.dll";

        /// <summary>
        /// Resolves relative paths against a base directory.
        /// If a path is null/empty or already absolute, it is left unchanged.
        /// </summary>
        public void ResolveAgainst(string basePath)
        {
            bool basePathValid = !string.IsNullOrEmpty(basePath);
            if (!basePathValid)
            {
                return;
            }

            WhisperBinaryPath = ResolvePath(WhisperBinaryPath, basePath);
            WhisperModelPath = ResolvePath(WhisperModelPath, basePath);
            PiperBinaryPath = ResolvePath(PiperBinaryPath, basePath);
            PiperVoiceModelPath = ResolvePath(PiperVoiceModelPath, basePath);
            TempDirectory = ResolvePath(TempDirectory, basePath);
            StyleTts2PythonExe = ResolvePath(StyleTts2PythonExe, basePath);
            StyleTts2ServerScript = ResolvePath(StyleTts2ServerScript, basePath);
            StyleTts2RepoPath = ResolvePath(StyleTts2RepoPath, basePath);
            StyleTts2CheckpointPath = ResolvePath(StyleTts2CheckpointPath, basePath);
            StyleTts2ConfigPath = ResolvePath(StyleTts2ConfigPath, basePath);
        }

        private string ResolvePath(string path, string basePath)
        {
            bool pathIsEmpty = string.IsNullOrEmpty(path);
            if (pathIsEmpty)
            {
                return path;
            }

            bool pathIsRooted = Path.IsPathRooted(path);
            if (pathIsRooted)
            {
                return path;
            }

            string resolved = Path.GetFullPath(Path.Combine(basePath, path));
            return resolved;
        }
    }
}


