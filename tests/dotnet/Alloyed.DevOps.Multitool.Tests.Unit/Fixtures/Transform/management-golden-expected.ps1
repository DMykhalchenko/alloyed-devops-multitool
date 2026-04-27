# management group fixture
$svc = Get-AlloyedService -Name wuauserv
Start-AlloyedService -Name wuauserv
Stop-AlloyedService -Name wuauserv -Force # Restart-Service in comment stays
Restart-AlloyedService -Name wuauserv
