# Alissa Speech Vendor Directory

This directory contains local binaries and models for offline speech-to-text (Whisper) and text-to-speech (Piper) services.
**Binaries and models are NOT committed to the repository.** They must be downloaded locally.

## Directory Structure

### `whisper/`
Offline Whisper speech-to-text (OpenAI Whisper via whisper.cpp):
- `whisper-cli.exe` — Compiled whisper.cpp CLI for Windows
- `models/` — Whisper GGML models
  - `ggml-base.en.bin` — English base model (recommended)
  - Other `.bin` or `.gguf` files (optional alternatives)

**Download:**
- Whisper binary: [whisper.cpp releases](https://github.com/ggerganov/whisper.cpp/releases)
- Models: [whisper.cpp models](https://huggingface.co/ggerganov/whisper.cpp/tree/main)

### `piper/`
Offline Piper text-to-speech (Rhasspy Piper):
- `piper.exe` — Compiled Piper CLI for Windows
- `*.dll` — Runtime dependencies (if needed by the binary)
- `espeak-ng-data/` — eSpeak-NG phoneme data (required by Piper)
- `voices/` — Voice models
  - `en_US-amy-medium.onnx` — English female voice (recommended)
  - `en_US-amy-medium.onnx.json` — Metadata (must accompany .onnx)
  - Other `.onnx` + `.onnx.json` pairs (optional alternatives)

**Download:**
- Piper binary + espeak-ng-data: [Piper releases](https://github.com/rhasspy/piper/releases)
- Voice models: [Piper voices](https://huggingface.co/rhasspy/piper-voices/tree/main)

### `temp/audio/`
Temporary audio files generated during synthesis. Safe to delete anytime.

## Configuration

Speech settings are in `config/speech.json`:
- `sttEnabled` / `ttsEnabled` — Enable/disable features (default: false)
- `whisperBinaryPath` / `whisperModelPath` — Paths to Whisper binary and model
- `piperBinaryPath` / `piperVoiceModelPath` — Paths to Piper binary and voice
- `tempDirectory` — Where temp WAV files go
- `processTimeoutSeconds` — Timeout for process execution (default: 120 seconds)

Paths are relative to the project root and resolved at startup.

## Notes

- Both services run as external processes and require their binaries to be present locally.
- Whisper models are GGML-quantized `.bin` files (not `.gguf`).
- Piper requires the `.onnx.json` metadata file to exist next to each `.onnx` voice model.
- No cloud APIs, Python, or NuGet speech packages are used.
- Speech services are disabled by default for security and performance.
