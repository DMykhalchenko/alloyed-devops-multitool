[CmdletBinding()]
param(
    [ValidateSet("fast", "full", "ci", "build", "unit", "integration", "smoke", "setup")]
    [string]$Stage = "fast",
    [switch]$Restore,
    [string]$Filter
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectRoot

$env:DOTNET_CLI_HOME = Join-Path $projectRoot ".dotnet-cli"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

New-Item -ItemType Directory -Path $env:DOTNET_CLI_HOME -Force | Out-Null

$solution = "Alloyed.DevOps.Multitool.slnx"
$unitProject = "tests/dotnet/Alloyed.DevOps.Multitool.Tests.Unit/Alloyed.DevOps.Multitool.Tests.Unit.csproj"
$integrationProject = "tests/dotnet/Alloyed.DevOps.Multitool.Tests.Integration/Alloyed.DevOps.Multitool.Tests.Integration.csproj"
$smokeScript = "tests/powershell/Smoke.Module.Tests.ps1"
$portsSyncScript = "tools/ports/Sync-PortsFromCatalog.ps1"
$generatedPortsTargets = @(
    "src/powershell/Alloyed.DevOps.Multitool.psm1",
    "src/powershell/Alloyed.DevOps.Multitool.psd1"
)

function Invoke-Step {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [scriptblock]$Action
    )

    Write-Host ""
    Write-Host "==> $Name" -ForegroundColor Cyan

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    & $Action
    $exitCode = $LASTEXITCODE
    $sw.Stop()

    if ($exitCode -ne 0) {
        throw "Step failed: $Name (exit code $exitCode)"
    }

    Write-Host "<== $Name completed in $($sw.Elapsed.ToString())" -ForegroundColor Green
}

function Get-TestArgs {
    param([string]$ProjectPath)

    $args = @("test", $ProjectPath, "-c", "Debug", "--nologo", "--no-restore")

    if (-not [string]::IsNullOrWhiteSpace($Filter)) {
        $args += @("--filter", $Filter)
    }

    return ,$args
}

function Invoke-VerifyPortsSync {
    Invoke-Step -Name "Verify ports sync is up-to-date" -Action {
        pwsh -NoProfile -File $portsSyncScript

        $changed = git status --porcelain -- $generatedPortsTargets
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to evaluate git status for generated ports targets."
        }

        if (-not [string]::IsNullOrWhiteSpace(($changed | Out-String))) {
            $changedText = ($changed | Out-String).Trim()
            throw @"
Generated ports files are out-of-date. Run:
  pwsh -NoProfile -File $portsSyncScript

Changed files:
$changedText
"@
        }
    }
}

if ($Restore) {
    Invoke-Step -Name "Restore" -Action {
        dotnet restore $solution --nologo
    }
}

switch ($Stage) {
    "fast" {
        Invoke-Step -Name "Unit tests (fast loop)" -Action {
            dotnet @((Get-TestArgs -ProjectPath $unitProject))
        }
    }

    "build" {
        Invoke-Step -Name "Build solution" -Action {
            $args = @("build", $solution, "-c", "Debug", "--nologo")
            if (-not $Restore) {
                $args += "--no-restore"
            }

            dotnet $args
        }
    }

    "unit" {
        Invoke-Step -Name "Unit tests" -Action {
            dotnet @((Get-TestArgs -ProjectPath $unitProject))
        }
    }

    "integration" {
        Invoke-Step -Name "Integration tests" -Action {
            dotnet @((Get-TestArgs -ProjectPath $integrationProject))
        }
    }

    "smoke" {
        Invoke-Step -Name "PowerShell smoke" -Action {
            pwsh -NoProfile -File $smokeScript
        }
    }

    "full" {
        Invoke-VerifyPortsSync

        Invoke-Step -Name "Build solution" -Action {
            $args = @("build", $solution, "-c", "Debug", "--nologo")
            if (-not $Restore) {
                $args += "--no-restore"
            }

            dotnet $args
        }

        Invoke-Step -Name "Unit tests" -Action {
            dotnet @((Get-TestArgs -ProjectPath $unitProject))
        }

        Invoke-Step -Name "Integration tests" -Action {
            dotnet @((Get-TestArgs -ProjectPath $integrationProject))
        }

        Invoke-Step -Name "PowerShell smoke" -Action {
            pwsh -NoProfile -File $smokeScript
        }
    }

    "ci" {
        Invoke-VerifyPortsSync

        Invoke-Step -Name "Restore" -Action {
            dotnet restore $solution --nologo
        }

        Invoke-Step -Name "Build solution" -Action {
            dotnet build $solution -c Debug --nologo --no-restore
        }

        Invoke-Step -Name "Unit tests" -Action {
            dotnet @((Get-TestArgs -ProjectPath $unitProject))
        }

        Invoke-Step -Name "Integration tests" -Action {
            dotnet @((Get-TestArgs -ProjectPath $integrationProject))
        }

        Invoke-Step -Name "PowerShell smoke" -Action {
            pwsh -NoProfile -File $smokeScript
        }
    }

    "setup" {
        Invoke-Step -Name "Install git hooks" -Action {
            git config core.hooksPath .githooks
            if ($IsLinux -or $IsMacOS) {
                chmod +x .githooks/pre-commit
                chmod +x .githooks/pre-push
            }
        }
        Write-Host "  pre-commit hook active: format check + PS lint" -ForegroundColor DarkGray
        Write-Host "  pre-push   hook active: build + unit tests + integration tests" -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "Done. Stage '$Stage' completed." -ForegroundColor Green
