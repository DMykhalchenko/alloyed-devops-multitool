# diagnostics group fixture — commands in executable positions only
$procs = Get-Process -Name pwsh
Start-Process -FilePath pwsh.exe -NoNewWindow -Wait
Stop-Process -Name notepad -Force # Wait-Process in comment should stay
Wait-Process -Name pwsh -Timeout 30
$alive = Test-Connection -TargetName localhost -Count 1 -Quiet
$output = Invoke-Command -ScriptBlock { Get-Date }
Write-Host "Invoke-Command literal should stay"
