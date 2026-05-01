function Get-AlloyedProjectRoot {
    return Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

function Get-AlloyedRuntimeConfigFilePath {
    param(
        [string]$BasePath = (Get-Location).Path
    )

    return Join-Path $BasePath 'config/appsettings.json'
}

function Read-AlloyedRuntimeConfigFile {
    param(
        [Parameter(Mandatory)] [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return @{
            Alloyed = @{
                Runtime = @{}
                Session = @{}
                Decoration = @{}
                Mocking = @{}
                Catalog = @{}
            }
        }
    }

    return Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json -AsHashtable
}

function Write-AlloyedRuntimeConfigFile {
    param(
        [Parameter(Mandatory)] [string]$Path,
        [Parameter(Mandatory)] [hashtable]$Config
    )

    $dir = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -Path $dir -ItemType Directory -Force | Out-Null
    }

    $json = $Config | ConvertTo-Json -Depth 10
    Set-Content -LiteralPath $Path -Value $json
}

function Initialize-AlloyedRuntimeConfig {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter()] [string]$BasePath = (Get-AlloyedProjectRoot),
        [Parameter()] [switch]$Force,
        [Parameter()] [bool]$ApplyToCurrentSession = $true
    )

    $useSpectre = $true
    try {
        Initialize-AlloyedInternalSpectreRuntime
    } catch {
        $useSpectre = $false
        Write-Verbose "Spectre runtime unavailable, using plain prompts. $($_.Exception.Message)"
    }

    $configPath = Get-AlloyedRuntimeConfigFilePath -BasePath $BasePath
    if ((Test-Path -LiteralPath $configPath) -and -not $Force.IsPresent) {
        throw "Config already exists at '$configPath'. Use -Force to overwrite."
    }

    if ($useSpectre) {
        try {
            $selection = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.RuntimeConfigurationPromptService]::PromptDefaults()
            $outputMode = [string]$selection.OutputMode
            $enableTransparency = [bool]$selection.EnableTransparency
            $enableSession = [bool]$selection.EnableSession
            $maxRetries = [int]$selection.MaxRetries
            $enableBackoff = [bool]$selection.EnableBackoff
            $enablePreview = [bool]$selection.EnablePreview
            $transparencyProfile = [string]$selection.TransparencyProfile
        } catch {
            $useSpectre = $false
            Write-Verbose "Spectre prompts failed, using plain prompts. $($_.Exception.Message)"
        }
    }

    if (-not $useSpectre) {
        $outputModeInput = Read-Host "Output mode [Plain/Rich] (Plain)"
        if ($outputModeInput -match '^(?i)rich$') { $outputMode = 'Rich' } else { $outputMode = 'Plain' }

        $enableTransparency = ((Read-Host "Enable transparency by default? [y/n] (y)") -notmatch '^(?i)n')
        $enableSession = ((Read-Host "Enable session mode by default? [y/n] (n)") -match '^(?i)y')

        $maxRetriesInput = Read-Host "Runtime max retries [0-3] (1)"
        if (-not ($maxRetriesInput -match '^[0-3]$')) { $maxRetriesInput = '1' }
        $maxRetries = [int]$maxRetriesInput

        $enableBackoff = ((Read-Host "Enable exponential backoff? [y/n] (y)") -notmatch '^(?i)n')
        $enablePreview = ((Read-Host "Enable runtime preview logs? [y/n] (n)") -match '^(?i)y')

        $profileInput = Read-Host "Transparency profile [standard/minimal/debug] (standard)"
        switch -Regex ($profileInput) {
            '^(?i)minimal$' { $transparencyProfile = 'minimal'; break }
            '^(?i)debug$' { $transparencyProfile = 'debug'; break }
            default { $transparencyProfile = 'standard'; break }
        }
    }

    $runtimeConfig = @{
        Alloyed = @{
            Runtime = @{
                DefaultOutputPath = 'out'
            }
            Session = @{
                Enabled = $enableSession
            }
            Decoration = @{
                EnableErrorHandling = $true
                EnableObservability = $true
                EnableCorrelation = $true
                EnableTransparency = $enableTransparency
                TransparencyProfile = $transparencyProfile
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

    if ($PSCmdlet.ShouldProcess($configPath, 'Write runtime config file')) {
        Write-AlloyedRuntimeConfigFile -Path $configPath -Config $runtimeConfig
        [System.Environment]::SetEnvironmentVariable('ALLOYED_CONSOLE_OUTPUT_MODE', $outputMode, 'Process')
        [System.Environment]::SetEnvironmentVariable('ALLOYED_RUNTIME_MAX_RETRIES', [string]$maxRetries, 'Process')
        [System.Environment]::SetEnvironmentVariable('ALLOYED_RUNTIME_EXPONENTIAL_BACKOFF', [string]$enableBackoff, 'Process')
        [System.Environment]::SetEnvironmentVariable('ALLOYED_RUNTIME_PREVIEW', [string]$enablePreview, 'Process')

        if ($ApplyToCurrentSession) {
            $null = Set-AlloyedRuntimeConfig -BasePath $BasePath
        }
    }

    $reporter = Get-AlloyedConsoleReporter
    [Alloyed.DevOps.Multitool.Host.PowerShell.Services.RuntimeConfigurationConsolePresenter]::WriteInitializationSummary(
        $reporter,
        $configPath,
        $outputMode,
        [bool]$enableTransparency,
        [string]$transparencyProfile,
        [bool]$enableSession,
        [int]$maxRetries,
        [bool]$enableBackoff,
        [bool]$enablePreview,
        [bool]$ApplyToCurrentSession)

    [pscustomobject]@{
        PSTypeName = 'Alloyed.RuntimeConfigInitializationResult'
        ConfigPath = $configPath
        OutputMode = $outputMode
        EnableTransparency = $enableTransparency
        TransparencyProfile = $transparencyProfile
        SessionEnabled = $enableSession
        RuntimeMaxRetries = $maxRetries
        RuntimeExponentialBackoff = $enableBackoff
        RuntimePreview = $enablePreview
        ApplyToCurrentSession = $ApplyToCurrentSession
    }
}

function Test-AlloyedRuntimeConfig {
    [CmdletBinding()]
    param(
        [Parameter()] [string]$BasePath = (Get-Location).Path
    )

    $effective = Get-AlloyedRuntimeConfiguration -BasePath $BasePath
    $policy = Get-AlloyedRuntimeExecutionPolicy
    $reporter = Get-AlloyedConsoleReporter

    [Alloyed.DevOps.Multitool.Host.PowerShell.Services.RuntimeConfigurationConsolePresenter]::WriteValidationSummary(
        $reporter,
        $BasePath,
        (Get-AlloyedRuntimeConfigFilePath -BasePath $BasePath),
        [string]$effective.Runtime.DefaultOutputPath,
        [bool]$effective.Session.Enabled,
        [bool]$effective.Decoration.EnableTransparency,
        (Resolve-AlloyedConsoleOutputMode).ToString(),
        [int]$policy.MaxRetries,
        [int]$policy.RetryDelaySec,
        [bool]$policy.ExponentialBackoff,
        [bool]$policy.Preview,
        [int]$policy.TimeoutSec)

    [pscustomobject]@{
        PSTypeName = 'Alloyed.RuntimeConfigValidationResult'
        BasePath = $BasePath
        ConfigPath = Get-AlloyedRuntimeConfigFilePath -BasePath $BasePath
        RuntimeDefaultOutputPath = $effective.Runtime.DefaultOutputPath
        SessionEnabled = $effective.Session.Enabled
        TransparencyEnabled = $effective.Decoration.EnableTransparency
        ConsoleOutputMode = (Resolve-AlloyedConsoleOutputMode).ToString()
        RuntimeMaxRetries = $policy.MaxRetries
        RuntimeRetryDelaySec = $policy.RetryDelaySec
        RuntimeExponentialBackoff = $policy.ExponentialBackoff
        RuntimePreview = $policy.Preview
        RuntimeTimeoutSec = $policy.TimeoutSec
    }
}

function Get-AlloyedRuntimeExecutionPolicy {
    [CmdletBinding()]
    param()

    $maxRetries = 0
    $retryDelaySec = 2
    $exponentialBackoff = $false
    $preview = $false
    $timeoutSec = 0

    $rawRetries = [System.Environment]::GetEnvironmentVariable('ALLOYED_RUNTIME_MAX_RETRIES')
    if ([int]::TryParse($rawRetries, [ref]$maxRetries) -and $maxRetries -ge 0) {
        $maxRetries = [int]$maxRetries
    } else {
        $maxRetries = 0
    }

    $rawDelay = [System.Environment]::GetEnvironmentVariable('ALLOYED_RUNTIME_RETRY_DELAY_SEC')
    if ([int]::TryParse($rawDelay, [ref]$retryDelaySec) -and $retryDelaySec -ge 0) {
        $retryDelaySec = [int]$retryDelaySec
    } else {
        $retryDelaySec = 2
    }

    $rawBackoff = [System.Environment]::GetEnvironmentVariable('ALLOYED_RUNTIME_EXPONENTIAL_BACKOFF')
    if ([bool]::TryParse($rawBackoff, [ref]$exponentialBackoff)) {
        $exponentialBackoff = [bool]$exponentialBackoff
    } else {
        $exponentialBackoff = $false
    }

    $rawPreview = [System.Environment]::GetEnvironmentVariable('ALLOYED_RUNTIME_PREVIEW')
    if ([bool]::TryParse($rawPreview, [ref]$preview)) {
        $preview = [bool]$preview
    } else {
        $preview = $false
    }

    $rawTimeout = [System.Environment]::GetEnvironmentVariable('ALLOYED_RUNTIME_TIMEOUT_SEC')
    if ([int]::TryParse($rawTimeout, [ref]$timeoutSec) -and $timeoutSec -ge 0) {
        $timeoutSec = [int]$timeoutSec
    } else {
        $timeoutSec = 0
    }

    [pscustomobject]@{
        MaxRetries = $maxRetries
        RetryDelaySec = $retryDelaySec
        ExponentialBackoff = $exponentialBackoff
        Preview = $preview
        TimeoutSec = $timeoutSec
    }
}
