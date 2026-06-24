# Video Analysis Protocol Documentation

## Overview

The Video Analysis feature allows external systems to send video files or individual frames to Alissa for analysis. Alissa samples frames, runs them through a vision model (Ollama with llava or similar), and injects the descriptions into the conversation context.

**Important**: This feature is Linux-server-ready and does NOT depend on desktop screen capture. It receives video from external sources via HTTP.

## Endpoints

### POST /api/video
Accepts a video file upload and returns an analysis.

**Request**:
- **Content-Type**: `multipart/form-data`
- **Body**: Binary video file (mp4, webm, mkv, etc.)
- **Max size**: 100MB

**Response**:
```json
{
  "status": "success",
  "analysis": "Frame 1: Microsoft Visual Studio is open with C# code for a payment service... Frame 2: Three browser tabs are visible showing documentation..."
}
```

**Error Response**:
```json
{
  "status": "error",
  "message": "ffmpeg not found in system PATH"
}
```

### POST /api/frame
Accepts a single frame (JPEG/PNG bytes) and returns a description.

**Request**:
- **Content-Type**: `image/jpeg` or `image/png`
- **Body**: Raw image bytes
- **Max size**: 10MB

**Response**:
```json
{
  "status": "success",
  "description": "A desk workspace with dual monitors. Left monitor shows IntelliJ IDEA with Java code. Right monitor displays a terminal with build output."
}
```

## Configuration

Add `config/video_analysis.json`:

```json
{
  "enabled": false,
  "visionModelName": "llava:7b",
  "frameIntervalSeconds": 5,
  "maxFramesPerVideo": 10,
  "frameQuality": 75,
  "tempDirectory": "temp/frames",
  "injectIntoPrompt": true
}
```

### Fields:
- **enabled**: Whether to activate video analysis
- **visionModelName**: Ollama model name (llava, moondream, bakllava, etc.)
- **frameIntervalSeconds**: Sample 1 frame per N seconds
- **maxFramesPerVideo**: Limit frames extracted from long videos
- **frameQuality**: JPEG quality (1-100)
- **tempDirectory**: Where to temporarily store extracted frames
- **injectIntoPrompt**: If true, automatically inject analyses into the prompt

## CLI Usage

Start Alissa with the optional video receiver:

```bash
dotnet run --project main -- --video-port=8080
```

This enables the HTTP endpoints on `http://localhost:8080/api/video` and `/api/frame`.

## How It Works

1. **Frame Extraction**: Uses `ffmpeg` to sample frames at the configured interval
2. **Vision Model Call**: Each frame is converted to base64 and sent to Ollama
3. **Analysis Combination**: Per-frame descriptions are combined into a paragraph
4. **Prompt Injection**: If enabled, the analysis is injected as "## Screen Context" in the system prompt
5. **Temporary Cleanup**: All extracted frames are deleted after processing

## Dependencies

- **ffmpeg** and **ffprobe**: Required on system PATH
- **Ollama**: Running with a vision model (llava:7b recommended)
- **ASP.NET Core** (if using the optional HTTP server)

## Error Handling

- If ffmpeg is missing, VideoAnalysisService logs a warning and returns an empty analysis
- Vision model failures are caught and returned as error descriptions
- Failed frame processing does not halt the entire video analysis

## Security Considerations

- Video files are temporarily stored in disk; consider privacy implications
- No input sanitization is performed on video content
- Recommend running on localhost or within a trusted network
- Future versions may add authentication via shared secret header

## Example Integration

```csharp
var videoService = new VideoAnalysisService(
	basePath: "/path/to/alissa",
	chatClient: ollamaClient,
	visionModelName: "llava:7b",
	frameIntervalSeconds: 5);

// Analyze video from a file
using (var videoStream = File.OpenRead("recording.mp4"))
{
	string analysis = await videoService.AnalyzeVideoAsync(videoStream, "video/mp4", ct);
	Console.WriteLine(analysis);
}

// Or analyze a single frame
byte[] frameData = File.ReadAllBytes("screenshot.png");
string frameAnalysis = await videoService.AnalyzeFrameAsync(frameData, "Current state");
```
