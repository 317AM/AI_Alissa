// ================================================================
//  TextManager.cs  —  Drop into Alissa.Core/Utils/
//  Replaces TextHandler.cs for all I/O.
//
//  Adds a private OutputMode variable:
//    OutputMode.Console  →  behaves identically to old TextHandler
//    OutputMode.Hub317   →  routes AI tokens over WebSocket to Hub 317
//
//  Status/log messages always print to the console regardless of mode
//  so you still see what Alissa is doing even in Hub mode.
// ================================================================

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alissa.Core.Utils;

/// <summary>
/// Where Alissa's AI output is sent.
/// </summary>
public enum OutputMode
{
    Console = 0,  // default — console I/O like before
    Hub317  = 1,  // WebSocket bridge to Hub 317 site
}

/// <summary>
/// Unified I/O manager.  All Print / Read calls go through here.
/// Call TextManager.Configure() once at startup; everything else
/// stays the same in the rest of Alissa's codebase.
/// </summary>
public static class TextManager
{
    // ══════════════════════════════════════════════════════════════════════════════════════════════════════════════
    // Constants
    // ══════════════════════════════════════════════════════════════════════════════════════════════════════════════

    private const string HUB_URL_REQUIRED = "hubUrl is required for Hub317 mode.";
    private const string CONFIG_MESSAGE_PREFIX = "[TextManager] Mode = ";
    private const string CONFIG_MESSAGE_ARROW = " → ";
    private const string HUB_ARG_PREFIX = "--hub";
    private const string HUB_ARG_WITH_EQUALS = "--hub=";
    private const string HUB_ENV_VARIABLE = "ALISSA_HUB_URL";
    private const string DEFAULT_HUB_URL = "ws://localhost:317/ws/alissa";
    private const string ARG_SPLIT_CHAR = "=";
    private const string ALISSA_RESPONSE_PREFIX = "\nAlissa: ";
    private const string STREAM_START_TYPE = "stream_start";
    private const string STREAM_CHUNK_TYPE = "stream_chunk";
    private const string STREAM_END_TYPE = "stream_end";
    private const string MESSAGE_TYPE = "message";
    private const string ROLE_USER = "user";
    private const string ROLE_ASSISTANT = "assistant";

    // ── Private state ───────────────────────────────────────────
    private static OutputMode       _mode = OutputMode.Console;
    private static HubOutputClient? _hub  = null;

    // ── Public read-only properties ─────────────────────────────
    public static OutputMode Mode         => _mode;
    public static bool       IsHubMode    => _mode == OutputMode.Hub317;
    public static bool       HubConnected => _hub?.IsConnected ?? false;
    public static string?    LastUserName => _hub?.LastUserName;

    // ── Setup ────────────────────────────────────────────────────
    /// <summary>
    /// Call once at startup.  Pass hubUrl only when mode is Hub317.
    /// </summary>
    public static void Configure(OutputMode mode, string? hubUrl = null)
    {
        _mode = mode;

        if (mode == OutputMode.Hub317)
        {
            if (string.IsNullOrWhiteSpace(hubUrl))
                throw new ArgumentException(HUB_URL_REQUIRED);

            _hub = new HubOutputClient(hubUrl);
            // Fire-and-forget — reconnects automatically in background
            _ = _hub.RunAsync();
        }

        string configMessage = CONFIG_MESSAGE_PREFIX + mode + (hubUrl != null ? CONFIG_MESSAGE_ARROW + hubUrl : string.Empty);
        Status(configMessage);
    }

