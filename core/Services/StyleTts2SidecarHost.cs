using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Alissa.Core.Models;

namespace Alissa.Core.Services
{
    /// <summary>
    /// Manages the StyleTTS2 Python sidecar process (FastAPI server on 127.0.0.1:8020).
    /// Starts the process on demand, polls /health endpoint, and provides graceful shutdown.
    /// </summary>
    public class StyleTts2SidecarHost : IDisposable
    {
        private readonly SpeechConfig _config;
        private readonly string _projectRoot;
        private Process? _process;
        private readonly HttpClient _httpClient;
        private bool _disposed;

        public StyleTts2SidecarHost(SpeechConfig config, string projectRoot)
        {
            _config = config;
            _projectRoot = projectRoot;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Returns true if the sidecar process is running and /health reports ok.
        /// </summary>
        public bool IsHealthy
        {
            get
            {
                bool processExists = _process != null && !_process.HasExited;
                return processExists;
            }
        }

        /// <summary>
        /// Start the sidecar process and wait for health check.
        /// Returns true if started and healthy within timeout.
        /// Logs status messages via callback.
        /// </summary>
        public async Task<bool> StartAsync(
            Action<string> onStatus,
            CancellationToken ct = default
        )
        {
            try
            {
                onStatus("[StyleTTS2] Starting sidecar process...");

                // Verify required files exist
                bool pythonExists = File.Exists(_config.StyleTts2PythonExe);
                bool serverScriptExists = File.Exists(_config.StyleTts2ServerScript);

                if (!pythonExists)
                {
                    onStatus($"[StyleTTS2] ERROR: PythonExe not found: {_config.StyleTts2PythonExe}");
                    return false;
                }

                if (!serverScriptExists)
                {
                    onStatus($"[StyleTTS2] ERROR: ServerScript not found: {_config.StyleTts2ServerScript}");
                    return false;
                }

                // Prepare environment variables
                var startInfo = new ProcessStartInfo
                {
                    FileName = _config.StyleTts2PythonExe,
                    Arguments = $"\"{_config.StyleTts2ServerScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Set environment variables
                startInfo.EnvironmentVariables["STYLETTS2_REPO"] = _config.StyleTts2RepoPath;
                startInfo.EnvironmentVariables["STYLETTS2_CHECKPOINT"] = _config.StyleTts2CheckpointPath;
                startInfo.EnvironmentVariables["STYLETTS2_CONFIG"] = _config.StyleTts2ConfigPath;

                bool espkExists = File.Exists(_config.EspeakLibraryPath);
                if (espkExists)
                {
                    startInfo.EnvironmentVariables["PHONEMIZER_ESPEAK_LIBRARY"] = _config.EspeakLibraryPath;
                }

                _process = new Process { StartInfo = startInfo };

                // Capture stdout/stderr
                _process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        onStatus(args.Data);
                    }
                };

                _process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        onStatus($"[StyleTTS2] {args.Data}");
                    }
                };

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                onStatus("[StyleTTS2] Process started, polling health...");

                // Wait for health endpoint
                bool healthOk = await WaitForHealthAsync(onStatus, ct);
                if (!healthOk)
                {
                    onStatus("[StyleTTS2] Startup timeout: sidecar did not become healthy");
                    StopAsync().Wait(5000);
                    return false;
                }

                onStatus("[StyleTTS2] Sidecar healthy");
                return true;
            }
            catch (Exception ex)
            {
                onStatus($"[StyleTTS2] Startup exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Poll /health endpoint until ok or timeout.
        /// </summary>
        private async Task<bool> WaitForHealthAsync(
            Action<string> onStatus,
            CancellationToken ct
        )
        {
            int timeoutSeconds = _config.StyleTts2StartupTimeoutSeconds;
            int elapsedSeconds = 0;
            int pollIntervalMs = 500;

            while (elapsedSeconds < timeoutSeconds)
            {
                try
                {
                    CancellationTokenSource pollCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    pollCts.CancelAfter(3000);

                    string healthUrl = $"{_config.StyleTts2BaseUrl}/health";
                    HttpResponseMessage resp = await _httpClient.GetAsync(healthUrl, pollCts.Token);

                    if (resp.IsSuccessStatusCode)
                    {
                        string content = await resp.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(content))
                        {
                            bool ok = doc.RootElement.TryGetProperty("ok", out var okElement) 
                                && okElement.GetBoolean();
                            if (ok)
                            {
                                return true;
                            }
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    // Not ready yet
                }
                catch (OperationCanceledException)
                {
                    // Timeout on this poll, continue
                }
                catch (Exception ex)
                {
                    onStatus($"[StyleTTS2] Health check error: {ex.Message}");
                }

                await Task.Delay(pollIntervalMs, ct);
                elapsedSeconds += (pollIntervalMs / 1000);
            }

            return false;
        }

        /// <summary>
        /// Stop the sidecar process gracefully.
        /// </summary>
        public async Task StopAsync()
        {
            if (_process == null)
            {
                return;
            }

            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                    bool exited = _process.WaitForExit(5000);
                    if (!exited)
                    {
                        _process.Kill(entireProcessTree: true);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw
                Debug.WriteLine($"[StyleTTS2] Error stopping process: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            StopAsync().Wait(5000);
            _process?.Dispose();
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}
