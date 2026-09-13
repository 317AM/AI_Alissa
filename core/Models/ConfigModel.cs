namespace Alissa.Core.Models
{
    public class ConfigModel
    {
        public string ModelName { get; set; } = null!;
        public int KeepAliveMinutes { get; set; }
        public int MaxTokens { get; set; }
        public int ResponseTimeoutSeconds { get; set; }
        public double Temperature { get; set; } = 0.7;
        public bool EnableThinking { get; set; } = true;

        /// <summary>
        /// Speech-to-text model configuration.
        /// </summary>
        public SttModelConfig Stt { get; set; } = new();

        /// <summary>
        /// Text-to-speech model configuration.
        /// </summary>
        public TtsModelConfig Tts { get; set; } = new();
    }

    public class SttModelConfig
    {
        public string Engine { get; set; } = "whisper";
        public string WhisperModelPath { get; set; } = string.Empty;

        public void ResolveAgainst(string basePath)
        {
            bool basePathValid = !string.IsNullOrEmpty(basePath);
            if (!basePathValid)
            {
                return;
            }

            WhisperModelPath = ResolvePath(WhisperModelPath, basePath);
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

    public class TtsModelConfig
    {
        public string Engine { get; set; } = "piper";
        public string PiperVoiceModelPath { get; set; } = string.Empty;
        public string StyleTts2CheckpointPath { get; set; } = string.Empty;
        public string StyleTts2ConfigPath { get; set; } = string.Empty;

        public void ResolveAgainst(string basePath)
        {
            bool basePathValid = !string.IsNullOrEmpty(basePath);
            if (!basePathValid)
            {
                return;
            }

            PiperVoiceModelPath = ResolvePath(PiperVoiceModelPath, basePath);
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

