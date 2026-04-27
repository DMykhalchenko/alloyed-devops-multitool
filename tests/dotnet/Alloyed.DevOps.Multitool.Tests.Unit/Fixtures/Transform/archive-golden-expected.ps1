# archive group fixture
Compress-AlloyedArchive -Path ./src -DestinationPath ./release.zip -Force
Expand-AlloyedArchive -Path ./release.zip -DestinationPath ./deploy -Force
Write-Host "Compress-Archive and Expand-Archive in string stay"
