using Alissa.Core.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Real implementation of IProcessRunner using System.Diagnostics.Process.
    /// </summary>
    public class ProcessRunner : IProcessRunner
    {
        /// <summary>
        /// Runs an external process and captures text output.
        /// </summary>
        public async Task<(string output, int exitCode)> RunAsync(
            string binaryPath,
            string arguments,
            byte[]? stdinData = null,
            int timeoutMilliseconds = 120000,
            CancellationToken cancellationToken = default)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = binaryPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = stdinData != null,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            string output = string.Empty;
            string stderr = string.Empty;
            int exitCode = -1;

            try
            {
                using (Process process = Process.Start(psi))
                {
                    bool processValid = process != null;
                    if (!processValid)
                    {
                        throw new InvalidOperationException($"Failed to start process: {binaryPath}");
                    }

                    bool hasStdin = stdinData != null && stdinData.Length > 0;
                    if (hasStdin)
                    {
                        using (var stdin = process.StandardInput.BaseStream)
                        {
                            await stdin.WriteAsync(stdinData, 0, stdinData!.Length, cancellationToken).ConfigureAwait(false);
                            stdin.Close();
                        }
                    }

                    output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                    stderr = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);

                    bool waitSucceeded = process.WaitForExit(timeoutMilliseconds);
                    if (!waitSucceeded)
                    {
                        process.Kill();
                        throw new InvalidOperationException($"Process {binaryPath} exceeded timeout of {timeoutMilliseconds}ms");
                    }

                    exitCode = process.ExitCode;

                    bool isNonZeroExit = exitCode != 0;
                    if (isNonZeroExit && !string.IsNullOrEmpty(stderr))
                    {
                        output = $"{output}\n[stderr]: {stderr}";
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to run process: {binaryPath}", ex);
            }

            return (output, exitCode);
        }

        /// <summary>
        /// Runs an external process and captures binary output.
        /// </summary>
        public async Task<(byte[] output, int exitCode)> RunBinaryAsync(
            string binaryPath,
            string arguments,
            byte[]? stdinData = null,
            int timeoutMilliseconds = 120000,
            CancellationToken cancellationToken = default)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = binaryPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = stdinData != null,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            byte[] output = Array.Empty<byte>();
            int exitCode = -1;

            try
            {
                using (Process process = Process.Start(psi))
                {
                    bool processValid = process != null;
                    if (!processValid)
                    {
                        throw new InvalidOperationException($"Failed to start process: {binaryPath}");
                    }

                    bool hasStdin = stdinData != null && stdinData.Length > 0;
                    if (hasStdin)
                    {
                        using (var stdin = process.StandardInput.BaseStream)
                        {
                            await stdin.WriteAsync(stdinData, 0, stdinData!.Length, cancellationToken).ConfigureAwait(false);
                            stdin.Close();
                        }
                    }

                    using (var stdoutStream = process.StandardOutput.BaseStream)
                    {
                        using (var memStream = new MemoryStream())
                        {
                            await stdoutStream.CopyToAsync(memStream, 81920, cancellationToken).ConfigureAwait(false);
                            output = memStream.ToArray();
                        }
                    }

                    bool waitSucceeded = process.WaitForExit(timeoutMilliseconds);
                    if (!waitSucceeded)
                    {
                        process.Kill();
                        throw new InvalidOperationException($"Process {binaryPath} exceeded timeout of {timeoutMilliseconds}ms");
                    }

                    exitCode = process.ExitCode;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to run process: {binaryPath}", ex);
            }

            return (output, exitCode);
        }
    }
}
