using Alissa.Core.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Service for analyzing video files and extracting frame descriptions.
    /// Uses ffmpeg to sample frames and a vision model (via Ollama) to describe them.
    /// </summary>
    public class VideoAnalysisService : IVideoAnalysisService
    {
        // Constants
        private const string TEMP_FRAMES_DIR = "temp";
        private const string FRAMES_SUBDIR = "frames";
        private const string VIDEO_PREFIX = "video_";
        private const string TEMP_EXTENSION = ".tmp";
        private const string FRAME_EXTENSION = ".jpg";
        private const string FRAME_PATTERN_FORMAT = "frame_{0}.jpg";
        private const string FFMPEG_EXECUTABLE = "ffmpeg";
        private const string FFPROBE_EXECUTABLE = "ffprobe";
        private const string VERSION_ARG = "-version";
        private const string NO_FRAMES_MSG = "No frames could be extracted from the video.";
        private const string FRAME_LABEL_FORMAT = "Frame {0}/{1}";
        private const string VISION_PROMPT = "Describe what is on screen in this frame. Focus on: open applications, visible text, code, errors, or work context. Be concise (2-3 sentences).";
        private const string VISION_CONTEXT_SUFFIX = " Context: ";
        private const string NO_RESPONSE_MSG = "No response from vision model";
        private const string FRAME_ANALYSIS_ERROR = "Frame analysis error: ";
        private const string VIDEO_ANALYSIS_ERROR = "Video analysis error: ";
        private const string FFMPEG_NOT_FOUND = "ffmpeg not found in PATH";
        private const string FFMPEG_CHECK_FAILED = "ffmpeg check failed";
        private const string FRAME_FILE_PATTERN = "frame_*.jpg";
        private const string FFPROBE_ARGS = "-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1:noinfer_types=0 \"{0}\"";
        private const string DEFAULT_DURATION = "0";
        private const char QUOTE = '"';
        private const string FPS_FORMAT = "fps=1/{0}";
        private const string VIDEO_FILTER_FORMAT = "-i \"{0}\" -vf \"{1}\" -q:v {2} \"{3}\"";

        private readonly string _basePath;
        private readonly IChatClient _chatClient;
        private readonly string _visionModelName;
        private readonly int _frameIntervalSeconds;
        private readonly int _maxFramesPerVideo;
        private readonly int _frameQuality;
        private readonly string _tempDirectory;
        private string? _lastAnalysis;

        public VideoAnalysisService(
            string basePath,
            IChatClient chatClient,
            string visionModelName = "llava:7b",
            int frameIntervalSeconds = 5,
            int maxFramesPerVideo = 10,
            int frameQuality = 75)
        {
            _basePath = basePath;
            _chatClient = chatClient;
            _visionModelName = visionModelName;
            _frameIntervalSeconds = frameIntervalSeconds;
            _maxFramesPerVideo = maxFramesPerVideo;
            _frameQuality = frameQuality;
            _tempDirectory = Path.Combine(basePath, TEMP_FRAMES_DIR, FRAMES_SUBDIR);
        }

        /// <summary>
        /// Analyzes a video file by sampling frames and describing them.
        /// </summary>
        public async Task<string> AnalyzeVideoAsync(Stream videoStream, string mimeType, CancellationToken ct = default)
        {
            try
            {
                Directory.CreateDirectory(_tempDirectory);

                string videoPath = Path.Combine(_tempDirectory, VIDEO_PREFIX + Guid.NewGuid() + TEMP_EXTENSION);
                using (FileStream fileStream = File.Create(videoPath))
                {
                    await videoStream.CopyToAsync(fileStream, ct);
                }

                List<string> framePaths = await ExtractFramesAsync(videoPath, ct);

                File.Delete(videoPath);

                bool hasFrames = framePaths.Count > 0;
                if (!hasFrames)
                {
                    _lastAnalysis = NO_FRAMES_MSG;
                    return _lastAnalysis;
                }

                StringBuilder descriptions = new StringBuilder();
                string result = string.Empty;

                for (int i = 0; i < framePaths.Count; i++)
                {
                    bool isCancelled = ct.IsCancellationRequested;
                    if (isCancelled)
                    {
                        break;
                    }

                    try
                    {
                        byte[] frameBytes = await File.ReadAllBytesAsync(framePaths[i], ct);
                        string description = await AnalyzeFrameAsync(frameBytes, string.Format(FRAME_LABEL_FORMAT, i + 1, framePaths.Count), ct);
                        descriptions.AppendLine(description);
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.Handle(ex, _basePath, false);
                    }
                }

                for (int i = 0; i < framePaths.Count; i++)
                {
                    try 
                    { 
                        File.Delete(framePaths[i]); 
                    } 
                    catch { }
                }

                result = descriptions.ToString();
                _lastAnalysis = result;
                return _lastAnalysis;
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, _basePath, false);
                _lastAnalysis = VIDEO_ANALYSIS_ERROR + ex.Message;
                return _lastAnalysis;
            }
        }

        /// <summary>
        /// Analyzes a single frame (JPEG/PNG bytes) and returns a description.
        /// </summary>
        public async Task<string> AnalyzeFrameAsync(byte[] frameBytes, string context = "", CancellationToken ct = default)
        {
            try
            {
                string base64Image = Convert.ToBase64String(frameBytes);

                string visionPrompt = VISION_PROMPT;
                bool hasContext = !string.IsNullOrEmpty(context);
                if (hasContext)
                {
                    visionPrompt += VISION_CONTEXT_SUFFIX + context;
                }

                StringBuilder result = new StringBuilder();
                bool hasResult = false;
                await foreach (string token in _chatClient.StreamAsync(visionPrompt, base64Image))
                {
                    result.Append(token);
                    hasResult = true;
                    bool isCancelled = ct.IsCancellationRequested;
                    if (isCancelled)
                    {
                        break;
                    }
                }

                string analysis = hasResult ? result.ToString() : NO_RESPONSE_MSG;
                _lastAnalysis = analysis;
                return analysis;
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, _basePath, false);
                return FRAME_ANALYSIS_ERROR + ex.Message;
            }
        }

        /// <summary>
        /// Returns the most recent analysis result for prompt injection.
        /// </summary>
        public string? GetLastAnalysis() => _lastAnalysis;

        /// <summary>
        /// Extracts frames from a video file using ffmpeg.
        /// </summary>
        private async Task<List<string>> ExtractFramesAsync(string videoPath, CancellationToken ct = default)
        {
            List<string> framePaths = new List<string>();

            try
            {
                ProcessStartInfo ffmpegCheck = new ProcessStartInfo
                {
                    FileName = FFMPEG_EXECUTABLE,
                    Arguments = VERSION_ARG,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                string processResult = string.Empty;
                using (Process? process = Process.Start(ffmpegCheck))
                {
                    bool processIsNull = process == null;
                    if (processIsNull)
                    {
                        ErrorHandler.Handle(new InvalidOperationException(FFMPEG_NOT_FOUND), _basePath, false);
                        return framePaths;
                    }

                    await process.WaitForExitAsync(ct);
                    bool checkFailed = process.ExitCode != 0;
                    if (checkFailed)
                    {
                        ErrorHandler.Handle(new InvalidOperationException(FFMPEG_CHECK_FAILED), _basePath, false);
                        return framePaths;
                    }
                }

                string framePattern = Path.Combine(_tempDirectory, string.Format(FRAME_PATTERN_FORMAT, "%04d"));
                string duration = await GetVideoDurationAsync(videoPath, ct);

                string fpsFilter = string.Format(FPS_FORMAT, _frameIntervalSeconds);
                string videoFilterArgs = string.Format(VIDEO_FILTER_FORMAT, videoPath, fpsFilter, _frameQuality, framePattern);

                ProcessStartInfo extractProcess = new ProcessStartInfo
                {
                    FileName = FFMPEG_EXECUTABLE,
                    Arguments = videoFilterArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(extractProcess))
                {
                    bool processNotNull = process != null;
                    if (processNotNull)
                    {
                        await process.WaitForExitAsync(ct);

                        bool extractSucceeded = process.ExitCode == 0;
                        if (extractSucceeded)
                        {
                            DirectoryInfo frameDir = new DirectoryInfo(_tempDirectory);
                            FileInfo[] frames = frameDir.GetFiles(FRAME_FILE_PATTERN);

                            List<string> frameResult = new List<string>();
                            int maxFrames = Math.Min(frames.Length, _maxFramesPerVideo);
                            for (int i = 0; i < maxFrames; i++)
                            {
                                frameResult.Add(frames[i].FullName);
                            }

                            framePaths = frameResult;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, _basePath, false);
            }

            return framePaths;
        }

        /// <summary>
        /// Gets the duration of a video file.
        /// </summary>
        private async Task<string> GetVideoDurationAsync(string videoPath, CancellationToken ct = default)
        {
            try
            {
                string ffprobeArgs = string.Format(FFPROBE_ARGS, videoPath);
                ProcessStartInfo process = new ProcessStartInfo
                {
                    FileName = FFPROBE_EXECUTABLE,
                    Arguments = ffprobeArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                string result = string.Empty;
                using (Process? p = Process.Start(process))
                {
                    bool processNotNull = p != null;
                    if (processNotNull)
                    {
                        string? duration = await p.StandardOutput.ReadLineAsync(ct);
                        await p.WaitForExitAsync(ct);
                        result = duration ?? DEFAULT_DURATION;
                    }
                }

                return result;
            }
            catch
            {
                return DEFAULT_DURATION;
            }
        }
    }
}
