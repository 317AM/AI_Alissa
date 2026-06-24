# VS Extension Code Protocol Documentation

## Overview

The Code Communication Protocol enables Visual Studio extensions (and other external tools) to send code context to Alissa via HTTP. This allows Alissa to be aware of the code the user is working on and provide context-aware assistance.

## Endpoint

### POST /api/code
Accepts code context information and optionally processes a task.

**Content-Type**: `application/json`

**Request Body** (CodeContextMessage):
```json
{
  "type": "code_context",
  "filePath": "C:\\Projects\\MyApp\\Services\\PaymentService.cs",
  "language": "csharp",
  "selectedText": "public async Task<bool> ProcessPaymentAsync(PaymentRequest request) { ... }",
  "fullFileContent": "using System; ... namespace MyApp.Services { ... }",
  "task": "explain",
  "errorMessage": "",
  "buildOutput": "",
  "projectName": "MyApp",
  "solutionPath": "C:\\Projects\\MyApp\\MyApp.sln"
}
```

**Response**:
```json
{
  "status": "success",
  "message": "Code context updated. Current file: PaymentService.cs (C#)",
  "taskResult": null
}
```

If `task` is set to a processing task:
```json
{
  "status": "success",
  "message": "Code context updated.",
  "taskResult": "Here's an explanation of your method..."
}
```

## CodeContextMessage Fields

### Required Fields:
- **type** (string): Message type. Values: "code_context", "code_request", "error_context", "build_result"
- **filePath** (string): Absolute path to the file
- **language** (string): Programming language ("csharp", "python", "javascript", etc.)

### Optional Fields:
- **selectedText** (string): Currently selected text in the editor
- **fullFileContent** (string): Complete file content
- **task** (string): Task to process. Values: "explain", "fix", "review", "complete", "test", "refactor"
- **errorMessage** (string): Error message if type is "error_context"
- **buildOutput** (string): Build output if type is "build_result"
- **projectName** (string): Name of the project
- **solutionPath** (string): Path to the solution file
- **timestamp** (string, ISO 8601): When the message was created

## Message Types

### "code_context"
Sent when the user opens a file or changes selection. Updates Alissa's awareness of the current file.

### "code_request"  
Sent when the user explicitly asks for help with code. Should include a `task`.

### "error_context"
Sent when a compilation or runtime error occurs. Includes `errorMessage`.

### "build_result"
Sent after a build completes. Includes `buildOutput`.

## Tasks

When a task is specified, Alissa processes it and returns a result:

- **explain**: Describe the selected code or file
- **fix**: Generate a fix for the current error or issue
- **review**: Perform a code review on the selected code
- **complete**: Generate code completion or suggestions
- **test**: Generate unit tests for the selected code
- **refactor**: Suggest refactoring improvements

## Configuration

Add `config/vs_extension.json`:

```json
{
  "enabled": false,
  "listenPort": 31700,
  "allowedHosts": ["127.0.0.1", "::1"],
  "autoUpdateCodeContext": true,
  "sharedSecretHeader": ""
}
```

### Fields:
- **enabled**: Whether the code endpoint is active
- **listenPort**: HTTP server port for the endpoint
- **allowedHosts**: Allowed client IPs (empty = localhost only)
- **autoUpdateCodeContext**: Automatically update persona.json when code context changes
- **sharedSecretHeader**: Shared secret for authentication (optional, empty = no auth)

## CLI Usage

Start Alissa with the optional code endpoint:

```bash
dotnet run --project main -- --code-port=31700
```

This enables the HTTP endpoint at `http://localhost:31700/api/code`.

## Example Integration (VS Extension)

```csharp
// When user opens/changes file
var codeMessage = new CodeContextMessage
{
	Type = "code_context",
	FilePath = activeEditor.Document.FilePath,
	Language = "csharp",
	SelectedText = activeEditor.SelectedText,
	FullFileContent = activeEditor.Document.Text,
	ProjectName = project.Name,
	SolutionPath = solution.FullName
};

using (var client = new HttpClient())
{
	var json = JsonSerializer.Serialize(codeMessage);
	var content = new StringContent(json, Encoding.UTF8, "application/json");
	var response = await client.PostAsync("http://localhost:31700/api/code", content);
	var result = await response.Content.ReadAsStringAsync();
	// Show result in Alissa panel...
}
```

## Authentication

### No Authentication (Default)
- Endpoint is open to any client
- Recommended for localhost-only setups

### Shared Secret (Future)
- Set `sharedSecretHeader` in vs_extension.json
- Extension includes header: `X-Alissa-Secret: <secret>`
- Alissa validates before processing

## Persona Update Flow

When `autoUpdateCodeContext: true`:

1. Extension sends code context
2. Alissa's PersonaService.UpdateCurrentCodeAsync is called
3. `persona.json` current_code section is updated:
   ```json
   {
	 "current_code": {
	   "name": "PaymentService.cs",
	   "language": "csharp",
	   "task": "explain",
	   "snippet": "...",
	   "related_concepts": ["async/await", "dependency injection"]
	 }
   }
   ```
4. PromptBuilder loads this on the next prompt generation
5. Alissa naturally references the code in subsequent messages

## Error Handling

Common error responses:

```json
{
  "status": "error",
  "message": "Code endpoint not enabled"
}
```

```json
{
  "status": "error",
  "message": "Invalid authentication"
}
```

```json
{
  "status": "error",
  "message": "Task processing failed: file not found"
}
```

## Security Considerations

- Default: localhost only (set allowedHosts to restrict further)
- File paths are not validated; extension must ensure safe paths
- No rate limiting is implemented
- Consider HTTPS in future versions for remote use
- Shared secret should be sufficiently random and protected
