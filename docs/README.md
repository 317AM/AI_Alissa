# AI_Alissa

A modular, local AI assistant built with .NET and Ollama focused on **clean architecture**, **config-driven behavior**, and **local-first AI experimentation**.

---

## Features
- **Pluggable model configuration** — choose models via JSON.  
- **Persistent sessions** — conversations are saved to disk.  
- **Extendable memory system** — modular memory manager for long-term context.  
- **Structured error handling** — centralized error recording and logs.  
- **Modular service architecture** — clear separation between core services and runtime.  
- **Local-first experimentation** — run models and tooling locally for privacy and speed.

---

## Project Structure
- **Alissa.Core** — Business logic, services, models, utilities.  
- **Alissa.Main** — Runtime entry point and CLI loop.  
- **Alissa.Tests** — Diagnostic and integration test runner.  

Key folders:
- `/config` — runtime configuration files (`model.json`, `limits.json`, `settings.json`).  
- `/data` — persisted session and auxiliary data.  
- `/logs` — `conversations`, `summaries`, `errors`.  
- `/memory` — memory entries and preference files.  
- `/personality` — persona text files (behaviour, identity, boundaries).  
- `/docs` — generated documentation (structure, configs, personality).  
- `/scripts` — helper scripts for updating docs and cleaning logs.

---

## Configuration
All runtime behavior is driven by JSON files in `/config`. The app expects these files to exist and contain valid JSON.

**Files**
- `model.json` — model selection and model-specific options.  
- `limits.json` — numeric limits and timeouts.  
- `settings.json` — feature toggles (e.g., summaries enabled).

**Example `model.json`**
```json
{
  "ModelName": "qwen3:8b"
}
```

**Example `limits.json`**
```json
{
  "KeepAliveMinutes": 10,
  "MaxTokens": 1024,
  "SummaryDivisionFactor": 10
}
```

**Example `settings.json`**
```json
{
  "EnableSummaries": true
}
```

---

## Data Storage
- **Conversations** — `/logs/conversations` as `conversation_YYYY-MM-DD_HH-mm-ss.txt`.  
- **Summaries** — `/logs/summaries` as `summary_YYYY-MM-DD_HH-mm-ss.txt`.  
- **Memory** — `/memory` as JSON entries for long-term context.  
- **Personality** — `/personality/*.txt` merged into `docs/personality.md` by scripts.

---

## Scripts
- `scripts/update_configs.ps1` / `update_configs.bat` — generate `docs/configs.md` from `/config`.  
- `scripts/update_personality.ps1` / `update_personality.bat` — generate `docs/personality.md` from `/personality`.  
- `scripts/clean_logs.ps1` — interactive log cleanup by date range.  
- `scripts/update_structure.ps1` / `update_structure.bat` — generate `docs/structure.md` from project tree.

---

## Getting Started

### Prerequisites
- .NET SDK (compatible with the project target, e.g., .NET 10).  
- Ollama or another local model runtime reachable at `http://localhost:11434`.  
- Clone repository to a writable path (example paths below use Windows).

### Quick setup (Windows copy/paste)
1. Open PowerShell or a terminal and clone the repo:
```powershell
git clone https://your-repo-url.git "D:\OneDrive - HTBLA Leonding\Projects\AI_Alissa"
cd "D:\OneDrive - HTBLA Leonding\Projects\AI_Alissa"
```
2. Ensure `/config` contains the three JSON files shown above. Example create `model.json`:
```powershell
@'
{
  "ModelName": "qwen3:8b"
}
'@ | Out-File -FilePath .\config\model.json -Encoding utf8
```
3. Build the solution:
```powershell
dotnet build
```
4. Run the main app:
```powershell
dotnet run --project main/Alissa.Main.csproj
```
5. Use the console to interact. Type `exit` to quit. Conversation and summary files will be saved under `/logs`.

### Regenerate docs
- Update `docs/configs.md` from `/config`:
```powershell
.\scripts\update_configs.ps1
```
- Update `docs/personality.md` from `/personality`:
```powershell
.\scripts\update_personality.ps1
```

---

## Extending the Project
- **Add a model adapter**: implement `IChatClient` and register it in the runtime.  
- **Extend memory**: implement `IMemoryManager` and wire it into `AlissaClient`.  
- **Persona editing**: edit `/personality/*.txt` and run the update script to regenerate `docs/personality.md`.

---

## Roadmap
- Advanced memory indexing and retrieval.  
- Semantic memory compression and summarization.  
- Persona auto-merge and conflict resolution.  
- Improved test diagnostics and CI integration.

---

## Contributing
Follow the existing clean-architecture patterns in `Alissa.Core`. Open issues for feature requests and submit PRs for review. Keep changes modular and configuration-driven.

---
