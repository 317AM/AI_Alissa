$docsPath = Join-Path $PSScriptRoot "..\docs\structure.md"

tree /F /A > "$PSScriptRoot\temp_structure.txt"

"# Project Structure`n" | Out-File $docsPath
'```' | Out-File $docsPath -Append
Get-Content "$PSScriptRoot\temp_structure.txt" | Out-File $docsPath -Append
'```' | Out-File $docsPath -Append

Remove-Item "$PSScriptRoot\temp_structure.txt"

Write-Host
Write-Host
Write-Host "Successfully updated structure.md in /docs"
Write-Host