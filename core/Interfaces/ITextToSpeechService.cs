using System;
using System.Threading;
using System.Threading.Tasks;

namespace Alissa.Core.Interfaces
{
    /// <summary>
    /// Service for converting text to audio (Text-to-Speech).
    /// </summary>
    public interface ITextToSpeechService
    {
        /// <summary>
        /// Synthesizes audio from text and returns it as byte array.
        /// </summary>
        /// <param name="text">Text to synthesize</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Audio data (WAV format)</returns>
        Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Synthesizes audio from text and writes it to a file.
        /// </summary>
        /// <param name="text">Text to synthesize</param>
        /// <param name="outputPath">Path to write audio file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SynthesizeToFileAsync(string text, string outputPath, CancellationToken cancellationToken = default);
    }
}
