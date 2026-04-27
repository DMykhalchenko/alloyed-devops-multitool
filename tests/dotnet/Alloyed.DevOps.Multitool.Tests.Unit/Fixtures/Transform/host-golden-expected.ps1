# host group fixture
Write-AlloyedHost "Starting deployment..." -ForegroundColor Cyan
$env = Read-AlloyedHost -Prompt "Enter environment name"
Write-AlloyedProgress -Activity "Deploying" -Status "Step 1" -PercentComplete 25
Clear-AlloyedHost
Write-AlloyedHost "Clear-Host after clear stays in string"
