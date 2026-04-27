Copy-AlloyedItem -Path ./a.txt -Destination ./b.txt
Move-AlloyedItem -Path ./b.txt -Destination ./c.txt
Remove-AlloyedItem -Path ./c.txt -Force
New-AlloyedItem -Path ./new.txt -ItemType File
Get-AlloyedContent -Path ./new.txt
Set-AlloyedContent -Path ./new.txt -Value 'hello'
Write-Host "Copy-Item should stay in string"
# Move-Item should stay in comment
