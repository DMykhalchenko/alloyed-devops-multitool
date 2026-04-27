# archive group fixture
Compress-Archive -Path ./src -DestinationPath ./release.zip -Force
Expand-Archive -Path ./release.zip -DestinationPath ./deploy -Force
Write-Host "Compress-Archive and Expand-Archive in string stay"
