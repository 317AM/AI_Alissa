# Hub317 Protocol Documentation

## Overview

Alissa communicates with Hub317 via WebSocket at `ws://localhost:317/ws/alissa` (configurable). The protocol is message-based with clear separation between inbound (user → Alissa) and outbound (Alissa → user) contracts.

## Inbound Contract (User to Alissa)

Hub317 sends JSON messages with the following structure:

```json
{
  "role": "user",
  "content": "hello, what can you do?",
  "userName": "jackson"
}
```

### Fields:
- **role** (string): Always "user" for messages from the user
- **content** (string): The user's message text
- **userName** (string, optional): Username to inject into Alissa's user context. If provided, Alissa will use this name in the conversation and prompt injection.

Additional optional fields (from Hub317's extended contract):
- **systemPrompt** (string, optional): Custom system prompt override
- **history** (array, optional): Conversation history for context

## Outbound Contract (Alissa to Hub317)

Alissa streams responses in chunks using standardized message types.

### Stream Start
Sent when Alissa begins generating a response:
```json
{
  "type": "stream_start",
  "role": "assistant"
}
```

### Stream Chunk
Sent for each token in the response:
```json
{
  "type": "stream_chunk",
  "content": "Hello "
}
```

### Stream End
Sent when the response is complete:
```json
{
  "type": "stream_end",
  "role": "assistant"
}
```

### Complete Message
Sent for non-streamed responses (e.g., command results):
```json
{
  "type": "message",
  "role": "assistant",
  "content": "Command executed successfully"
}
```

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
