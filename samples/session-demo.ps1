#Requires -Version 7.0
<#
.SYNOPSIS
    Demonstrates the Alloyed session and transparency features interactively.

.DESCRIPTION
    Walks through the full session lifecycle in one script:

      1. Start a session (transparency + command interception)
      2. Run native commands — session mode intercepts them and emits decoration events
      3. Switch transparency profiles (minimal / standard / debug)
      4. Inspect combined session state
      5. Stop the session and verify teardown

    Run from the repository root after building the project:

      Import-Module ./src/powershell/Alloyed.DevOps.Multitool.psd1
      ./samples/session-demo.ps1

    Session mode intercepts native PowerShell cmdlets (Get-ChildItem, Test-Path, etc.)
    and wraps them with the Alloyed decoration pipeline. You do NOT call Get-AlloyedChildItem
    directly — you call Get-ChildItem and session mode handles the interception.

.PARAMETER OutputMode
    Console rendering back-end for this run. Defaults to Rich.

.PARAMETER Profile
    Transparency profile to start with. Defaults to standard.
#>
param(
    [ValidateSet('Plain', 'Rich')]
    [string]$OutputMode = 'Rich',

    [ValidateSet('minimal', 'standard', 'debug')]
    [string]$Profile = 'standard'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Guard — module must already be imported
# ---------------------------------------------------------------------------

if (-not (Get-Command Start-AlloyedSession -ErrorAction SilentlyContinue)) {
    throw "Alloyed module is not loaded. Run: Import-Module ./src/powershell/Alloyed.DevOps.Multitool.psd1"
}

# ---------------------------------------------------------------------------
# Section helper
# ---------------------------------------------------------------------------

function Write-DemoSection {
    param([string]$Title)
    Write-Host ""
    Write-Host "--- $Title ---"
    Write-Host ""
}

# ---------------------------------------------------------------------------
# 1. Start session — this also initialises the host assembly
# ---------------------------------------------------------------------------

Write-DemoSection "Start-AlloyedSession (profile: $Profile, outputMode: $OutputMode)"

$null = Start-AlloyedSession -Profile $Profile -OutputMode $OutputMode

$after = Get-AlloyedTransparencyModeStatus
Write-Host "Transparency enabled : $($after.Enabled)"
Write-Host "Session mode active  : $($after.SessionModeEnabled)"
Write-Host "Output mode          : $($after.OutputMode)"
Write-Host "Profile              : $($after.Profile ?? '<none>')"

# ---------------------------------------------------------------------------
# 2. Wrapped command execution — standard profile
#    Session mode intercepts native cmdlets; call them by their normal names.
# ---------------------------------------------------------------------------

Write-DemoSection "Native commands intercepted by session mode (profile: $Profile)"

Write-Host "Get-ChildItem `$PSScriptRoot -Filter *.ps1 (intercepted)"
$psFiles = Get-ChildItem -Path $PSScriptRoot -Filter "*.ps1"
$psFiles | ForEach-Object { Write-Host "  $($_.FullName)" }

Write-Host ""
Write-Host "Test-Path ./samples (intercepted)"
$samplesExist = Test-Path -Path "./samples"
Write-Host "  Exists: $samplesExist"

Write-Host ""
Write-Host "ConvertTo-Json + ConvertFrom-Json round-trip (intercepted)"
$data   = [pscustomobject]@{ Name = "deploy"; Version = "1.0"; Tags = @("prod", "infra") }
$json   = $data   | ConvertTo-Json
$parsed = $json   | ConvertFrom-Json
Write-Host "  Name   : $($parsed.Name)"
Write-Host "  Version: $($parsed.Version)"

Write-Host ""
Write-Host "Join-Path + Split-Path (intercepted)"
$joined = Join-Path "samples" "deploy-scenario.ps1"
$parent = Split-Path $joined -Parent
Write-Host "  Joined : $joined"
Write-Host "  Parent : $parent"

# ---------------------------------------------------------------------------
# 3. Switch to minimal profile — less noise
# ---------------------------------------------------------------------------

Write-DemoSection "Switch to minimal profile"

$null = Set-AlloyedTransparencyProfile -Profile minimal

Write-Host "Get-Item ./samples (intercepted)"
$item = Get-Item -Path "./samples"
Write-Host "  $($item.FullName)"

Write-Host ""
Write-Host "Test-Path (non-existent, intercepted)"
$missing = Test-Path -Path "./does-not-exist"
Write-Host "  Exists: $missing"

# ---------------------------------------------------------------------------
# 4. Switch to debug profile — full tag dump
# ---------------------------------------------------------------------------

Write-DemoSection "Switch to debug profile"

$null = Set-AlloyedTransparencyProfile -Profile debug

Write-Host "Get-Location (intercepted)"
$location = Get-Location
Write-Host "  $($location.Path)"

# ---------------------------------------------------------------------------
# 5. Combined session state snapshot
# ---------------------------------------------------------------------------

Write-DemoSection "Get-AlloyedSessionState"

$state = Get-AlloyedSessionState
Write-Host "Runtime transparency : $($state.RuntimeTransparencyEnabled)"
Write-Host "Runtime profile      : $($state.RuntimeTransparencyProfile)"
Write-Host "Current transparency : $($state.CurrentTransparencyEnabled)"
Write-Host "Current profile      : $($state.CurrentProfile)"
Write-Host "Current output mode  : $($state.CurrentOutputMode)"
Write-Host "Session mode active  : $($state.CurrentSessionModeEnabled)"

# ---------------------------------------------------------------------------
# 6. Stop session
# ---------------------------------------------------------------------------

Write-DemoSection "Stop-AlloyedSession"

$null = Stop-AlloyedSession

$final = Get-AlloyedTransparencyModeStatus
Write-Host "Transparency enabled : $($final.Enabled)"
Write-Host "Session mode active  : $($final.SessionModeEnabled)"
Write-Host ""
Write-Host "Session demo complete."
