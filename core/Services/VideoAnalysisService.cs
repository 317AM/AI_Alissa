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
            _tempDirectory = Path.Combine(basePath, "temp", "frames");
        }

        /// <summary>
        /// Analyzes a video file by sampling frames and describing them.
        /// </summary>
        public async Task<string> AnalyzeVideoAsync(Stream videoStream, string mimeType, CancellationToken ct = default)
        {
            try
            {
                Directory.CreateDirectory(_tempDirectory);

                // Save video to temp file
                string videoPath = Path.Combine(_tempDirectory, $"video_{Guid.NewGuid()}.tmp");
                using (var fileStream = File.Create(videoPath))
                {
                    await videoStream.CopyToAsync(fileStream, ct);
                }

                // Extract frames
                var framePaths = await ExtractFramesAsync(videoPath, ct);

                // Clean up video
                File.Delete(videoPath);

                if (framePaths.Count == 0)
                {
                    _lastAnalysis = "No frames could be extracted from the video.";
                    return _lastAnalysis;
                }

                // Analyze each frame
                var descriptions = new StringBuilder();
                var result = string.Empty;
                if (framePaths.Count > 0)
                {
                    for (int i = 0; i < framePaths.Count; i++)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            break;
                        }

                        try
                        {
                            byte[] frameBytes = await File.ReadAllBytesAsync(framePaths[i], ct);
                            string description = await AnalyzeFrameAsync(frameBytes, $"Frame {i + 1}/{framePaths.Count}", ct);
                            descriptions.AppendLine(description);
                        }
                        catch (Exception ex)
                        {
                            ErrorHandler.Handle(ex, _basePath, false);
                        }
                    }

                    // Clean up frames
                    foreach (var framePath in framePaths)
                    {
                        try { File.Delete(framePath); } catch { }
                    }

                    result = descriptions.ToString();
                }

                _lastAnalysis = result;
                return _lastAnalysis;
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, _basePath, false);
                _lastAnalysis = $"Video analysis error: {ex.Message}";
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
                // Convert image to base64
                string base64Image = Convert.ToBase64String(frameBytes);

                // Build vision model prompt
                string visionPrompt = "Describe what is on screen in this frame. Focus on: open applications, visible text, code, errors, or work context. Be concise (2-3 sentences).";
                if (!string.IsNullOrEmpty(context))
                {
                    visionPrompt += $" Context: {context}";
                }

                // Call Ollama vision model
                // Note: This is a simplified approach. Full implementation would use proper Ollama API with vision support
                var result = new StringBuilder();
                var hasResult = false;
                await foreach (var token in _chatClient.StreamAsync(visionPrompt, base64Image))
                {
                    result.Append(token);
                    hasResult = true;
                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }
                }

                string analysis = hasResult ? result.ToString() : "No response from vision model";
                _lastAnalysis = analysis;
                return analysis;
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, _basePath, false);
                return $"Frame analysis error: {ex.Message}";
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
            var framePaths = new List<string>();

            try
            {
                // Check if ffmpeg is available
                var ffmpegCheck = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                var processResult = string.Empty;
                using (var process = Process.Start(ffmpegCheck))
                {
                    if (process == null)
                    {
                        ErrorHandler.Handle(new InvalidOperationException("ffmpeg not found in PATH"), _basePath, false);
                        return framePaths;
                    }

                    await process.WaitForExitAsync(ct);
                    if (process.ExitCode != 0)
                    {
                        ErrorHandler.Handle(new InvalidOperationException("ffmpeg check failed"), _basePath, false);
                        return framePaths;
                    }
                }

                // Get video duration and calculate frame extraction
                string framePattern = Path.Combine(_tempDirectory, $"frame_%04d.jpg");
                string duration = await GetVideoDurationAsync(videoPath, ct);

                // Extract frames at regular intervals
                var extractProcess = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{videoPath}\" -vf \"fps=1/{_frameIntervalSeconds}\" -q:v {_frameQuality} \"{framePattern}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(extractProcess))
                {
                    if (process != null)
                    {
                        await process.WaitForExitAsync(ct);

                        if (process.ExitCode == 0)
                        {
                            // Collect generated frames
                            var frameDir = new DirectoryInfo(_tempDirectory);
                            var frames = frameDir.GetFiles("frame_*.jpg");

                            var frameResult = new List<string>();
                            for (int i = 0; i < Math.Min(frames.Length, _maxFramesPerVideo); i++)
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
                var process = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1:noinfer_types=0 \"{videoPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                var result = string.Empty;
                using (var p = Process.Start(process))
                {
                    if (p != null)
                    {
                        string? duration = await p.StandardOutput.ReadLineAsync(ct);
                        await p.WaitForExitAsync(ct);
                        result = duration ?? "0";
                    }
                }

                return result;
            }
            catch
            {
                // ffprobe not available
                return "0";
            }
        }
    }
}
