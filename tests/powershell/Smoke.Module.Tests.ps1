param(
    [switch]$CiQuiet
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$moduleManifest = Join-Path $repoRoot 'src/powershell/Alloyed.DevOps.Multitool.psd1'
$sampleScript = Join-Path $repoRoot 'samples/sample-transform-input.ps1'
$artifactsRoot = Join-Path $repoRoot 'tests/powershell/artifacts'
$dotnetBuildRoot = Join-Path $artifactsRoot 'dotnet-build'
$dotnetBuildBaseOutput = $dotnetBuildRoot + [System.IO.Path]::DirectorySeparatorChar
$moduleLibPath = Join-Path $repoRoot 'src/powershell/lib'
$sessionConfigBasePath = Join-Path $artifactsRoot 'session-config'
$sessionConfigPath = Join-Path $sessionConfigBasePath 'config/appsettings.json'
$generatedModuleName = 'AlloyedSmokeModule'
$generatedModulePath = Join-Path (Join-Path $artifactsRoot $generatedModuleName) "$generatedModuleName.psm1"
$hostProject = Join-Path $repoRoot 'src/dotnet/Alloyed.DevOps.Multitool.Host.PowerShell/Alloyed.DevOps.Multitool.Host.PowerShell.csproj'
$decorationProject = Join-Path $repoRoot 'src/dotnet/Alloyed.DevOps.Multitool.Core.Decoration/Alloyed.DevOps.Multitool.Core.Decoration.csproj'

if (-not (Test-Path $sampleScript)) {
    throw "Sample script not found: $sampleScript"
}

# Build host assembly required by wrapper module.
$env:DOTNET_CLI_HOME = Join-Path $repoRoot '.dotnet-cli'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

dotnet restore (Join-Path $repoRoot 'Alloyed.DevOps.Multitool.slnx') --verbosity minimal | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE."
}

dotnet build $hostProject -c Debug --no-restore -p:BaseOutputPath=$dotnetBuildBaseOutput | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed for host project with exit code $LASTEXITCODE."
}

dotnet build $decorationProject -c Debug --no-restore -p:BaseOutputPath=$dotnetBuildBaseOutput | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed for decoration project with exit code $LASTEXITCODE."
}

$packagedBuildOutput = Join-Path $dotnetBuildRoot 'Debug/net8.0'
if (-not (Test-Path -LiteralPath $packagedBuildOutput)) {
    throw "Expected build output directory not found: $packagedBuildOutput"
}

if (-not (Test-Path -LiteralPath $moduleLibPath)) {
    New-Item -ItemType Directory -Path $moduleLibPath -Force | Out-Null
}

Get-ChildItem -LiteralPath $packagedBuildOutput -File | Copy-Item -Destination $moduleLibPath -Force

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
$enableTransparencyParams = @{}
if ($CiQuiet.IsPresent) {
    $enableTransparencyParams['Quiet'] = $true
    $enableTransparencyParams['Profile'] = 'minimal'
}
$transparencyEnabled = Enable-AlloyedTransparencyMode @enableTransparencyParams
if (-not $transparencyEnabled.Enabled) {
    throw 'Enable-AlloyedTransparencyMode did not enable transparency mode.'
}
if (-not $transparencyEnabled.SessionModeEnabled) {
    throw 'Enable-AlloyedTransparencyMode did not enable session mode by default.'
}

$transparencyFormatted = Get-AlloyedTransparencyModeStatus | Out-String -Width 200
if ($transparencyFormatted -notmatch 'Enabled' -or $transparencyFormatted -notmatch 'OutputMode') {
    throw 'Get-AlloyedTransparencyModeStatus formatting did not expose expected columns.'
}

$null = Get-ChildItem -Path $repoRoot

$transparencyDisabled = Disable-AlloyedTransparencyMode
if ($transparencyDisabled.Enabled) {
    throw 'Disable-AlloyedTransparencyMode did not disable transparency mode.'
}
$null = Disable-AlloyedSessionMode

# Validate new one-shot session bootstrap API.
$startedSession = Start-AlloyedSession -BasePath $sessionConfigBasePath -Profile minimal -QuietTransparency:$CiQuiet
if (-not $startedSession.Enabled) {
    throw 'Start-AlloyedSession did not enable transparency mode.'
}
if (-not $startedSession.SessionModeEnabled) {
    throw 'Start-AlloyedSession did not enable session mode.'
}
if ($startedSession.Profile -ne 'minimal') {
    throw "Start-AlloyedSession did not apply minimal profile. Actual='$($startedSession.Profile)'"
}

$state = Get-AlloyedSessionState -BasePath $sessionConfigBasePath
if (-not $state.CurrentSessionModeEnabled -or -not $state.CurrentTransparencyEnabled) {
    throw 'Get-AlloyedSessionState does not reflect active session/transparency state.'
}
if ($state.CurrentProfile -ne 'minimal') {
    throw "Get-AlloyedSessionState profile mismatch. Actual='$($state.CurrentProfile)'"
}

