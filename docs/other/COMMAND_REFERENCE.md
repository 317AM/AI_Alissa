# Command Reference

## Build Commands

```powershell
# Build entire solution
dotnet build

# Build specific project
dotnet build --project main
dotnet build --project core
dotnet build --project tests

# Clean and rebuild
dotnet clean
dotnet build

# Release build
dotnet build --configuration Release

# Check build only (don't create output)
dotnet build --no-build
```

---

## Run Commands

```powershell
# Run main application
dotnet run --project main

# Run tests
dotnet run --project tests

# Run with specific configuration
dotnet run --project main --configuration Release

# Run and capture output
dotnet run --project tests 2>&1 | Tee-Object -FilePath test-results.txt

# Run in background (if main is a server)
dotnet run --project main &
```

---

## Test Commands

```powershell
# Run all tests
dotnet run --project tests

# Run tests from command line (if using xUnit/NUnit)
dotnet test

# Run specific test class
dotnet test --filter "Category=MemoryPipeline"

# Run with verbose output
dotnet test --verbosity detailed

# List all tests
dotnet test --list-tests
```

---

## Git Commands

```powershell
# Check status
git status

# View branches
git branch -a

# Create new branch
git checkout -b feature/my-feature

# Stage changes
git add .
git add src/file.cs    # Specific file
git add -p             # Interactive staging

# Commit
git commit -m "message"
git commit -am "message"  # Stage all tracked

# Push
git push origin master
git push origin feature/my-feature

# Pull latest
git pull origin master

# View history
git log --oneline
git log --oneline -n 10

# View changes
git diff
git diff src/file.cs

# Revert changes
git checkout -- src/file.cs
git reset --hard

# View remotes
git remote -v

# Add remote
git remote add origin https://github.com/user/repo
```

---

## File Management

```powershell
# Navigate
cd "D:\OneDrive - HTBLA Leonding\Projects\AI_Alissa"

# List files
Get-ChildItem
ls -la

# Find files
Get-ChildItem -Recurse -Filter "*.cs"
find . -name "*.json"

# View file content
Get-Content config/model.json
cat config/model.json

# Edit file
notepad config/model.json
code src/file.cs  # Visual Studio Code

# Create directory
New-Item -ItemType Directory -Name "new_folder"
mkdir new_folder

# Copy file
Copy-Item source.txt destination.txt

# Remove file
Remove-Item file.txt
rm file.txt

# Search in files
Get-ChildItem -Recurse | Select-String "search term"
grep -r "search term" .
```

---

## Directory Navigation

```powershell
# Current directory
$PWD
pwd

# Change to project root
cd "D:\OneDrive - HTBLA Leonding\Projects\AI_Alissa"

# Change to subdirectory
cd core
cd core\Services
cd ..  # Parent directory

# List contents
ls
dir
Get-ChildItem

# Tree view
tree /F  # Windows
ls -R     # PowerShell
```

---

## Useful PowerShell Tips

```powershell
# Run command and capture output
$output = dotnet build 2>&1

# Run command and save to file
dotnet build > build-output.txt 2>&1

# Pipe to file
dotnet test | Out-File test-results.txt

# Show last N lines
Get-Content file.txt | Select-Object -Last 10

# Search in output
dotnet build | Select-String "error"

# Count files
(Get-ChildItem -Recurse -Filter "*.cs").Count

# Run multiple commands
dotnet clean; dotnet build; dotnet run --project tests

# Schedule delayed execution
Start-Sleep -Seconds 5; dotnet run --project main
```

---

## Configuration File Management

```powershell
# View config
Get-Content config/model.json

# Edit config (opens in default editor)
notepad config/model.json

# View formatted JSON
Get-Content config/model.json | ConvertFrom-Json | ConvertTo-Json

# Validate JSON syntax
Get-Content config/model.json | Test-Json

# Create backup
Copy-Item config/model.json config/model.json.backup

# Compare configs
Compare-Object (Get-Content config/model.json) (Get-Content config/model.json.backup)
```

---

## Monitoring & Debugging

```powershell
# Watch file changes
Get-Content -Path config/model.json -Wait

# Monitor memory usage
Get-Process dotnet | Select-Object Name, WorkingSet

# Kill process
Stop-Process -Name dotnet

# Check disk usage
Get-ChildItem -Path memory -Recurse | Measure-Object -Property Length -Sum

# View event log
Get-EventLog -LogName Application -Source "YourApp" | Select-Object -First 10

# Network info
Test-Connection localhost -Count 4
Get-NetTCPConnection -State Listen | Where-Object LocalPort -eq 11434
```

---

## Development Workflow

```powershell
# Start fresh
git status
git pull origin master

# Build and test
dotnet clean
dotnet build
dotnet run --project tests

# Make changes (edit files)
code src/file.cs

# Verify changes
dotnet build
dotnet run --project tests

# Commit and push
git add .
git commit -m "Add feature X"
git push origin master
```

