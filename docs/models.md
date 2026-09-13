# Alissa Model Configuration Guide

## Token Budget for Reasoning Models

When using reasoning models like **qwen3:14b** or **deepseek-r1**, the `MaxTokens` setting in `config/model.json` controls the ceiling for **total generated tokens**, not just the visible response.

### How Reasoning Models Spend Tokens

Reasoning models produce **internal reasoning text** before generating their visible answer:

1. **Invisible Reasoning Phase**: The model generates internal thoughts/reasoning to analyze the user's input and plan its response. This can consume **800-1500+ tokens** before any visible text appears.
2. **Visible Response Phase**: Only what remains of the token budget goes to the actual reply the user sees.

### Token Budget Example

With `MaxTokens: 4096`:
- Reasoning model uses ~1000 tokens on internal reasoning
- ~3000 tokens remain for the visible response
- User sees the final ~3000 tokens of response text

### Configuring MaxTokens

For **reasoning models** (like qwen3:14b):
- **Recommended minimum**: `4096` tokens
- **Recommended optimal**: `6000-8000` tokens

For **non-reasoning models** (like mistral, llama):
- **Recommended**: `2048-4096` tokens

### Temperature Setting

The `Temperature` field (default `0.7`) controls response randomness:
- **Lower values** (0.1-0.3): More deterministic, focused responses
- **Mid values** (0.5-0.8): Balanced creativity and consistency (default)
- **Higher values** (0.9-1.0): More creative, varied responses

### EnableThinking Setting

The `EnableThinking` flag controls whether to send `"think": true` to Ollama when the model supports native thinking separation.

- **`true`**: Sends the think flag; reasoning is separated into a dedicated `"thinking"` field
- **`false`**: Relies on inline `<think>...</think>` tag stripping if the model emits reasoning anyway

## Configuration Example

```json
{
  "ModelName": "qwen3:14b",
  "MaxTokens": 4096,
  "Temperature": 0.7,
  "ResponseTimeoutSeconds": 60,
  "KeepAliveMinutes": 30,
  "EnableThinking": true
}
```

## How Alissa Handles Thinking

Alissa automatically:
1. **Captures** native thinking from models that support it
2. **Strips** inline `<think>...</think>` tags from visible responses
3. **Stores** captured reasoning in the ThoughtService for internal use
4. **Never displays** raw thinking text to the user

This ensures a clean, professional user experience while preserving the model's reasoning for internal learning and memory.
