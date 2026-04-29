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
            $outputPrompt = [Spectre.Console.SelectionPrompt[string]]::new()
            $outputPrompt.Title = "Select console output mode:"
            $null = $outputPrompt.AddChoice('Plain')
            $null = $outputPrompt.AddChoice('Rich')
            $outputMode = [Spectre.Console.AnsiConsole]::Prompt[string]($outputPrompt)

            $transparencyPrompt = [Spectre.Console.ConfirmationPrompt]::new("Enable transparency by default?")
            $enableTransparency = [Spectre.Console.AnsiConsole]::Prompt[bool]($transparencyPrompt)

            $sessionPrompt = [Spectre.Console.ConfirmationPrompt]::new("Enable session mode by default?")
            $enableSession = [Spectre.Console.AnsiConsole]::Prompt[bool]($sessionPrompt)

            $retryPrompt = [Spectre.Console.SelectionPrompt[string]]::new()
            $retryPrompt.Title = "Select runtime retry policy:"
            $null = $retryPrompt.AddChoice('0')
            $null = $retryPrompt.AddChoice('1')
            $null = $retryPrompt.AddChoice('2')
            $null = $retryPrompt.AddChoice('3')
            $maxRetriesRaw = [Spectre.Console.AnsiConsole]::Prompt[string]($retryPrompt)
            $maxRetries = [int]$maxRetriesRaw

            $backoffPrompt = [Spectre.Console.ConfirmationPrompt]::new("Enable exponential backoff?")
            $enableBackoff = [Spectre.Console.AnsiConsole]::Prompt[bool]($backoffPrompt)

            $previewPrompt = [Spectre.Console.ConfirmationPrompt]::new("Enable runtime preview logs?")
            $enablePreview = [Spectre.Console.AnsiConsole]::Prompt[bool]($previewPrompt)

            $profilePrompt = [Spectre.Console.SelectionPrompt[string]]::new()
            $profilePrompt.Title = "Select transparency output profile:"
            $null = $profilePrompt.AddChoice('standard')
            $null = $profilePrompt.AddChoice('minimal')
            $null = $profilePrompt.AddChoice('debug')
            $transparencyProfile = [Spectre.Console.AnsiConsole]::Prompt[string]($profilePrompt)
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
            $null = Apply-AlloyedRuntimeConfig -BasePath $BasePath
        }
    }

    if ($useSpectre) {
        try {
            $table = [Spectre.Console.Table]::new()
            $null = $table.AddColumn('Setting')
            $null = $table.AddColumn('Value')
            $null = $table.AddRow('ConfigPath', $configPath)
            $null = $table.AddRow('OutputMode (process)', $outputMode)
            $null = $table.AddRow('EnableTransparency', [string]$enableTransparency)
            $null = $table.AddRow('TransparencyProfile', [string]$transparencyProfile)
            $null = $table.AddRow('SessionEnabled', [string]$enableSession)
            $null = $table.AddRow('RuntimeMaxRetries (process)', [string]$maxRetries)
            $null = $table.AddRow('RuntimeExponentialBackoff (process)', [string]$enableBackoff)
            $null = $table.AddRow('RuntimePreview (process)', [string]$enablePreview)
            [Spectre.Console.AnsiConsole]::Write($table)
        } catch {
            $useSpectre = $false
        }
    }

    if (-not $useSpectre) {
        Write-Host "ConfigPath                : $configPath"
        Write-Host "OutputMode                : $outputMode"
        Write-Host "EnableTransparency        : $enableTransparency"
        Write-Host "TransparencyProfile       : $transparencyProfile"
        Write-Host "SessionEnabled            : $enableSession"
        Write-Host "RuntimeMaxRetries         : $maxRetries"
        Write-Host "RuntimeExponentialBackoff : $enableBackoff"
        Write-Host "RuntimePreview            : $enablePreview"
    }

    [pscustomobject]@{
        ConfigPath = $configPath
        OutputMode = $outputMode
        EnableTransparency = $enableTransparency
        TransparencyProfile = $transparencyProfile
        SessionEnabled = $enableSession
        RuntimeMaxRetries = $maxRetries
        RuntimeExponentialBackoff = $enableBackoff
        RuntimePreview = $enablePreview
    }
}

function Test-AlloyedRuntimeConfig {
    [CmdletBinding()]
    param(
        [Parameter()] [string]$BasePath = (Get-Location).Path
    )

    $effective = Get-AlloyedRuntimeConfiguration -BasePath $BasePath
    $policy = Get-AlloyedRuntimeExecutionPolicy

    [pscustomobject]@{
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
