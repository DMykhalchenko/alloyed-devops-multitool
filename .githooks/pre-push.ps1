[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$root = git rev-parse --show-toplevel
Set-Location $root

$env:DOTNET_CLI_HOME            = Join-Path $root '.dotnet-cli'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT       = '1'

$solution    = 'Alloyed.DevOps.Multitool.slnx'
$unitProject = 'tests/dotnet/Alloyed.DevOps.Multitool.Tests.Unit/Alloyed.DevOps.Multitool.Tests.Unit.csproj'

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

Invoke-Step 'Build' {
    dotnet build $solution -c Debug --no-restore --nologo
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

Invoke-Step 'Unit tests' {
    dotnet test $unitProject -c Debug --no-build --nologo
}

Write-Host "`nAll pre-push checks passed.`n" -ForegroundColor Green
