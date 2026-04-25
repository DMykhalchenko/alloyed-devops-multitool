# fixture: verify only executable code gets transformed
Get-ChildItem -Path .
Write-Host "Get-Item should stay here"
@"
Test-Path -Path ./inside-here-string
"@
Get-Item -Path . # Test-Path in comment should stay
