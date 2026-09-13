# Hub317 Protocol Documentation

## Overview

Alissa communicates with Hub317 via WebSocket at `ws://localhost:317/ws/alissa` (configurable). The protocol is message-based with clear separation between inbound (user → Alissa) and outbound (Alissa → user) contracts.

## Inbound Contract (User to Alissa)

Hub317 sends JSON messages. Alissa accepts the following formats:

### Text Input (Original)
```json
{
  "role": "user",
  "content": "hello, what can you do?",
  "userName": "jackson"
}
```

**Fields:**
- **role** (string): Always "user" for messages from the user
- **content** (string): The user's message text
- **userName** (string, optional): Username for user context injection
- **systemPrompt** (string, optional): Custom system prompt override
- **history** (array, optional): Conversation history for context

### Audio Input (NEW)
Hub317 can send audio in two ways:

#### JSON Audio Message
```json
{
  "type": "audio",
  "role": "user",
  "content": "SUQzBAAAI1NORQU...",
  "format": "wav",
  "userName": "jackson"
}
```

**Fields:**
- **type** (string): "audio" to indicate audio input
- **role** (string): Always "user"
- **content** (string): Base64-encoded WAV audio data
- **format** (string): Audio format (currently "wav" only)
- **userName** (string, optional): Username for context

#### Binary Audio (Alternative)
Alissa also accepts raw WAV data as binary WebSocket frames. Frames are automatically assembled until `EndOfMessage` is set.

**Processing:**
1. Alissa receives and decodes the audio
2. Transcribes via Whisper STT to obtain transcript text
3. Sends a `transcript` message back to Hub (see Outbound Contract below)
4. Streams the response as text
5. If TTS is enabled, sends an `audio_chunk` with the response audio

## Outbound Contract (Alissa to Hub317)

Alissa streams responses in chunks using standardized message types. When TTS is enabled, audio is also sent.

### Text Response Stream

**Stream Start**
Sent when Alissa begins generating a response:
```json
{
  "type": "stream_start",
  "role": "assistant"
}
```

**Stream Chunk**
Sent for each token in the response:
```json
{
  "type": "stream_chunk",
  "content": "Hello "
}
```

**Stream End**
Sent when the response text is complete:
```json
{
  "type": "stream_end",
  "role": "assistant"
}
```

### Transcript Notice (NEW)
**Only sent if audio input was received.** Sent immediately after transcription, before the response stream:
```json
{
  "type": "transcript",
  "role": "user",
  "content": "what can you tell me about async"
}
```

This message notifies Hub of what was heard from the audio. Hub can use this to display to the user that their speech was understood.

### Audio Response (NEW)
**Only sent if TTS is enabled.** Sent after the response stream completes, containing base64-encoded WAV audio:
```json
{
  "type": "audio_chunk",
  "role": "assistant",
  "content": "SUQzBAAAI1NORQU..."
}
```

**Fields:**
- **type** (string): "audio_chunk"
- **role** (string): "assistant"
- **content** (string): Base64-encoded WAV audio (mono or stereo, 16000 Hz recommended)

The WAV data is the synthesized speech from Piper TTS, pitched up according to config. Clients decode and queue for playback.

### Complete Message (Non-Streamed)
Sent for command results and errors:
```json
{
  "type": "message",
  "role": "assistant",
  "content": "Command executed successfully"
}
```

### Response Flow with Audio

When both STT and TTS are enabled, the order is:

1. **Audio Received** (user sends audio)
2. **transcript** message (what was heard)
3. **stream_start** (response generation begins)
4. **stream_chunk** × N (response text tokens)
5. **stream_end** (response complete)
6. **audio_chunk** (response audio, if TTS enabled)

## Configuration

### Environment Variables
- **ALISSA_HUB_URL**: Set the Hub317 WebSocket URL (default: `ws://localhost:317/ws/alissa`)

### Command-Line Arguments
- `--hub`: Activate Hub317 mode using the default URL
- `--hub=<url>`: Activate Hub317 mode with a custom URL

## Example Flow

1. Hub317 sends user message:
   ```json
   {"role": "user", "content": "explain async/await", "userName": "jackson"}
   ```

2. Alissa receives, sets user context to "jackson", and begins streaming:
   ```json
   {"type": "stream_start", "role": "assistant"}
   {"type": "stream_chunk", "content": "Async/await "}
   {"type": "stream_chunk", "content": "is a pattern "}
   ...
   {"type": "stream_end", "role": "assistant"}
   ```

## Authentication

Currently, no authentication is implemented. Hub317 is assumed to be on localhost or within a trusted network. Future enhancements may add a shared secret header for validation.

## Error Handling

If Alissa encounters an error:
1. Connection is automatically maintained (reconnect logic in HubOutputClient)
2. Error messages are logged to console but do not disrupt the stream
3. Hub317 should implement timeout logic to avoid hanging on lost connections
