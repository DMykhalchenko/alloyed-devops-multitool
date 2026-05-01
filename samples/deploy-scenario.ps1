#Requires -Version 7.0
<#
.SYNOPSIS
    Simulated multi-phase production deployment pipeline.

.DESCRIPTION
    Realistic deployment scenario designed to demonstrate the Alloyed module's decoration,
    transparency, and transform features. All operations are simulated (no real infrastructure
    is touched). Run the script in the following ways:

    # Plain run — no decoration
    ./samples/deploy-scenario.ps1

    # With Alloyed transparency and session interception active
    Import-Module ./src/powershell/Alloyed.DevOps.Multitool.psd1
    Invoke-AlloyedScript -ScriptPath ./samples/deploy-scenario.ps1 -ArgumentList @('-Environment', 'staging')

    # Dry-run the AST transform to see which commands would be wrapped
    Import-Module ./src/powershell/Alloyed.DevOps.Multitool.psd1
    Test-AlloyedTransform -ScriptPath ./samples/deploy-scenario.ps1

.PARAMETER Environment
    Target environment name. Defaults to 'prod'.

.PARAMETER Version
    Release version string. Defaults to a timestamp-based string.

.PARAMETER SimulateFailure
    Injects a failure during the WorkerService artifact deploy to exercise the rollback path.

.PARAMETER RetryDatabaseScript
    Simulates a transient database script failure on the first attempt to exercise retry logic.
