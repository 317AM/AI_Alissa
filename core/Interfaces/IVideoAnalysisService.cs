using System;
using System.Threading;
using System.Threading.Tasks;

namespace Alissa.Core.Interfaces
{
    /// <summary>
    /// Interface for video and frame analysis services.
    /// Analyzes video files or individual frames using a vision model.
    /// </summary>
    public interface IVideoAnalysisService
    {
        /// <summary>
        /// Accepts a video file (mp4, webm, etc.) from an HTTP upload or local path.
        /// Samples frames at the configured interval, runs them through the vision model,
        /// and returns a natural-language summary of what was on screen.
        /// </summary>
        Task<string> AnalyzeVideoAsync(Stream videoStream, string mimeType, CancellationToken ct = default);

        /// <summary>
        /// Accepts a single frame (JPEG/PNG bytes) and returns a description.
        /// Used for real-time frame-by-frame analysis when the client sends frames instead of video.
        /// </summary>
        Task<string> AnalyzeFrameAsync(byte[] frameBytes, string context = "", CancellationToken ct = default);

        /// <summary>
        /// Returns the most recent analysis result for prompt injection.
        /// </summary>
        string? GetLastAnalysis();
    }
}
