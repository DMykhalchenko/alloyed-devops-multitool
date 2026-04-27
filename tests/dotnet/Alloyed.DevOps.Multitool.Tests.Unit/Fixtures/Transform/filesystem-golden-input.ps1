Copy-Item -Path ./a.txt -Destination ./b.txt
Move-Item -Path ./b.txt -Destination ./c.txt
Remove-Item -Path ./c.txt -Force
New-Item -Path ./new.txt -ItemType File
Get-Content -Path ./new.txt
Set-Content -Path ./new.txt -Value 'hello'
Write-Host "Copy-Item should stay in string"
# Move-Item should stay in comment
