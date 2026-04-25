# pipeline fixture input
Get-ChildItem -Path .
Write-Host "Get-Item should stay literal"
@"
Test-Path -Path ./inside-here-string
"@
Get-Item -Path . # Test-Path in comment
Unknown-Command -Path .
