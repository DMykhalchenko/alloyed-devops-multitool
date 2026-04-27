# host group fixture
Write-Host "Starting deployment..." -ForegroundColor Cyan
$env = Read-Host -Prompt "Enter environment name"
Write-Progress -Activity "Deploying" -Status "Step 1" -PercentComplete 25
Clear-Host
Write-Host "Clear-Host after clear stays in string"
