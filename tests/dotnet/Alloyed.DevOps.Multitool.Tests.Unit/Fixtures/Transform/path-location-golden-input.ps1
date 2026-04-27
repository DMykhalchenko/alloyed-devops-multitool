Get-Location
Set-Location -Path .
Push-Location -Path .
Pop-Location
Join-Path -Path a -ChildPath b
Split-Path -Path a\b\c.txt -Parent
Resolve-Path -Path .
Write-Host "Set-Location should stay in string"
# Join-Path should stay in comment
