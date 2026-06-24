# VS Extension Design Documentation

## Overview

This document describes the design and architecture for a Visual Studio extension that integrates with Alissa for real-time code assistance.

**Status**: Design only. Implementation is future work.

## Motivation

A VS Extension enables:
1. **Automatic code context injection** when files are opened/changed
2. **Real-time error reporting** to Alissa for faster debugging
3. **One-click assistance** without leaving the IDE (Ask Alissa commands)
4. **Bidirectional communication** for advanced workflows

## Architecture

```
┌──────────────────────────────────────────────┐
│  Visual Studio IDE                           │
│  ┌────────────────────────────────────────┐  │
│  │  Alissa Extension                      │  │
│  │  - File change listener                │  │
│  │  - Error listener                      │  │
│  │  - Build event listener                │  │
│  │  - Output panel                        │  │
│  └────────────────────────────────────────┘  │
└───────────────┬──────────────────────────────┘
				│ HTTP POST to /api/code
				▼
┌──────────────────────────────────────────────┐
│  Alissa Server                               │
│  ┌────────────────────────────────────────┐  │
│  │  Code API Endpoint (/api/code)         │  │
│  │  - CodeContextMessage handler          │  │
│  │  - PersonaService.UpdateCurrentCode    │  │
│  │  - CommandService for tasks            │  │
│  └────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────┐  │
│  │  PromptBuilder                         │  │
│  │  - Injects current_code into ## User   │  │
│  │    Profile section                     │  │
│  └────────────────────────────────────────┘  │
└──────────────────────────────────────────────┘
```

## Extension Features

### 1. File Change Listener
- Detects when user opens/closes/changes files
- Sends CodeContextMessage with type="code_context"
- Updates Alissa's persona.json with current file info

### 2. Error Listener
- Listens for compilation errors from the IDE
- Sends CodeContextMessage with type="error_context"
- Includes errorMessage field
- Optional: User can ask "Alissa, fix this error" from the Error List

### 3. Build Event Listener
- Listens for build completion
- Sends build output to Alissa (type="build_result")
- Useful for: "Why did my build fail?"

### 4. Ask Alissa Commands
Available from context menus:

- **Ask Alissa: Explain** - Send selected code with task="explain"
- **Ask Alissa: Fix** - Send error context with task="fix"
- **Ask Alissa: Review** - Send selected code with task="review"
- **Ask Alissa: Generate Tests** - Send selected code with task="test"
- **Ask Alissa: Refactor** - Send selected code with task="refactor"

### 5. Output Panel
- Displays Alissa's responses inline
- Shows code suggestions (if provided as diff-style suggestions)
- Allows quick copy/apply operations

## Message Flow Examples

### Example 1: User Opens a File
1. User opens `PaymentProcessor.cs`
2. Extension detects file open event
3. Reads file content and sends:
   ```json
   {
	 "type": "code_context",
	 "filePath": "..\\Services\\PaymentProcessor.cs",
	 "language": "csharp",
	 "projectName": "PaymentService",
	 "solutionPath": "C:\\Projects\\PaymentService.sln"
   }
   ```
4. Alissa updates persona.json:
   ```json
   {
	 "current_code": {
	   "name": "PaymentProcessor.cs",
	   "language": "csharp"
	 }
   }
   ```
5. User asks: "Explain this file"
6. Alissa's response includes natural references to `PaymentProcessor.cs`

### Example 2: Build Fails
1. User runs build; compilation fails
2. Extension captures error: "CS0246: 'PaymentRequest' not found"
3. User right-clicks error in Error List → "Ask Alissa: Fix"
4. Extension sends:
   ```json
   {
	 "type": "error_context",
	 "errorMessage": "CS0246: 'PaymentRequest' not found in current context",
	 "filePath": "..\\Services\\PaymentProcessor.cs",
	 "selectedText": "var request = new PaymentRequest();"
   }
   ```
5. Alissa suggests adding `using MyApp.Models;`

### Example 3: Code Review
1. User selects a method
2. Right-clicks → "Ask Alissa: Review"
3. Extension sends:
   ```json
   {
	 "type": "code_request",
	 "task": "review",
	 "selectedText": "[selected method code]",
	 "filePath": "...",
	 "language": "csharp"
   }
   ```
4. Alissa returns review comments and suggestions

## Implementation Phases

### Phase 1: Minimal (MVP)
- File open/change listener only
- Static "Ask Alissa" command in context menu
- Manual input field for questions

### Phase 2: Enhanced
- Error listener integration
- Build event listener
- Auto-send build failures
- Output panel with better formatting

### Phase 3: Advanced
- Diff-based code suggestion application
- Conversation history in output panel
- Refactoring preview
- Test generation with quick-apply

## Configuration

User can configure in VS:
- Alissa server URL (default: http://localhost:31700)
- Enable/disable auto-context updates
- Enable/disable error reporting
- Output panel position
- Response formatting preferences

## Security & Privacy

- **Data**: Code is sent as plain text to the local Alissa server
- **Recommendation**: Run Alissa on localhost only
- **HTTPS Future**: Support HTTPS for remote Alissa servers
- **Sensitive Info**: Users should be aware that proprietary code is sent to the AI model

## Technology Stack

- **Language**: C# (.NET)
- **VS SDK**: Visual Studio Extensibility SDK
- **HTTP Client**: System.Net.Http
- **JSON**: System.Text.Json

## Example Minimal Extension Skeleton

```csharp
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
[ProvideMenuResource("Menus.ctmenu", 1)]
public sealed class AlissaExtensionPackage : AsyncPackage
{
	private DocumentEventsListener? _documentListener;
	private IVsStatusbar? _statusBar;

	protected override async Task InitializeAsync(
		CancellationToken cancellationToken,
		IProgress<ServiceProgressData> progress)
	{
		await base.InitializeAsync(cancellationToken, progress);

		// Initialize listeners
		var dte = await GetServiceAsync(typeof(DTE)) as DTE;
		_documentListener = new DocumentEventsListener(dte);

		// Register command handler
		var commandService = 
			await GetServiceAsync(typeof(IMenuCommandService)) 
			as OleMenuCommandService;
		RegisterCommands(commandService);
	}

	private void RegisterCommands(OleMenuCommandService? mcs)
	{
		if (mcs == null) return;

		// "Ask Alissa: Explain" command
		var explainCmd = new CommandID(
			new Guid("12345678-1234-1234-1234-123456789012"), 0x0100);
		var menuItem = new MenuCommand(
			(s, e) => OnAskAlissaExplain(), explainCmd);
		mcs.AddCommand(menuItem);
	}

	private async void OnAskAlissaExplain()
	{
		// Get selected code
		// Send to /api/code with task="explain"
		// Display result in output panel
	}
}
```

## Notes for Developers

- Use async/await throughout to avoid blocking VS UI
- Cache file content to avoid repeated reads
- Implement exponential backoff for Alissa server connection retries
- Show connection status in status bar
- Log all HTTP errors for debugging
- Consider rate limiting to avoid overwhelming Alissa