---

## Quick Aliases

```powershell
# Add to PowerShell profile ($PROFILE)

# Build and test
function Test-All { dotnet clean; dotnet build; dotnet run --project tests }

# Run main app
function Start-Alissa { dotnet run --project main }

# View recent log
function Show-Logs { Get-ChildItem logs -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 5 }

# Status check
function Check-Status { 
	Write-Host "Building..."
	dotnet build
	Write-Host "Testing..."
	dotnet run --project tests
}

# Save profile
Add-Content -Path $PROFILE -Value "`nfunction Show-Memory { Get-ChildItem memory -Recurse -Force | Measure-Object -Property Length -Sum }"
```

---

## Memory/Storage Commands

```powershell
# Check memory folder size
(Get-ChildItem -Path memory -Recurse -Force | Measure-Object -Property Length -Sum).Sum / 1GB

# List memory files
Get-ChildItem -Path memory -Recurse

# Backup memory
Copy-Item memory memory.backup -Recurse

# Clear medium-term memory
Remove-Item memory/medium_term/*.json

# Clear session cache
Remove-Item memory/short_term/session_cache.json

# Archive old conversations
Move-Item logs/conversations logs/archive/
```

---

## Useful Keyboard Shortcuts (Visual Studio)

```
Ctrl+B          Build solution
Ctrl+Shift+B    Rebuild solution
F5              Start debugging
Ctrl+F5         Start without debugging
Ctrl+.          Quick actions / fixes
Ctrl+K Ctrl+D   Format document
Ctrl+H          Find and replace
Ctrl+F          Find
Ctrl+G          Go to line
F12             Go to definition
Ctrl+F12        Go to implementation
Shift+F12       Find all references
```

---

## Docker Commands (If Using Docker)

```powershell
# Build image
docker build -t alissa .

# Run container
docker run -it alissa

# List containers
docker ps -a

# Remove container
docker rm container-id

# View logs
docker logs container-id

# Execute command in running container
docker exec -it container-id bash
```

---

## Environment Variables

```powershell
# View environment variable
$env:PATH

# Set environment variable (temporary)
$env:ASPNETCORE_ENVIRONMENT = "Production"

# Set permanently (requires restart)
[System.Environment]::SetEnvironmentVariable("VAR_NAME", "value", [System.EnvironmentVariableTarget]::User)

# View all environment variables
Get-ChildItem env: | Sort-Object Name
```

---

## Troubleshooting Commands

```powershell
# Check if port is in use
Get-NetTCPConnection -LocalPort 11434

# Verify Ollama is running
Test-Connection localhost:11434

# Check disk space
Get-Volume C

# View recent errors
$error | Select-Object -First 5

# Clear error history
$error.Clear()

# Run tests with debug output
dotnet run --project tests -- --verbose

# Build with verbose output
dotnet build --verbosity detailed
```

---

## One-Liners for Common Tasks

```powershell
# Count C# files
(Get-ChildItem -Recurse -Filter "*.cs").Count

# Count lines of code
(Get-ChildItem -Recurse -Filter "*.cs" | Get-Content | Measure-Object -Line).Lines

# Find TODO comments
Get-ChildItem -Recurse -Filter "*.cs" | Select-String "TODO|FIXME"

# List all classes
Get-ChildItem -Recurse -Filter "*.cs" | Select-String "^[[:space:]]*public class" | Select-Object -Unique

# Backup entire project
Copy-Item . (Get-Date -Format "backup-yyyy-MM-dd-HH-mm-ss") -Recurse

# Clean build artifacts
Get-ChildItem -Recurse -Include "bin", "obj" | Remove-Item -Recurse

# Update file timestamps
Get-ChildItem -Recurse | ForEach-Object { $_.LastWriteTime = Get-Date }
```

---

## Quick Reference Terminal Setup

```powershell
# Create convenient shortcut in PowerShell profile
# Edit profile: notepad $PROFILE

# Add these to $PROFILE:
function cd-alissa { Set-Location "D:\OneDrive - HTBLA Leonding\Projects\AI_Alissa" }
function build { dotnet build }
function test-all { dotnet run --project tests }
function run-alissa { dotnet run --project main }
function view-config { Get-Content config/model.json | ConvertFrom-Json }
function view-logs { Get-ChildItem logs -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 5 FullName }
function memory-size { (Get-ChildItem memory -Recurse -Force | Measure-Object -Property Length -Sum).Sum / 1MB }

# Reload profile
. $PROFILE
```

---

**For more help, run**:
```powershell
Get-Help dotnet
Get-Help dotnet run -Examples
Get-Help git
Get-Help Get-ChildItem -Full
```

---

**Last Updated**: 2024
**Tested On**: Windows 11, PowerShell 7.x, .NET 10

Happy coding! 🐱
