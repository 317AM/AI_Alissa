# clean_logs.ps1
#
# Usage: run the script and follow prompts for start and end date (YYYY-MM-DD).
# The script will show the files that would be deleted and ask for confirmation.

$logsDirectory = 'D:\OneDrive - HTBLA Leonding\Projects\AI_Alissa\logs'

if (-not (Test-Path -Path $logsDirectory)) {
    Write-Host
    Write-Host "Logs directory not found: $logsDirectory"
    exit 1
}

function Read-DateInput([string]$prompt) {
    while ($true) {
        $input = Read-Host $prompt
        if ([string]::IsNullOrWhiteSpace($input)) {
            Write-Host
            Write-Host "Input cannot be empty. Please enter a date in format YYYY-MM-DD."
            continue
        }

        # Accept YYYY-MM-DD only
        $success = [datetime]::TryParseExact($input, 'yyyy-MM-dd', $null, [System.Globalization.DateTimeStyles]::None, [ref]$parsed)
        if ($success) {
            return $parsed.Date
        }
        else {
            Write-Host "Invalid date format. Use YYYY-MM-DD (example: 2026-01-15)."
        }
    }
}

# Prompt user for start and end dates
Write-Host
Write-Host "Enter the start date for deletion (inclusive). Format: YYYY-MM-DD"
$startDate = Read-DateInput "Start date"
Write-Host
Write-Host "Enter the end date for deletion (inclusive). Format: YYYY-MM-DD"
$endDate = Read-DateInput "End date"

if ($endDate -lt $startDate) {
    Write-Host "End date is earlier than start date. Swapping values."
    $temp = $startDate
    $startDate = $endDate
    $endDate = $temp
}

# We will compare full timestamps; include entire end day by adding one day and using < next day
$rangeStart = $startDate.Date
$rangeEndExclusive = $endDate.Date.AddDays(1)

# Regex to capture timestamp portion: _YYYY-MM-DD_HH-mm-ss before .txt at end of filename
$timestampRegex = '_([0-9]{4}-[0-9]{2}-[0-9]{2}_[0-9]{2}-[0-9]{2}-[0-9]{2})\.txt$'

# Gather candidate files (files only, not directories)
$allFiles = Get-ChildItem -Path $logsDirectory -File -Recurse

$filesToDelete = @()

foreach ($file in $allFiles) {
    $match = [regex]::Match($file.Name, $timestampRegex)
    if ($match.Success) {
        $timestampString = $match.Groups[1].Value
        # Parse using exact format yyyy-MM-dd_HH-mm-ss
        $parseSuccess = [datetime]::TryParseExact(
            $timestampString,
            'yyyy-MM-dd_HH-mm-ss',
            $null,
            [System.Globalization.DateTimeStyles]::None,
            [ref]$fileTimestamp
        )

        if ($parseSuccess) {
            if ($fileTimestamp -ge $rangeStart -and $fileTimestamp -lt $rangeEndExclusive) {
                $filesToDelete += $file
            }
        }
    }
}

if ($filesToDelete.Count -eq 0) {
    Write-Host
    Write-Host "No files found in $logsDirectory matching the date range $($startDate.ToString('yyyy-MM-dd')) to $($endDate.ToString('yyyy-MM-dd'))."
    exit 0
}

Write-Host ""
Write-Host "The following $($filesToDelete.Count) file(s) will be deleted:"
Write-Host "--------------------------------------------------------"
foreach ($f in $filesToDelete) {
    Write-Host $f.FullName
}
Write-Host "--------------------------------------------------------"
Write-Host ""

# Confirm with the user
$confirmation = Read-Host "Type 'DELETE' to confirm and remove these files, or press Enter to cancel"
if ($confirmation -ne 'DELETE') {
    Write-Host "Operation cancelled by user. No files were deleted."
    exit 0
}

# Perform deletion and report results
$deletedCount = 0
$failedDeletes = @()

foreach ($file in $filesToDelete) {
    try {
        Remove-Item -LiteralPath $file.FullName -Force -ErrorAction Stop
        $deletedCount++
    }
    catch {
        $failedDeletes += @{ Path = $file.FullName; Error = $_.Exception.Message }
    }
}

Write-Host ""
Write-Host "Deletion complete. Deleted files: $deletedCount"
if ($failedDeletes.Count -gt 0) {
    Write-Host "Failed to delete $($failedDeletes.Count) file(s):"
    foreach ($failure in $failedDeletes) {
        Write-Host "- $($failure.Path) : $($failure.Error)"
    }
}
else {
    Write-Host
    Write-Host
    Write-Host "All targeted files were deleted successfully."
    Write-Host
}
