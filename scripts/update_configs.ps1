# update_configs.ps1

# Adjust $configDirectory and $outputFilePath if needed.

$configDirectory = 'D:\OneDrive - HTBLA Leonding\Projects\AI_Alissa\config'
$outputFilePath  = 'D:\OneDrive - HTBLA Leonding\Projects\AI_Alissa\docs\configs.md'

# Ensure output directory exists
$outputDir = Split-Path -Path $outputFilePath -Parent
if (-not (Test-Path -Path $outputDir)) {
    New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
}

# Start fresh: overwrite the file
"# Config files`n" | Out-File -FilePath $outputFilePath -Encoding UTF8 -Force

# Iterate files in the config directory (sorted by name)
Get-ChildItem -Path $configDirectory -File | Sort-Object Name | ForEach-Object {
    $fileName = $_.Name
    $filePath = $_.FullName

    # Read entire file content as a single string
    $fileContent = Get-Content -Path $filePath -Raw -ErrorAction SilentlyContinue

    # Write the structured block for this file
    "($fileName):`n" | Out-File -FilePath $outputFilePath -Encoding UTF8 -Append
    '```' | Out-File -FilePath $outputFilePath -Encoding UTF8 -Append
    # If fileContent is $null or empty, still write an empty line inside the code block
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
Write-Host
Write-Host "Successfully updated $outputFilePath with contents of $configDirectory"
Write-Host