    /// <summary>
    /// Convenience: reads --hub / --hub=URL from command-line args
    /// and auto-configures.  Call before RunChatLoop.
    /// Returns true if Hub mode was activated.
    /// </summary>
    public static bool TryConfigureFromArgs(string[] args, string defaultUrl = DEFAULT_HUB_URL)
    {
        bool result = false;
        string? hubArg = args.FirstOrDefault(a =>
            a == HUB_ARG_PREFIX || a.StartsWith(HUB_ARG_WITH_EQUALS, StringComparison.OrdinalIgnoreCase));

        string? envUrl = Environment.GetEnvironmentVariable(HUB_ENV_VARIABLE);

        string? url = hubArg?.Contains('=') == true
            ? hubArg.Split(ARG_SPLIT_CHAR, 2)[1]
            : (hubArg != null ? defaultUrl : envUrl);

        if (url != null)
        {
            Configure(OutputMode.Hub317, url);
            result = true;
        }

        return result;
    }

    // ── AI streaming output ──────────────────────────────────────

    /// <summary>
    /// Signal that Alissa is starting to generate a response.
    /// In Hub mode sends stream_start; in Console mode prints "Alissa: ".
    /// </summary>
    public static void BeginResponse()
    {
        if (_mode == OutputMode.Console)
        {
            Console.Write(ALISSA_RESPONSE_PREFIX);
        }
        else if (_mode == OutputMode.Hub317)
        {
            _hub?.SendStreamStart();
        }
    }

    /// <summary>
    /// Print a single token from the AI stream.
    /// In Console mode: writes directly; in Hub mode: sends as stream_chunk.
    /// </summary>
    public static void PrintToken(string token)
    {
        if (_mode == OutputMode.Console)
        {
            Console.Write(token);
        }
        else if (_mode == OutputMode.Hub317)
        {
            _hub?.SendChunk(token);
        }
    }

    /// <summary>
    /// Signal the end of a response.
    /// In Hub mode sends stream_end; in Console mode prints a newline.
    /// </summary>
    public static void EndResponse()
    {
        if (_mode == OutputMode.Console)
        {
            Console.WriteLine();
        }
        else if (_mode == OutputMode.Hub317)
        {
            _hub?.SendStreamEnd();
        }
    }

    /// <summary>
    /// Send a complete, non-streamed response in one shot.
    /// Useful for short replies where streaming isn't needed.
    /// </summary>
    public static void SendMessage(string content)
    {
        string capitalizedRole = char.ToUpper(ROLE_ASSISTANT[0]) + ROLE_ASSISTANT.Substring(1);
        string formattedContent = $"\n{capitalizedRole}: {content}";

        if (_mode == OutputMode.Console)
        {
            Console.WriteLine(formattedContent);
        }
        else if (_mode == OutputMode.Hub317)
        {
            _hub?.SendMessage(content);
        }
    }

    public static void SendComplete(string content) => SendMessage(content);

    /// <summary>
    /// Send audio data as base64-encoded audio_chunk (Hub317 mode only).
    /// In Console mode, this is a no-op.
    /// </summary>
    public static void SendAudioChunk(byte[] audioData)
    {
        if (_mode == OutputMode.Hub317)
        {
            _hub?.SendAudioChunk(audioData);
        }
    }

    // ── User input ───────────────────────────────────────────────

    /// <summary>
    /// Read a user message.
    /// Console: reads a line; Hub: waits for next inbound WebSocket message.
    /// </summary>
    public static async Task<string?> ReadInputAsync(CancellationToken ct = default)
    {
        return _mode switch
        {
            OutputMode.Console => await Task.Run(Console.ReadLine, ct),
            OutputMode.Hub317  => await (_hub?.WaitForMessageAsync(ct) ?? Task.FromResult<string?>(null)),
            _                  => null
        };
    }

    /// <summary>
    /// In Hub mode, returns metadata (system prompt, history)
    /// attached to the most recently received message. Null fields in Console mode.
    /// </summary>
    public static (string? SystemPrompt, List<HistEntry>? History) LastMessageMeta =>
        (_hub?.LastSystemPrompt, _hub?.LastHistory);

    /// <summary>
    /// In Hub mode, returns audio WAV bytes if the last message was audio.
    /// Returns null in Console mode or if last message was text.
    /// </summary>
    public static byte[]? LastMessageAudioWav =>
        _hub?.LastAudioWavBytes;

