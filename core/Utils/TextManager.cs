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
    // ── Private state ───────────────────────────────────────────
    private static OutputMode       _mode        = OutputMode.Console;
    private static HubOutputClient? _hub         = null;
    private static string           _personality = "alissa";

    // ── Public read-only properties ─────────────────────────────
    public static OutputMode Mode         => _mode;
    public static bool       IsHubMode    => _mode == OutputMode.Hub317;
    public static bool       HubConnected => _hub?.IsConnected ?? false;

    // ── Setup ────────────────────────────────────────────────────
    /// <summary>
    /// Call once at startup.  Pass hubUrl only when mode is Hub317.
    /// </summary>
    public static void Configure(OutputMode mode, string? hubUrl = null, string personality = "alissa")
    {
        _mode        = mode;
        _personality = personality;

        if (mode == OutputMode.Hub317)
        {
            if (string.IsNullOrWhiteSpace(hubUrl))
                throw new ArgumentException("hubUrl is required for Hub317 mode.");

            _hub = new HubOutputClient(hubUrl);
            // Fire-and-forget — reconnects automatically in background
            _ = _hub.RunAsync();
        }

        Status($"[TextManager] Mode = {mode}" + (hubUrl != null ? $" → {hubUrl}" : ""));
    }

    /// <summary>
    /// Convenience: reads --hub / --hub=URL from command-line args
    /// and auto-configures.  Call before RunChatLoop.
    /// Returns true if Hub mode was activated.
    /// </summary>
    public static bool TryConfigureFromArgs(string[] args, string defaultUrl = "ws://localhost:317/ws/alissa")
    {
        var hubArg = args.FirstOrDefault(a =>
            a == "--hub" || a.StartsWith("--hub=", StringComparison.OrdinalIgnoreCase));

        var envUrl = Environment.GetEnvironmentVariable("ALISSA_HUB_URL");

        var url = hubArg?.Contains('=') == true
            ? hubArg.Split('=', 2)[1]
            : (hubArg != null ? defaultUrl : envUrl);

        if (url is null) return false;

        Configure(OutputMode.Hub317, url);
        return true;
    }

    // ── AI streaming output ──────────────────────────────────────

    /// <summary>
    /// Signal that Alissa is starting to generate a response.
    /// In Hub mode sends stream_start; in Console mode prints "Alissa: ".
    /// </summary>
    public static void BeginResponse(string? personality = null)
    {
        personality ??= _personality;
        switch (_mode)
        {
            case OutputMode.Console:
                Console.Write("\nAlissa: ");
                break;
            case OutputMode.Hub317:
                _hub?.SendStreamStart(personality);
                break;
        }
    }

    /// <summary>
    /// Print a single token from the AI stream.
    /// In Console mode: writes directly; in Hub mode: sends as stream_chunk.
    /// </summary>
    public static void PrintToken(string token, string? personality = null)
    {
        switch (_mode)
        {
            case OutputMode.Console:
                Console.Write(token);
                break;
            case OutputMode.Hub317:
                _hub?.SendChunk(token);
                break;
        }
    }

    /// <summary>
    /// Signal the end of a response.
    /// In Hub mode sends stream_end; in Console mode prints a newline.
    /// </summary>
    public static void EndResponse(string? personality = null)
    {
        personality ??= _personality;
        switch (_mode)
        {
            case OutputMode.Console:
                Console.WriteLine();
                break;
            case OutputMode.Hub317:
                _hub?.SendStreamEnd(personality);
                break;
        }
    }

    /// <summary>
    /// Send a complete, non-streamed response in one shot.
    /// Useful for short replies where streaming isn't needed.
    /// </summary>
    public static void SendComplete(string content, string? personality = null)
    {
        personality ??= _personality;
        switch (_mode)
        {
            case OutputMode.Console:
                Console.WriteLine($"\nAlissa: {content}");
                break;
            case OutputMode.Hub317:
                _hub?.SendMessage(content, personality);
                break;
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
    /// In Hub mode, returns metadata (system prompt, history, personality id)
    /// attached to the most recently received message. Null fields in Console mode.
    /// </summary>
    public static (string? PersonalityId, string? SystemPrompt, List<HistEntry>? History) LastMessageMeta =>
        (_hub?.LastPersonalityId, _hub?.LastSystemPrompt, _hub?.LastHistory);

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
        var buf = new byte[65_536];
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

            var raw = Encoding.UTF8.GetString(ms.ToArray());
            try
            {
                var msg = JsonSerializer.Deserialize<HubInboundMsg>(raw, _json);
                // Only queue messages where the user is talking — ignore system pings
                if (msg?.Role == "user" && msg.Content != null)
                    _inbox.TryAdd(msg, millisecondsTimeout: 0);
            }
            catch { /* malformed JSON — skip */ }
        }
    }

    // ── Outbound helpers ─────────────────────────────────────────
    public void SendStreamStart(string personalityId) =>
        _ = SendRawAsync(new OutMsg { Type = "stream_start", Role = "assistant", PersonalityId = personalityId });

    public void SendChunk(string token) =>
        _ = SendRawAsync(new OutMsg { Type = "stream_chunk", Content = token });

    public void SendStreamEnd(string personalityId) =>
        _ = SendRawAsync(new OutMsg { Type = "stream_end", PersonalityId = personalityId });

    public void SendMessage(string content, string personalityId) =>
        _ = SendRawAsync(new OutMsg { Type = "message", Role = "assistant", Content = content, PersonalityId = personalityId });

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
    /// Also stores personalityId and systemPrompt on the message for use by PromptBuilder.
    /// </summary>
    public async Task<string?> WaitForMessageAsync(CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var msg = _inbox.Take(ct);
                LastPersonalityId = msg.PersonalityId ?? "alissa";
                LastSystemPrompt  = msg.SystemPrompt;
                LastHistory       = msg.History;
                return msg.Content;
            }
            catch (OperationCanceledException) { return null; }
        }, ct);
    }

    // The most recent metadata from the Hub for use by PromptBuilder
    public string?          LastPersonalityId { get; private set; }
    public string?          LastSystemPrompt  { get; private set; }
    public List<HistEntry>? LastHistory       { get; private set; }

    // ── DTOs ──────────────────────────────────────────────────────
    private class OutMsg
    {
        [JsonPropertyName("type")]          public string? Type          { get; set; }
        [JsonPropertyName("role")]          public string? Role          { get; set; }
        [JsonPropertyName("content")]       public string? Content       { get; set; }
        [JsonPropertyName("personalityId")] public string? PersonalityId { get; set; }
    }

    private class HubInboundMsg
    {
        [JsonPropertyName("type")]          public string?          Type          { get; set; }
        [JsonPropertyName("role")]          public string?          Role          { get; set; }
        [JsonPropertyName("content")]       public string?          Content       { get; set; }
        [JsonPropertyName("personalityId")] public string?          PersonalityId { get; set; }
        [JsonPropertyName("systemPrompt")]  public string?          SystemPrompt  { get; set; }
        [JsonPropertyName("history")]       public List<HistEntry>? History       { get; set; }
    }
}

/// <summary>A single turn in the conversation history sent by Hub 317.</summary>
public record HistEntry(
    [property: JsonPropertyName("role")]    string Role,
    [property: JsonPropertyName("content")] string Content
);