$stateFormatted = Get-AlloyedSessionState -BasePath $sessionConfigBasePath | Out-String -Width 200
if ($stateFormatted -notmatch 'RuntimeConfigPath' -or $stateFormatted -notmatch 'CurrentOutputMode') {
    throw 'Get-AlloyedSessionState formatting did not expose expected fields.'
}

$updatedProfile = Set-AlloyedTransparencyProfile -Profile debug
if ($updatedProfile.Profile -ne 'debug') {
    throw "Set-AlloyedTransparencyProfile did not switch profile to debug. Actual='$($updatedProfile.Profile)'"
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
$firstCatalogEntry = $catalog | Select-Object -First 1
if (-not $firstCatalogEntry.PSObject.TypeNames.Contains('Alloyed.CatalogMapping')) {
    throw 'Get-AlloyedCatalog entries are missing Alloyed.CatalogMapping type name.'
}

$catalogFormatted = $catalog | Select-Object -First 5 | Out-String -Width 200
if ($catalogFormatted -notmatch 'Command' -or $catalogFormatted -notmatch 'Wrapper') {
    throw 'Get-AlloyedCatalog formatting did not expose expected columns.'
}

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

$validation = Test-AlloyedTransform -ScriptPath $sampleScript
if (-not $validation.PSObject.TypeNames.Contains('Alloyed.TransformValidationResult')) {
    throw 'Test-AlloyedTransform result is missing Alloyed.TransformValidationResult type name.'
}
if ($validation.MissingCommandCount -lt 0 -or $validation.DiagnosticCount -lt 0) {
    throw 'Test-AlloyedTransform summary counters are invalid.'
}

$validationFormatted = $validation | Out-String -Width 200
if ($validationFormatted -notmatch 'CommandsFound' -or $validationFormatted -notmatch 'DiagnosticCount') {
    throw 'Test-AlloyedTransform formatting did not expose expected summary fields.'
}

$runtimeConfigurationFormatted = Get-AlloyedRuntimeConfiguration -BasePath $sessionConfigBasePath | Out-String -Width 200
if ($runtimeConfigurationFormatted -notmatch 'DefaultOutputPath' -or $runtimeConfigurationFormatted -notmatch 'TransparencyProfile') {
    throw 'Get-AlloyedRuntimeConfiguration formatting did not expose expected fields.'
}

$runtimeValidation = Test-AlloyedRuntimeConfig -BasePath $sessionConfigBasePath
if (-not $runtimeValidation.PSObject.TypeNames.Contains('Alloyed.RuntimeConfigValidationResult')) {
    throw 'Test-AlloyedRuntimeConfig result is missing Alloyed.RuntimeConfigValidationResult type name.'
}
if ($runtimeValidation.RuntimeRetryDelaySec -lt 0 -or $runtimeValidation.RuntimeTimeoutSec -lt 0) {
    throw 'Test-AlloyedRuntimeConfig returned invalid runtime policy values.'
}

$runtimeValidationFormatted = $runtimeValidation | Out-String -Width 200
if ($runtimeValidationFormatted -notmatch 'RuntimeDefaultOutputPath' -or $runtimeValidationFormatted -notmatch 'RuntimeTimeoutSec') {
    throw 'Test-AlloyedRuntimeConfig formatting did not expose expected summary fields.'
}

$previousPreview = [System.Environment]::GetEnvironmentVariable('ALLOYED_RUNTIME_PREVIEW', 'Process')
try {
    [System.Environment]::SetEnvironmentVariable('ALLOYED_RUNTIME_PREVIEW', 'true', 'Process')
    $previewCommand = @(
        "Import-Module '$moduleManifest' -Force"
        "[System.Environment]::SetEnvironmentVariable('ALLOYED_RUNTIME_PREVIEW','true','Process')"
        "& (Get-Module Alloyed.DevOps.Multitool) { Invoke-AlloyedCommandRuntime -Operation 'PreviewCheck' -Action { 'preview-ok' } | Out-Null }"
    ) -join '; '
    $runtimePreviewOutput = & pwsh -NoProfile -Command $previewCommand | Out-String -Width 200
} finally {
    [System.Environment]::SetEnvironmentVariable('ALLOYED_RUNTIME_PREVIEW', $previousPreview, 'Process')
}

if ($runtimePreviewOutput -notmatch '\[INFO\].*runtime-preview phase=attempt op=PreviewCheck attempt=1') {
    throw 'Invoke-AlloyedCommandRuntime preview output did not flow through the console reporter as expected.'
}

Write-Host 'Smoke test passed: end-to-end transform and module generation are working.' -ForegroundColor Green
