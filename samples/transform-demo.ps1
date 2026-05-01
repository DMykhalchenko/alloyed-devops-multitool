#Requires -Version 7.0
<#
.SYNOPSIS
    Demonstrates the Alloyed AST transformation pipeline end to end.

.DESCRIPTION
    Walks through three progressive steps:

      1. Catalog exploration  — show every command-to-wrapper mapping
      2. Dry-run validation   — Test-AlloyedTransform against deploy-scenario.ps1
      3. Full transform       — New-AlloyedModuleTransform writes an importable module

    The full transform step is gated by -Execute so you can run the first two steps
    safely during demos without producing output on disk.

    Run from the repository root after building the project:

      Import-Module ./src/powershell/Alloyed.DevOps.Multitool.psd1
      ./samples/transform-demo.ps1
      ./samples/transform-demo.ps1 -Execute              # also produce the output module

.PARAMETER ScriptPath
    Source script to transform. Defaults to ./samples/deploy-scenario.ps1.

.PARAMETER ModuleName
    Output module name. Defaults to DeployScenarioModule.

.PARAMETER OutputPath
    Directory where the module is written. Defaults to ./out.

.PARAMETER Execute
    When present, runs the full New-AlloyedModuleTransform after the dry-run.

.PARAMETER OutputMode
    Console rendering back-end for result summaries. Defaults to Rich.
#>
param(
    [string]$ScriptPath  = (Join-Path $PSScriptRoot "deploy-scenario.ps1"),
    [string]$ModuleName  = "DeployScenarioModule",
    [string]$OutputPath  = (Join-Path (Split-Path $PSScriptRoot -Parent) "out"),
    [switch]$Execute,
    [ValidateSet('Plain', 'Rich')]
    [string]$OutputMode  = 'Rich'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Guard
# ---------------------------------------------------------------------------

if (-not (Get-Command Get-AlloyedCatalog -ErrorAction SilentlyContinue)) {
    throw "Alloyed module is not loaded. Run: Import-Module ./src/powershell/Alloyed.DevOps.Multitool.psd1"
}

if (-not (Test-Path -LiteralPath $ScriptPath)) {
    throw "Source script not found: $ScriptPath"
}

function Write-DemoSection {
    param([string]$Title)
    Write-Host ""
    Write-Host "--- $Title ---"
    Write-Host ""
}

# ---------------------------------------------------------------------------
# 1. Catalog exploration
# ---------------------------------------------------------------------------

Write-DemoSection "Catalog — registered command-to-wrapper mappings"

$catalog = Get-AlloyedCatalog
$total = @($catalog).Count
Write-Host "$total commands registered."
Write-Host ""

$catalog |
    Group-Object { $_.Command.Split('-')[0] } |
    Sort-Object Name |
    ForEach-Object {
        $verb  = $_.Name
        $names = ($_.Group | ForEach-Object { $_.Command }) -join ', '
        Write-Host ("  {0,-12} {1}" -f "${verb}:", $names)
    }

Write-Host ""
Write-Host "Wrapper prefix convention: <Verb>-Alloyed<Noun>"
Write-Host "Example: Get-ChildItem -> Get-AlloyedChildItem"

# ---------------------------------------------------------------------------
# 2. Dry-run validation
# ---------------------------------------------------------------------------

Write-DemoSection "Test-AlloyedTransform (dry run) — $($ScriptPath | Split-Path -Leaf)"

$validation = Test-AlloyedTransform -ScriptPath $ScriptPath -OutputMode $OutputMode

Write-Host "Result:"
Write-Host "  Success          : $($validation.Success)"
Write-Host "  Commands found   : $($validation.CommandsFound)"
Write-Host "  Commands replaced: $($validation.CommandsReplaced)"
Write-Host "  Missing mappings : $($validation.MissingCommands.Count)"

if ($validation.MissingCommands.Count -gt 0) {
    Write-Host ""
    Write-Host "  Commands without catalog entries:"
    $validation.MissingCommands | ForEach-Object { Write-Host "    - $_" }
}

if ($validation.Diagnostics.Count -gt 0) {
    Write-Host ""
    Write-Host "  AST diagnostics:"
    $validation.Diagnostics | ForEach-Object {
        Write-Host ("    [{0}] {1} — {2} (line {3})" -f $_.Severity, $_.Code, $_.Message, $_.Line)
    }
}

# ---------------------------------------------------------------------------
# 3. Full transform (optional)
# ---------------------------------------------------------------------------

if (-not $Execute.IsPresent) {
    Write-Host ""
    Write-Host "Skipping full transform. Pass -Execute to produce the output module."
    Write-Host "  New-AlloyedModuleTransform -ScriptPath '$ScriptPath' -ModuleName '$ModuleName' -OutputPath '$OutputPath' -Force"
    exit 0
}

Write-DemoSection "New-AlloyedModuleTransform — writing module to disk"

Write-Host "ModuleName : $ModuleName"
Write-Host "OutputPath : $OutputPath"
Write-Host "Source     : $ScriptPath"
Write-Host ""

$result = New-AlloyedModuleTransform `
    -ScriptPath  $ScriptPath `
    -ModuleName  $ModuleName `
    -OutputPath  $OutputPath `
    -Force `
    -OutputMode  $OutputMode

if ($result.Success) {
    $modulePath = Join-Path $OutputPath $ModuleName "$ModuleName.psd1"
    Write-Host ""
    Write-Host "Module written. Import with:"
    Write-Host "  Import-Module '$modulePath'"
    Write-Host ""
    Write-Host "Then run the transformed deployment:"
    Write-Host "  Invoke-ProductionDeployment"
} else {
    Write-Warning "Transform completed with errors. Check the diagnostics above."
}
