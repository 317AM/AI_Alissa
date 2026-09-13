#!/usr/bin/env python3
"""
StyleTTS2 FastAPI sidecar server.
Provides HTTP endpoints for text-to-speech synthesis.
Loads model once at startup; handles requests with configurable phonemizer, diffusion, and style parameters.
"""

import os
import sys
import json
import torch
import torchaudio
import io
from pathlib import Path
from fastapi import FastAPI, HTTPException
from fastapi.responses import Response
from pydantic import BaseModel
from typing import Optional
import uvicorn

# Get paths from environment or defaults
STYLETTS2_ROOT = Path(__file__).parent.resolve()
STYLETTS2_REPO = os.getenv("STYLETTS2_REPO", str(STYLETTS2_ROOT / "repo"))
STYLETTS2_CHECKPOINT = os.getenv("STYLETTS2_CHECKPOINT", str(STYLETTS2_ROOT / "models" / "LJSpeech" / "epoch_2nd_00100.pth"))
STYLETTS2_CONFIG = os.getenv("STYLETTS2_CONFIG", str(STYLETTS2_ROOT / "models" / "LJSpeech" / "config.yml"))
PHONEMIZER_ESPEAK_LIBRARY = os.getenv("PHONEMIZER_ESPEAK_LIBRARY", "")

# Set espeak path if provided
if PHONEMIZER_ESPEAK_LIBRARY and os.path.exists(PHONEMIZER_ESPEAK_LIBRARY):
    os.environ["PHONEMIZER_ESPEAK_LIBRARY"] = PHONEMIZER_ESPEAK_LIBRARY

sys.path.insert(0, STYLEETS2_REPO)

# Now import from the repo
try:
    from models import StyleTTS2
    from utils import get_phoneme_processor
except ImportError as e:
    print(f"[ERROR] Failed to import StyleTTS2 models: {e}")
    print(f"Expected repo at: {STYLETTS2_REPO}")
    print(f"PYTHONPATH entries: {sys.path[:3]}")
    sys.exit(1)

# Initialize FastAPI app
app = FastAPI(title="StyleTTS2 TTS Sidecar", version="1.0.0")

# Global state
device = "cuda" if torch.cuda.is_available() else "cpu"
model = None
config = None
phoneme_processor = None
checkpoint_path = None
config_path = None

def load_model():
    """Load StyleTTS2 model and config once at startup."""
    global model, config, phoneme_processor, checkpoint_path, config_path

    # Paths from environment
    checkpoint_path = Path(STYLETTS2_CHECKPOINT)
    config_path = Path(STYLETTS2_CONFIG)

    if not checkpoint_path.exists():
        raise RuntimeError(f"Checkpoint not found: {checkpoint_path}")
    if not config_path.exists():
        raise RuntimeError(f"Config not found: {config_path}")

    print(f"[StyleTTS2] Loading checkpoint: {checkpoint_path}")
    print(f"[StyleTTS2] Loading config: {config_path}")
    print(f"[StyleTTS2] Repo: {STYLETTS2_REPO}")
    print(f"[StyleTTS2] Device: {device}")

    try:
        # Load config
        import yaml
        with open(config_path) as f:
            config = yaml.safe_load(f)

        # Initialize model
        model = StyleTTS2(config).to(device)
        model.load_pretrained(checkpoint_path, device)
        model.eval()

        # Initialize phoneme processor
        phoneme_processor = get_phoneme_processor("english", "espeak")

        print(f"[StyleTTS2] Model loaded successfully")
    except Exception as e:
        print(f"[ERROR] Failed to load model: {e}")
        import traceback
        traceback.print_exc()
        raise

@app.on_event("startup")
async def startup_event():
    """Load model on startup."""
    try:
        load_model()
    except Exception as e:
        print(f"[ERROR] Startup failed: {e}")
        raise

@app.get("/health")
async def health():
    """Health check endpoint."""
    return {
        "ok": model is not None,
        "device": device
    }

class SynthesizeRequest(BaseModel):
    """Request body for synthesis."""
    text: str
    speed: float = 1.2
    alpha: float = 0.3
    beta: float = 0.7
    diffusion_steps: int = 8
    embedding_scale: float = 1.2

@app.post("/synthesize")
async def synthesize(request: SynthesizeRequest):
    """Synthesize audio from text and return WAV bytes at 24 kHz."""
    if model is None:
        raise HTTPException(status_code=503, detail="Model not loaded")

    text = request.text.strip()
    if not text:
        raise HTTPException(status_code=400, detail="Text is empty")

    try:
        # Get phonemes
        phonemes = phoneme_processor(text)

        # Synthesize
        with torch.no_grad():
            wav = model.synthesize(
                phonemes,
                speed=request.speed,
                alpha=request.alpha,
                beta=request.beta,
                diffusion_steps=request.diffusion_steps,
                embedding_scale=request.embedding_scale
            )

        # Convert to bytes (24 kHz WAV)
        wav_io = io.BytesIO()
        torchaudio.save(wav_io, wav.cpu().unsqueeze(0), sample_rate=24000, format="wav")
        wav_bytes = wav_io.getvalue()

        return Response(content=wav_bytes, media_type="audio/wav")

    except Exception as e:
        print(f"[ERROR] Synthesis failed: {e}")
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))

def main():
    """Run the sidecar server (127.0.0.1:8020 only)."""
    print("[StyleTTS2] Starting HTTP sidecar on 127.0.0.1:8020")
    uvicorn.run(
        app,
        host="127.0.0.1",
        port=8020,
        log_level="warning",
        access_log=False
    )

if __name__ == "__main__":
    main()
