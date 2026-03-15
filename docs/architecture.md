# Overview

AI_Alissa is a modular, layered .NET application designed around:

Separation of concerns
Service abstraction
Config-driven behavior
File-based persistence
Extensible memory system

The solution is split into three main projects:

Alissa.Core     → Domain logic + interfaces
Alissa.Main     → Runtime / CLI entry point
Alissa.Tests    → Diagnostic & debug runner


# Project Structure

## 1️ Alissa.Core

Contains all reusable logic and abstractions.

Interfaces:
- IChatClient
- IMemoryManager
- IPromptBuilder
- ISessionManager

Services:
- AlissaClient
- OllamaClient
- MemoryManager
- PromptBuilder
- SessionManager
- ConfigService
- BasePathService
- SaveConversation
- RepairService
- ErrorHandler

Models:
- AppConfig
- ConfigModel
- LimitsModel
- SettingsModel
- MemoryEntry
- Message
- Session
- ErrorResult

## 2️ Alissa.Main

Contains:
- Program.cs
- Dependency initialization
- Service wiring
- Minimal runtime logic

Note: keep this file clean and declarative.

## 3️ Alissa.Tests

Lightweight test runner for:
- Service verification
- Model validation
- Error behavior inspection
- Verbose debugging output

# Configuration System

Located in /config:

- model.json
- limits.json
- settings.json

Loaded via ConfigService.

Configuration is split intentionally:

| File          | Responsibility               |
| ------------- | ---------------------------- |
| model.json    | AI model selection           |
| limits.json   | Token, memory, summary rules |
| settings.json | Behavior flags               |
