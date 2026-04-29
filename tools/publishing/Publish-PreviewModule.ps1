[CmdletBinding()]
param(
    [Parameter()] [string]$Owner = "Ligare-Method",
    [Parameter()] [string]$Repository = "alloyed-devops-multitool",
    [Parameter()] [string]$ModuleName = "Alloyed.DevOps.Multitool",
    [Parameter(Mandatory)] [string]$ModuleVersion,
    [Parameter(Mandatory)] [string]$Prerelease,
    [Parameter()] [string]$ApiKey = $env:GITHUB_TOKEN,
    [Parameter()] [switch]$SkipBuild,
    [Parameter()] [switch]$SkipIfVersionExists,
    [Parameter()] [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    throw "API key is required. Pass -ApiKey or set GITHUB_TOKEN."
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$solutionPath = Join-Path $repoRoot "Alloyed.DevOps.Multitool.slnx"
$moduleSourcePath = Join-Path $repoRoot "src/powershell"
$sourceManifestPath = Join-Path $moduleSourcePath "$ModuleName.psd1"
$sourceModulePath = Join-Path $moduleSourcePath "$ModuleName.psm1"
$hostOutputPath = Join-Path $repoRoot "src/dotnet/Alloyed.DevOps.Multitool.Host.PowerShell/bin/Debug/net8.0"
$decorationOutputPath = Join-Path $repoRoot "src/dotnet/Alloyed.DevOps.Multitool.Core.Decoration/bin/Debug/net8.0"
$publishRoot = Join-Path $repoRoot "temp/publish/$ModuleName"
$stagedLibPath = Join-Path $publishRoot "lib"
$stagedManifestPath = Join-Path $publishRoot "$ModuleName.psd1"

$feedUri = "https://nuget.pkg.github.com/$Owner/index.json"
$repositoryName = "GitHubPackages-$Owner"
$fullVersion = "$ModuleVersion-$Prerelease"

Write-Host "[1/5] Preparing module stage: $publishRoot"
if (Test-Path $publishRoot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
New-Item -ItemType Directory -Path $stagedLibPath -Force | Out-Null

Copy-Item -LiteralPath $sourceManifestPath -Destination $stagedManifestPath -Force
Copy-Item -LiteralPath $sourceModulePath -Destination (Join-Path $publishRoot "$ModuleName.psm1") -Force

Write-Host "[2/5] Building .NET host assemblies"
if (-not $SkipBuild.IsPresent) {
    dotnet restore $solutionPath --verbosity minimal
    dotnet build $solutionPath -c Debug --no-restore
}

if (-not (Test-Path $hostOutputPath)) {
    throw "Host output path not found: $hostOutputPath"
}
if (-not (Test-Path $decorationOutputPath)) {
    throw "Decoration output path not found: $decorationOutputPath"
}

Get-ChildItem -Path $hostOutputPath -File | Copy-Item -Destination $stagedLibPath -Force
Get-ChildItem -Path $decorationOutputPath -File | Copy-Item -Destination $stagedLibPath -Force

Write-Host "[3/5] Updating manifest version to $fullVersion"
Update-ModuleManifest -Path $stagedManifestPath -ModuleVersion $ModuleVersion -Prerelease $Prerelease

Write-Host "[4/5] Registering PSResource repository: $feedUri"
Import-Module Microsoft.PowerShell.PSResourceGet -ErrorAction Stop

$existing = Get-PSResourceRepository -Name $repositoryName -ErrorAction SilentlyContinue
if ($existing) {
    Unregister-PSResourceRepository -Name $repositoryName
}

Register-PSResourceRepository -Name $repositoryName -Uri $feedUri -Trusted

Write-Host "[5/5] Publishing module $ModuleName $fullVersion"
$publishParams = @{
    Path = $publishRoot
    Repository = $repositoryName
    ApiKey = $ApiKey
    SkipDependenciesCheck = $true
}

if ($WhatIf.IsPresent) {
    Publish-PSResource @publishParams -WhatIf
} else {
    try {
        Publish-PSResource @publishParams
    } catch {
        $message = $_.Exception.Message
        if ($SkipIfVersionExists.IsPresent -and $message -match '409\s*\(Conflict\)') {
            Write-Warning "Module version already exists ($fullVersion). Skipping publish because -SkipIfVersionExists is enabled."
        } else {
            throw
        }
    }
}

Write-Host "Done. Published $ModuleName $fullVersion to $feedUri"
