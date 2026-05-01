[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$root = git rev-parse --show-toplevel
Set-Location $root

$env:DOTNET_CLI_HOME                    = Join-Path $root '.dotnet-cli'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE  = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT        = '1'

$solution = 'Alloyed.DevOps.Multitool.slnx'

function Invoke-Step {
    param([string]$Name, [scriptblock]$Action)
    Write-Host "`n==> $Name" -ForegroundColor Cyan
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    & $Action
    $code = $LASTEXITCODE
    $sw.Stop()
    if ($code -ne 0) { throw "Step failed: $Name (exit $code)" }
    Write-Host "<== $Name ($($sw.Elapsed.ToString('mm\:ss\.ff')))" -ForegroundColor Green
}

Invoke-Step 'Format check (C#)' {
    dotnet format $solution --verify-no-changes
}

Invoke-Step 'Lint (PowerShell)' {
    if (Get-Module -ListAvailable PSScriptAnalyzer) {
        Invoke-ScriptAnalyzer -Path src/powershell -Recurse `
            -Settings .config/PSScriptAnalyzerSettings.psd1 -EnableExit
    } else {
        Write-Warning 'PSScriptAnalyzer not installed — skipping PS lint'
    }
}

Write-Host "`nAll pre-commit checks passed.`n" -ForegroundColor Green
