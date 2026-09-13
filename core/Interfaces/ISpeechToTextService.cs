using System;
using System.Threading;
using System.Threading.Tasks;

namespace Alissa.Core.Interfaces
{
    /// <summary>
    /// Service for converting audio to text (Speech-to-Text).
    /// </summary>
    public interface ISpeechToTextService
    {
        /// <summary>
        /// Transcribes audio from a file.
        /// </summary>
        /// <param name="audioFilePath">Path to audio file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Transcribed text</returns>
        Task<string> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Transcribes audio from byte array.
        /// </summary>
        /// <param name="audioBytes">Audio data</param>
        /// <param name="format">Audio format (e.g., "wav", "mp3")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Transcribed text</returns>
        Task<string> TranscribeAsync(byte[] audioBytes, string format, CancellationToken cancellationToken = default);
    }
}
