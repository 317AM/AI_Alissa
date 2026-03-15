# update_personality.ps1
# Overwrites docs\personality.md with the contents of every .txt file in the personality directory.
# Adjust $projectRoot if you run the script from a different working directory.

$projectRoot = 'D:\OneDrive - HTBLA Leonding\Projects\AI_Alissa'
$personalityDirectory = Join-Path $projectRoot 'personality'
$outputFilePath = Join-Path $projectRoot 'docs\personality.md'

# Ensure output directory exists
$outputDir = Split-Path -Path $outputFilePath -Parent
if (-not (Test-Path -Path $outputDir)) {
    New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
}

# Start fresh: overwrite the file with a header
"# Personality Files`n" | Out-File -FilePath $outputFilePath -Encoding UTF8 -Force

# Iterate .txt files in the personality directory (sorted by name)
Get-ChildItem -Path $personalityDirectory -File -Filter '*.txt' | Sort-Object Name | ForEach-Object {
    $fileName = $_.Name
    $filePath = $_.FullName

    # Read entire file content as a single string
    $fileContent = Get-Content -Path $filePath -Raw -ErrorAction SilentlyContinue

    # Write the structured block for this file
    "($fileName):`n" | Out-File -FilePath $outputFilePath -Encoding UTF8 -Append
    '```' | Out-File -FilePath $outputFilePath -Encoding UTF8 -Append
    if ($null -eq $fileContent) {
        '' | Out-File -FilePath $outputFilePath -Encoding UTF8 -Append
    }
    else {
        $fileContent | Out-File -FilePath $outputFilePath -Encoding UTF8 -Append
    }
    '```' | Out-File -FilePath $outputFilePath -Encoding UTF8 -Append
    '' | Out-File -FilePath $outputFilePath -Encoding UTF8 -Append
}

Write-Host 
Write-Host "Successfully updated $outputFilePath with contents of $personalityDirectory"