    // ── Status / log output ──────────────────────────────────────
    // These always go to the console so you can monitor Alissa
    // even when AI output is going to the site.

    /// <summary>Status message — always printed to console.</summary>
    public static void Status(string text) => Console.WriteLine(text);

    /// <summary>Print a blank line to console.</summary>
    public static void Blank() => Console.WriteLine();

    // ── Backward-compat shims (drop-in for TextHandler) ──────────

    /// <summary>Backward-compat: maps to Status or PrintToken depending on context.</summary>
    public static void PrintText(string text, bool appendNewline = false)
    {
        if (appendNewline) Status(text);
        else               Console.Write(text);   // status always to console
    }

    /// <summary>Backward-compat: blank line.</summary>
    public static void NextLine() => Console.WriteLine();

    /// <summary>Backward-compat synchronous read (Console only).</summary>
    public static string ReadText() => Console.ReadLine() ?? string.Empty;

    /// <summary>Backward-compat synchronous readline.</summary>
    public static string ReadLine() => Console.ReadLine() ?? string.Empty;
}

// ================================================================
//  HubOutputClient  —  Internal WebSocket client used by TextManager
// ================================================================

/// <summary>
/// Maintains a persistent WebSocket connection to Hub 317's /ws/alissa endpoint.
/// Automatically reconnects on disconnect.
/// Thread-safe: SendChunk / SendStreamStart / SendStreamEnd can be called
/// from the AI streaming thread while WaitForMessageAsync blocks the main loop.
/// </summary>
public sealed class HubOutputClient
{
    // ── Config ───────────────────────────────────────────────────
    private readonly string _url;
    private readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(5);

    // ── State ────────────────────────────────────────────────────
    private ClientWebSocket?                 _ws;
    private readonly SemaphoreSlim           _sendLock = new(1, 1);
    private readonly BlockingCollection<HubInboundMsg> _inbox = new(128);
    private CancellationTokenSource          _cts = new();

    public bool IsConnected => _ws?.State == WebSocketState.Open;

    private static readonly JsonSerializerOptions _json =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public HubOutputClient(string url) => _url = url;

