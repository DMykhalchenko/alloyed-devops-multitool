param()

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$moduleManifest = Join-Path $repoRoot 'src/powershell/Alloyed.DevOps.Multitool.psd1'
$sampleScript = Join-Path $repoRoot 'samples/sample-transform-input.ps1'
$artifactsRoot = Join-Path $repoRoot 'tests/powershell/artifacts'
$sessionConfigBasePath = Join-Path $artifactsRoot 'session-config'
$sessionConfigPath = Join-Path $sessionConfigBasePath 'config/appsettings.json'
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

# Prepare isolated runtime config for session bootstrap checks.
if (-not (Test-Path -LiteralPath (Split-Path -Parent $sessionConfigPath))) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $sessionConfigPath) -Force | Out-Null
}

$sessionConfig = @{
    Alloyed = @{
        Runtime = @{
            DefaultOutputPath = 'out'
        }
        Session = @{
            Enabled = $true
        }
        Decoration = @{
            EnableErrorHandling = $true
            EnableObservability = $true
            EnableCorrelation = $true
            EnableTransparency = $true
            TransparencyProfile = 'standard'
        }
        Mocking = @{
            Enabled = $false
            Mode = 'InMemory'
        }
        Catalog = @{
            SourcePath = 'tools/ports/ports.catalog.json'
        }
    }
}
$sessionConfig | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $sessionConfigPath

# Validate session mode aliasing and rollback behavior.
$beforeGciAlias = (Get-Alias -Name gci -ErrorAction SilentlyContinue).Definition
$sessionEnable = Enable-AlloyedSessionMode
if (-not $sessionEnable.Enabled) {
    throw 'Enable-AlloyedSessionMode did not enable session mode.'
}

$childItemCommand = Get-Command -Name Get-ChildItem -ErrorAction SilentlyContinue
if (-not $childItemCommand -or $childItemCommand.CommandType -ne 'Function') {
    throw 'Session mode did not override Get-ChildItem with decorated function.'
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

# Validate transparency toggle surface.
$transparencyEnabled = Enable-AlloyedTransparencyMode
if (-not $transparencyEnabled.Enabled) {
    throw 'Enable-AlloyedTransparencyMode did not enable transparency mode.'
}
if (-not $transparencyEnabled.SessionModeEnabled) {
    throw 'Enable-AlloyedTransparencyMode did not enable session mode by default.'
}

$null = Get-ChildItem -Path $repoRoot

$transparencyDisabled = Disable-AlloyedTransparencyMode
if ($transparencyDisabled.Enabled) {
    throw 'Disable-AlloyedTransparencyMode did not disable transparency mode.'
}
$null = Disable-AlloyedSessionMode

# Validate new one-shot session bootstrap API.
$startedSession = Start-AlloyedSession -BasePath $sessionConfigBasePath -Profile minimal -QuietTransparency
if (-not $startedSession.Enabled) {
    throw 'Start-AlloyedSession did not enable transparency mode.'
}
if (-not $startedSession.SessionModeEnabled) {
    throw 'Start-AlloyedSession did not enable session mode.'
}
if ($startedSession.Profile -ne 'minimal') {
    throw "Start-AlloyedSession did not apply minimal profile. Actual='$($startedSession.Profile)'"
}

$stoppedSession = Stop-AlloyedSession
if ($stoppedSession.Enabled) {
    throw 'Stop-AlloyedSession did not disable transparency mode.'
}
if ($stoppedSession.SessionModeEnabled) {
    throw 'Stop-AlloyedSession did not disable session mode.'
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
