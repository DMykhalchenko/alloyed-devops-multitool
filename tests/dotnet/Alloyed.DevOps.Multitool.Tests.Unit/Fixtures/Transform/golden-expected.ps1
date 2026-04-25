# fixture: verify only executable code gets transformed
Get-AlloyedChildItem -Path .
Write-Host "Get-Item should stay here"
@"
Test-Path -Path ./inside-here-string
"@
Get-AlloyedItem -Path . # Test-Path in comment should stay
