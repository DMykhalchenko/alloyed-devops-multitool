param()

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$moduleManifest = Join-Path $repoRoot 'src/powershell/Alloyed.DevOps.Multitool.psd1'
$sampleScript = Join-Path $repoRoot 'samples/sample-transform-input.ps1'
$artifactsRoot = Join-Path $repoRoot 'tests/powershell/artifacts'
$generatedModuleName = 'AlloyedSmokeModule'
$generatedModulePath = Join-Path (Join-Path $artifactsRoot $generatedModuleName) "$generatedModuleName.psm1"

if (-not (Test-Path $sampleScript)) {
    throw "Sample script not found: $sampleScript"
}

# Build host assembly required by wrapper module.
$env:DOTNET_CLI_HOME = Join-Path $repoRoot '.dotnet-cli'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

dotnet restore (Join-Path $repoRoot 'Alloyed.DevOps.Multitool.slnx') --verbosity minimal | Out-Null
dotnet build (Join-Path $repoRoot 'Alloyed.DevOps.Multitool.slnx') -c Debug --no-restore | Out-Null

Import-Module $moduleManifest -Force

# Validate session mode aliasing and rollback behavior.
$beforeGciAlias = (Get-Alias -Name gci -ErrorAction SilentlyContinue).Definition
$sessionEnable = Enable-AlloyedSessionMode
if (-not $sessionEnable.Enabled) {
    throw 'Enable-AlloyedSessionMode did not enable session mode.'
}

$childItemAlias = Get-Alias -Name Get-ChildItem -ErrorAction SilentlyContinue
if (-not $childItemAlias -or $childItemAlias.Definition -ne 'Get-AlloyedChildItem') {
    throw 'Session mode did not map Get-ChildItem to Get-AlloyedChildItem.'
}

$disableResult = Disable-AlloyedSessionMode
if ($disableResult.Enabled) {
    throw 'Disable-AlloyedSessionMode did not disable session mode.'
}

$afterGciAlias = (Get-Alias -Name gci -ErrorAction SilentlyContinue).Definition
if ($beforeGciAlias -ne $afterGciAlias) {
    throw "Session mode did not restore original gci alias. Before='$beforeGciAlias' After='$afterGciAlias'"
}

if ((Get-Command -Name Get-ChildItem).CommandType -ne 'Cmdlet') {
    throw 'Get-ChildItem command did not revert to native cmdlet after disabling session mode.'
}

# Validate catalog is exposed and contains expected mapping.
$catalog = Get-AlloyedCatalog
$childItemMapping = $catalog | Where-Object { $_.Command -eq 'Get-ChildItem' } | Select-Object -First 1
if (-not $childItemMapping) {
    throw 'Get-AlloyedCatalog did not return mapping for Get-ChildItem.'
}

if ($childItemMapping.Wrapper -ne 'Get-AlloyedChildItem') {
    throw "Unexpected wrapper mapping: $($childItemMapping.Wrapper)"
}

# Execute transformation pipeline.
$result = New-AlloyedModuleTransform -ScriptPath $sampleScript -ModuleName $generatedModuleName -OutputPath $artifactsRoot -Force
if (-not $result.Success) {
    throw "Transformation failed: $($result.ErrorMessage)"
}

if (-not (Test-Path $generatedModulePath)) {
    throw "Generated module script not found: $generatedModulePath"
}

$generatedContent = Get-Content -Raw -Path $generatedModulePath
if ($generatedContent -notmatch 'Get-AlloyedChildItem') {
    throw 'Generated module does not contain transformed Get-AlloyedChildItem call.'
}

if ($generatedContent -notmatch 'Get-AlloyedItem') {
    throw 'Generated module does not contain transformed Get-AlloyedItem call.'
}

if ($generatedContent -notmatch 'Test-AlloyedPath') {
    throw 'Generated module does not contain transformed Test-AlloyedPath call.'
}

Write-Host 'Smoke test passed: end-to-end transform and module generation are working.' -ForegroundColor Green
