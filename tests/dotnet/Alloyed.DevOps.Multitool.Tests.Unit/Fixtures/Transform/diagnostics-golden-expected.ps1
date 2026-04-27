# diagnostics group fixture — commands in executable positions only
$procs = Get-AlloyedProcess -Name pwsh
Start-AlloyedProcess -FilePath pwsh.exe -NoNewWindow -Wait
Stop-AlloyedProcess -Name notepad -Force # Wait-Process in comment should stay
Wait-AlloyedProcess -Name pwsh -Timeout 30
$alive = Test-AlloyedConnection -TargetName localhost -Count 1 -Quiet
$output = Invoke-AlloyedCommand -ScriptBlock { Get-Date }
Write-Host "Invoke-Command literal should stay"