    // ── Connection loop ──────────────────────────────────────────
    public async Task RunAsync(CancellationToken externalCt = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
        var ct = _cts.Token;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                _ws = new ClientWebSocket();
                Console.WriteLine($"[Hub317] Connecting to {_url}…");
                await _ws.ConnectAsync(new Uri(_url), ct);
                Console.WriteLine("[Hub317] Connected. Listening for user messages.");
                await ListenAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.WriteLine($"[Hub317] Disconnected ({ex.Message}). Retry in {_reconnectDelay.TotalSeconds}s…");
                _ws?.Dispose(); _ws = null;
                await Task.Delay(_reconnectDelay, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        var buf = new byte[256_000]; // Increased for WAV data
        while (_ws?.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await _ws.ReceiveAsync(buf, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                    return;
                ms.Write(buf, 0, result.Count);
            } while (!result.EndOfMessage);

            // Try to parse as JSON first
            var raw = Encoding.UTF8.GetString(ms.ToArray());
            try
            {
                var msg = JsonSerializer.Deserialize<HubInboundMsg>(raw, _json);

                // Queue text messages where role=="user" && content!=null
                if (msg?.Role == "user" && msg.Content != null && msg.Type != "audio")
                {
                    _inbox.TryAdd(msg, millisecondsTimeout: 0);
                }
                // Queue audio messages where type=="audio" && content!=null (base64 WAV)
                else if (msg?.Type == "audio" && msg.Role == "user" && msg.Content != null)
                {
                    // Content is base64-encoded WAV data
                    msg.AudioWavBytes = Convert.FromBase64String(msg.Content);
                    _inbox.TryAdd(msg, millisecondsTimeout: 0);
                }
            }
            catch 
            { 
                // Try treating raw bytes as binary WAV data
                try
                {
                    var wavMsg = new HubInboundMsg
                    {
                        Type = "audio",
                        Role = "user",
                        AudioWavBytes = ms.ToArray()
                    };
                    _inbox.TryAdd(wavMsg, millisecondsTimeout: 0);
                }
                catch
                {
                    // Malformed message — skip
                }
            }
        }
    }

    // ── Outbound helpers ─────────────────────────────────────────
    public void SendStreamStart() =>
        _ = SendRawAsync(new OutMsg { Type = "stream_start", Role = "assistant" });

    public void SendChunk(string token) =>
        _ = SendRawAsync(new OutMsg { Type = "stream_chunk", Content = token });

    public void SendStreamEnd() =>
        _ = SendRawAsync(new OutMsg { Type = "stream_end", Role = "assistant" });

    public void SendMessage(string content) =>
        _ = SendRawAsync(new OutMsg { Type = "message", Role = "assistant", Content = content });

    public void SendAudioChunk(byte[] audioData) =>
        _ = SendRawAsync(new OutMsg { Type = "audio_chunk", Role = "assistant", Content = Convert.ToBase64String(audioData) });

    public void SendTranscript(string transcript) =>
        _ = SendRawAsync(new OutMsg { Type = "transcript", Role = "user", Content = transcript });

    private async Task SendRawAsync(OutMsg msg)
    {
        if (_ws?.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg, _json));
        await _sendLock.WaitAsync();
        try   { await _ws.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None); }
        catch { /* ws may have closed — RunAsync will reconnect */ }
        finally { _sendLock.Release(); }
    }

    // ── Inbound helper ───────────────────────────────────────────

    /// <summary>
    /// Blocks until a user message arrives from Hub 317.
    /// This replaces Console.ReadLine() in the chat loop when in Hub mode.
    /// Also stores userName, systemPrompt, and history for use by services.
    /// </summary>
    public async Task<string?> WaitForMessageAsync(CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var msg = _inbox.Take(ct);
                LastUserName      = msg.UserName ?? "User";
                LastSystemPrompt  = msg.SystemPrompt;
                LastHistory       = msg.History;
                LastAudioWavBytes = msg.AudioWavBytes;
                return msg.Content;
            }
            catch (OperationCanceledException) { return null; }
        }, ct);
    }

    // The most recent metadata from the Hub for use by PromptBuilder
    public string?          LastSystemPrompt  { get; private set; }
    public string?          LastUserName      { get; private set; }
    public List<HistEntry>? LastHistory       { get; private set; }
    public byte[]?          LastAudioWavBytes { get; private set; }

    // ── DTOs ──────────────────────────────────────────────────────
    private class OutMsg
    {
        [JsonPropertyName("type")]    public string? Type    { get; set; }
        [JsonPropertyName("role")]    public string? Role    { get; set; }
        [JsonPropertyName("content")] public string? Content { get; set; }
    }

    private class HubInboundMsg
    {
        [JsonPropertyName("type")]         public string?          Type         { get; set; }
        [JsonPropertyName("role")]         public string?          Role         { get; set; }
        [JsonPropertyName("content")]      public string?          Content      { get; set; }
        [JsonPropertyName("userName")]     public string?          UserName     { get; set; }
        [JsonPropertyName("systemPrompt")] public string?          SystemPrompt { get; set; }
        [JsonPropertyName("history")]      public List<HistEntry>? History      { get; set; }
        [JsonPropertyName("format")]       public string?          Format       { get; set; }

        // Not serialized from JSON, set after deserialization if type=="audio"
        public byte[]? AudioWavBytes { get; set; }
    }
}

/// <summary>A single turn in the conversation history sent by Hub 317.</summary>
public record HistEntry(
    [property: JsonPropertyName("role")]    string Role,
    [property: JsonPropertyName("content")] string Content
);