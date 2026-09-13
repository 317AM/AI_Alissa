using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Alissa.Core.Interfaces
{
    /// <summary>
    /// Abstraction for running external processes.
    /// Enables testing without actually spawning subprocesses.
    /// </summary>
    public interface IProcessRunner
    {
        /// <summary>
        /// Runs an external process with the given binary path and arguments.
        /// </summary>
        /// <param name="binaryPath">Path to executable</param>
        /// <param name="arguments">Command-line arguments</param>
        /// <param name="stdinData">Optional data to write to stdin (e.g., audio bytes)</param>
        /// <param name="timeoutMilliseconds">Timeout in milliseconds (default 120000)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Process output (stdout) as string and exit code</returns>
        Task<(string output, int exitCode)> RunAsync(
            string binaryPath,
            string arguments,
            byte[]? stdinData = null,
            int timeoutMilliseconds = 120000,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs an external process and captures binary output (e.g., audio data).
        /// </summary>
        /// <param name="binaryPath">Path to executable</param>
        /// <param name="arguments">Command-line arguments</param>
        /// <param name="stdinData">Optional data to write to stdin</param>
        /// <param name="timeoutMilliseconds">Timeout in milliseconds (default 120000)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Binary output (stdout) and exit code</returns>
        Task<(byte[] output, int exitCode)> RunBinaryAsync(
            string binaryPath,
            string arguments,
            byte[]? stdinData = null,
            int timeoutMilliseconds = 120000,
            CancellationToken cancellationToken = default);
    }
}
