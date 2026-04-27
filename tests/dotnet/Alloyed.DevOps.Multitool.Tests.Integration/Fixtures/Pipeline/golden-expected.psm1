# pipeline fixture input
Get-AlloyedChildItem -Path .
Write-AlloyedHost "Get-Item should stay literal"
@"
Test-Path -Path ./inside-here-string
"@
Get-AlloyedItem -Path . # Test-Path in comment
Unknown-Command -Path .