#>
param(
    [string]$Environment = "prod",
    [string]$Version = "2026.05.01.42",
    [switch]$SimulateFailure,
    [switch]$RetryDatabaseScript
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Write-Phase {
    param([string]$Name)
    Write-Host ""
    Write-Host "=== $Name ==="
}

function Invoke-FakeWork {
    param(
        [string]$Name,
        [int]$Seconds = 1
    )

    Write-Host "    $Name"
    Start-Sleep -Milliseconds ($Seconds * 400)
}

function Get-SimulatedWorkspacePath {
    param([string]$Relative)
    $root = Join-Path ([System.IO.Path]::GetTempPath()) "alloyed-deploy-sim"
    if (-not (Test-Path -Path $root)) {
        New-Item -Path $root -ItemType Directory -Force | Out-Null
    }
    if ([string]::IsNullOrWhiteSpace($Relative)) { return $root }
    Join-Path $root $Relative
}

# ---------------------------------------------------------------------------
# Phases
# ---------------------------------------------------------------------------

function Connect-DeploymentTarget {
    Write-Phase "Connect"
    Invoke-FakeWork "Resolving environment endpoint: $Environment"
    Invoke-FakeWork "Loading credentials from secure storage"
    Invoke-FakeWork "Validating access policy for version $Version"

    $configDir = Get-SimulatedWorkspacePath "config"
    if (-not (Test-Path -Path $configDir)) {
        New-Item -Path $configDir -ItemType Directory -Force | Out-Null
    }

    $endpointConfig = @{
        Environment = $Environment
        Endpoint    = "https://$Environment.internal/api"
        ConnectedAt = (Get-Date -Format 'o')
    } | ConvertTo-Json

    Set-Content -LiteralPath (Join-Path $configDir "endpoint.json") -Value $endpointConfig
    Write-Host "    Endpoint config written."
}

function Get-DeploymentManifest {
    Invoke-FakeWork "Resolving deployment manifest for $Version"

    $manifestPath = Get-SimulatedWorkspacePath "manifest.json"

    $manifest = [pscustomobject]@{
        Version  = $Version
        Services = @(
            "IdentityService",
            "ContentApi",
            "WorkerService",
            "ReportingService"
        )
        DatabaseScripts = @(
            "001_precheck.sql",
            "002_schema.sql",
            "003_indexes.sql"
        )
    }

    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath
    Write-Host "    Manifest written to: $manifestPath"

    $raw = Get-Content -LiteralPath $manifestPath -Raw
    return ($raw | ConvertFrom-Json)
}

function Invoke-PreDeploymentChecks {
    param($Manifest)

    Write-Phase "Pre-deployment checks"
    Invoke-FakeWork "Running pre-flight checklist"
    Test-ServiceAvailability -Services $Manifest.Services
    Test-DatabaseState
    Test-StorageState
}

function Test-ServiceAvailability {
    param([string[]]$Services)

    foreach ($service in $Services) {
        Invoke-FakeWork "Checking health of $service"
    }
}

function Test-DatabaseState {
    Invoke-FakeWork "Checking database connectivity"
    Invoke-FakeWork "Checking active locks and migration history"
}

function Test-StorageState {
    $artifactStore = Get-SimulatedWorkspacePath "artifacts"
    if (-not (Test-Path -Path $artifactStore)) {
        New-Item -Path $artifactStore -ItemType Directory -Force | Out-Null
    }

    Invoke-FakeWork "Checking shared storage mount"
    Invoke-FakeWork "Verifying artifact store at $artifactStore"
}

function Backup-ProductionState {
    param($Manifest)

    Write-Phase "Backup"
    Invoke-FakeWork "Creating production snapshot"
    Backup-Database
    Backup-ServiceConfiguration -Services $Manifest.Services
}

function Backup-Database {
    Invoke-FakeWork "Starting database backup"

    $backupDir = Get-SimulatedWorkspacePath "backup"
    if (-not (Test-Path -Path $backupDir)) {
        New-Item -Path $backupDir -ItemType Directory -Force | Out-Null
    }

    $backupFile = Join-Path $backupDir "db-$Version.bak"
    Set-Content -LiteralPath $backupFile -Value "SIMULATED_BACKUP_$Version"
    Invoke-FakeWork "Uploading database backup metadata"
}

function Backup-ServiceConfiguration {
    param([string[]]$Services)

    foreach ($service in $Services) {
        Invoke-FakeWork "Exporting configuration for $service"
    }
}

function Stop-ProductionTraffic {
    param($Manifest)

    Write-Phase "Drain traffic"
    Invoke-FakeWork "Draining in-flight requests"

    foreach ($service in $Manifest.Services) {
        Stop-ServiceInstance -ServiceName $service
    }
}

function Stop-ServiceInstance {
    param([string]$ServiceName)

    Invoke-FakeWork "Stopping $ServiceName"
    Invoke-FakeWork "Waiting for $ServiceName to go idle"
}

function Deploy-ProductionRelease {
    param($Manifest)

    Write-Phase "Deploy"
    Invoke-FakeWork "Starting release deployment for $Version"

    Deploy-DatabaseChanges -Scripts $Manifest.DatabaseScripts
    Deploy-ServiceArtifacts -Services $Manifest.Services
    Deploy-Configuration   -Services $Manifest.Services
}

function Deploy-DatabaseChanges {
    param([string[]]$Scripts)

    foreach ($script in $Scripts) {
        $firstAttemptShouldFail = $RetryDatabaseScript.IsPresent -and $script -eq "002_schema.sql"
        Invoke-RetryableOperation -Name "Executing $script" -Operation {
            if ($firstAttemptShouldFail) {
                $script:_schemaAttempt = ($script:_schemaAttempt ?? 0) + 1
                if ($script:_schemaAttempt -eq 1) {
                    throw "Transient lock timeout on $script"
                }
            }
            Invoke-FakeWork "Applying $script"
        }
    }
}

function Deploy-ServiceArtifacts {
    param([string[]]$Services)

    foreach ($service in $Services) {
        Deploy-SingleServiceArtifact -ServiceName $service
    }
}

function Deploy-SingleServiceArtifact {
    param([string]$ServiceName)

    Invoke-FakeWork "Pulling artifact for $ServiceName"
    Invoke-FakeWork "Validating checksum for $ServiceName"

    if ($SimulateFailure.IsPresent -and $ServiceName -eq "WorkerService") {
        throw "Simulated artifact corruption for $ServiceName"
    }

    $artifactPath = Get-SimulatedWorkspacePath "artifacts\$ServiceName-$Version"
    Set-Content -LiteralPath $artifactPath -Value "ARTIFACT_$ServiceName"
    Invoke-FakeWork "Installed artifact: $ServiceName"
}

function Deploy-Configuration {
    param([string[]]$Services)

    foreach ($service in $Services) {
        Invoke-FakeWork "Applying configuration to $service"
        Invoke-FakeWork "Reloading secrets for $service"
    }
}

function Start-ProductionTraffic {
    param($Manifest)

    Write-Phase "Enable traffic"

    foreach ($service in $Manifest.Services) {
        Start-ServiceInstance -ServiceName $service
    }

    Invoke-FakeWork "Restoring load balancer routing"
}

function Start-ServiceInstance {
    param([string]$ServiceName)

    Invoke-FakeWork "Starting $ServiceName"
    Invoke-FakeWork "Waiting for $ServiceName to pass health check"
}

function Invoke-PostDeploymentValidation {
    param($Manifest)

    Write-Phase "Post-deployment validation"
    Invoke-FakeWork "Running post-deploy smoke tests"

    foreach ($service in $Manifest.Services) {
        Test-ServiceHealth -ServiceName $service
    }

    Test-EndToEndFlow
}

function Test-ServiceHealth {
    param([string]$ServiceName)

    Invoke-FakeWork "Calling health endpoint for $ServiceName"
    Invoke-FakeWork "Scanning recent logs for $ServiceName"
}

function Test-EndToEndFlow {
    Invoke-FakeWork "Executing synthetic end-to-end transaction"
    Invoke-FakeWork "Asserting transaction result"
}

function Invoke-Rollback {
    param($Manifest)

    Write-Phase "Rollback"
    Write-Warning "Rollback triggered for $Version in $Environment."

    Invoke-FakeWork "Disabling production traffic"

    foreach ($service in $Manifest.Services) {
        Invoke-FakeWork "Reverting artifact for $service"
        Invoke-FakeWork "Restoring configuration for $service"
    }

    Invoke-FakeWork "Restoring database from backup"
    Invoke-FakeWork "Re-enabling previous production version"

    Write-Warning "Rollback complete."
}

function Invoke-RetryableOperation {
    param(
        [string]$Name,
        [scriptblock]$Operation,
        [int]$Attempts = 3
    )

    for ($i = 1; $i -le $Attempts; $i++) {
        try {
            Write-Host "    [$i/$Attempts] $Name"
            & $Operation
            return
        }
        catch {
            Write-Warning "Attempt $i/$Attempts failed: $($_.Exception.Message)"

            if ($i -eq $Attempts) {
                throw
            }

            Start-Sleep -Seconds 1
        }
    }
}

# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

function Invoke-ProductionDeployment {
    $manifest = $null

    try {
        Write-Host "Production deployment: $Environment / $Version"

        Connect-DeploymentTarget

        $manifest = Get-DeploymentManifest

        Invoke-PreDeploymentChecks -Manifest $manifest
        Backup-ProductionState     -Manifest $manifest
        Stop-ProductionTraffic     -Manifest $manifest
        Deploy-ProductionRelease   -Manifest $manifest
        Start-ProductionTraffic    -Manifest $manifest
        Invoke-PostDeploymentValidation -Manifest $manifest

        Write-Phase "Done"
        Write-Host "Deployment $Version to $Environment succeeded."
    }
    catch {
        Write-Error "Deployment failed: $($_.Exception.Message)"

        if ($null -ne $manifest) {
            Invoke-Rollback -Manifest $manifest
        }

        throw
    }
}

Invoke-ProductionDeployment
