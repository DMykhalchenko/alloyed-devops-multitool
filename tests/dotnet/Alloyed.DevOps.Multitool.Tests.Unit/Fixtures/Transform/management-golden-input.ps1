# management group fixture
$svc = Get-Service -Name wuauserv
Start-Service -Name wuauserv
Stop-Service -Name wuauserv -Force # Restart-Service in comment stays
Restart-Service -Name wuauserv
