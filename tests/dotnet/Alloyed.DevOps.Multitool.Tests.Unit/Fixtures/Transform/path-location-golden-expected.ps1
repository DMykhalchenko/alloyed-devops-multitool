Get-AlloyedLocation
Set-AlloyedLocation -Path .
Push-AlloyedLocation -Path .
Pop-AlloyedLocation
Join-AlloyedPath -Path a -ChildPath b
Split-AlloyedPath -Path a\b\c.txt -Parent
Resolve-AlloyedPath -Path .
Write-Host "Set-Location should stay in string"
# Join-Path should stay in comment
