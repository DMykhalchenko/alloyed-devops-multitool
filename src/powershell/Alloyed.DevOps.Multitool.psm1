$script:AssemblyLoaded = $false
$script:SessionModeEnabled = $false
$script:SessionModeAliases = @()
$script:SessionModeAliasBackup = @{}
$script:SessionModeCommandBackup = @{}
$script:DecorationPipeline = $null
$script:TransparencyModeOverride = $null
$script:ConsoleOutputModeOverride = $null
$script:LastRuntimeExecution = $null

function Initialize-AlloyedHostAssembly {
    if ($script:AssemblyLoaded) { return }

    $packagedDllPath = Join-Path $PSScriptRoot 'lib/Alloyed.DevOps.Multitool.Host.PowerShell.dll'
    $packagedDecorationDllPath = Join-Path $PSScriptRoot 'lib/Alloyed.DevOps.Multitool.Core.Decoration.dll'

    $moduleRoot = Split-Path -Parent $PSScriptRoot
    $devDllPath = Join-Path $moduleRoot 'dotnet/Alloyed.DevOps.Multitool.Host.PowerShell/bin/Debug/net8.0/Alloyed.DevOps.Multitool.Host.PowerShell.dll'
    $devDecorationDllPath = Join-Path $moduleRoot 'dotnet/Alloyed.DevOps.Multitool.Core.Decoration/bin/Debug/net8.0/Alloyed.DevOps.Multitool.Core.Decoration.dll'

    if ((Test-Path $packagedDllPath) -and (Test-Path $packagedDecorationDllPath)) {
        $dllPath = $packagedDllPath
        $decorationDllPath = $packagedDecorationDllPath
    } else {
        $dllPath = $devDllPath
        $decorationDllPath = $devDecorationDllPath
    }

    if (-not (Test-Path $dllPath)) {
        throw "Host assembly not found at '$dllPath'. Build solution first."
    }
    if (-not (Test-Path $decorationDllPath)) {
        throw "Decoration assembly not found at '$decorationDllPath'. Build solution first."
    }

    Add-Type -Path $decorationDllPath
    Add-Type -Path $dllPath
    $script:AssemblyLoaded = $true
}

function Resolve-FailOnSeverity {
    param(
        [string]$FailOnSeverity,
        [switch]$FailOnWarnings
    )

    if (-not [string]::IsNullOrWhiteSpace($FailOnSeverity)) {
        return [Alloyed.DevOps.Multitool.Host.PowerShell.Models.PipelineDiagnosticSeverity]::$FailOnSeverity
    }

    if ($FailOnWarnings.IsPresent) {
        return [Alloyed.DevOps.Multitool.Host.PowerShell.Models.PipelineDiagnosticSeverity]::Warning
    }

    return $null
}

function Initialize-AlloyedDecorationPipeline {
    if ($null -ne $script:DecorationPipeline) { return }

    Initialize-AlloyedHostAssembly

    $nullSink = [Alloyed.DevOps.Multitool.Core.Decoration.Services.NullDecorationSink]::new()
    $consoleSink = [Alloyed.DevOps.Multitool.Core.Decoration.Services.ConsoleDecorationSink]::new()

    $decorators = [System.Collections.Generic.List[Alloyed.DevOps.Multitool.Core.Decoration.Contracts.IDecorator]]::new()
    $decorators.Add([Alloyed.DevOps.Multitool.Core.Decoration.Decorators.ErrorHandlingDecorator]::new())
    $decorators.Add([Alloyed.DevOps.Multitool.Core.Decoration.Decorators.CorrelationDecorator]::new())
    $decorators.Add([Alloyed.DevOps.Multitool.Core.Decoration.Decorators.ObservabilityDecorator]::new($nullSink))
    $decorators.Add([Alloyed.DevOps.Multitool.Core.Decoration.Decorators.TransparencyDecorator]::new($consoleSink))

    $script:DecorationPipeline = [Alloyed.DevOps.Multitool.Core.Decoration.Services.DecorationPipeline]::new($decorators)
}

function Resolve-AlloyedTransparencyEnabled {
    if ($null -ne $script:TransparencyModeOverride) {
        return [bool]$script:TransparencyModeOverride
    }

    $configuration = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.PipelineBootstrap]::CreateRuntimeConfiguration((Get-Location).Path, $null)
    return [bool]$configuration.Decoration.EnableTransparency
}

function Resolve-AlloyedConsoleOutputMode {
    if ($null -ne $script:ConsoleOutputModeOverride) {
        return $script:ConsoleOutputModeOverride
    }

    $environmentMode = [System.Environment]::GetEnvironmentVariable('ALLOYED_CONSOLE_OUTPUT_MODE')
    if (-not [string]::IsNullOrWhiteSpace($environmentMode)) {
        if ($environmentMode -ieq 'rich') {
            return [Alloyed.DevOps.Multitool.Host.PowerShell.Services.ConsoleOutputMode]::Rich
        }
    }

    return [Alloyed.DevOps.Multitool.Host.PowerShell.Services.ConsoleOutputMode]::Plain
}

function Get-AlloyedPortsCatalogPath {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot 'tools/ports/ports.catalog.json'
}

function Get-AlloyedNativeCommandMap {
    $catalogPath = Get-AlloyedPortsCatalogPath
    $nativeMap = @{}

    if (-not (Test-Path -LiteralPath $catalogPath)) {
        return $nativeMap
    }

    $entries = @(Get-Content -Raw -LiteralPath $catalogPath | ConvertFrom-Json)
    foreach ($entry in $entries) {
        if ([string]::IsNullOrWhiteSpace($entry.command) -or [string]::IsNullOrWhiteSpace($entry.native)) {
            continue
        }

        $nativeMap[[string]$entry.command] = [string]$entry.native
        foreach ($alias in @($entry.aliases)) {
            if (-not [string]::IsNullOrWhiteSpace($alias)) {
                $nativeMap[[string]$alias] = [string]$entry.native
            }
        }
    }

    return $nativeMap
}

function Get-AlloyedProjectRoot {
    return Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

function Get-AlloyedRuntimeConfigFilePath {
    param(
        [string]$BasePath = (Get-Location).Path
    )

    return Join-Path $BasePath 'config/appsettings.json'
}

function Initialize-AlloyedInternalSpectreRuntime {
    [CmdletBinding()]
    param()

    Initialize-AlloyedHostAssembly

    if ($null -ne ('Spectre.Console.AnsiConsole' -as [type])) {
        return
    }

    $hostAssemblyPath = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.PipelineBootstrap].Assembly.Location
    if ([string]::IsNullOrWhiteSpace($hostAssemblyPath)) {
        throw "Unable to resolve Host.PowerShell assembly location for Spectre bootstrap."
    }

    $hostAssemblyDir = Split-Path -Parent $hostAssemblyPath
    $spectreDllPath = Join-Path $hostAssemblyDir 'Spectre.Console.dll'

    if (-not (Test-Path -LiteralPath $spectreDllPath)) {
        throw "Spectre.Console assembly was not found at '$spectreDllPath'. Build host project first: dotnet build src/dotnet/Alloyed.DevOps.Multitool.Host.PowerShell -c Debug"
    }

    Add-Type -Path $spectreDllPath

    if ($null -eq ('Spectre.Console.AnsiConsole' -as [type])) {
        throw "Spectre.Console types are still unavailable after loading '$spectreDllPath'."
    }
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

function Resolve-AlloyedTransparencyProfileFromConfig {
    param(
        [Parameter(Mandatory)] [string]$BasePath
    )

    $configPath = Get-AlloyedRuntimeConfigFilePath -BasePath $BasePath
    $config = Read-AlloyedRuntimeConfigFile -Path $configPath

    $profile = $null
    if ($config.ContainsKey('Alloyed') -and
        $config['Alloyed'] -is [hashtable] -and
        $config['Alloyed'].ContainsKey('Decoration') -and
        $config['Alloyed']['Decoration'] -is [hashtable] -and
        $config['Alloyed']['Decoration'].ContainsKey('TransparencyProfile')) {
        $profile = [string]$config['Alloyed']['Decoration']['TransparencyProfile']
    }

    if ([string]::IsNullOrWhiteSpace($profile)) {
        return 'standard'
    }

    switch -Regex ($profile.Trim().ToLowerInvariant()) {
        '^minimal$' { return 'minimal' }
        '^debug$' { return 'debug' }
        default { return 'standard' }
    }
}

function Initialize-AlloyedWrappersFromCatalog {
    [CmdletBinding()]
    param()

    $catalogPath = Get-AlloyedPortsCatalogPath
    if (-not (Test-Path -LiteralPath $catalogPath)) {
        return
    }

    $entries = @(Get-Content -Raw -LiteralPath $catalogPath | ConvertFrom-Json)
    foreach ($entry in $entries) {
        $wrapperName = [string]$entry.wrapper
        $operation = [string]$entry.command
        $native = [string]$entry.native

        if ([string]::IsNullOrWhiteSpace($wrapperName) -or [string]::IsNullOrWhiteSpace($operation) -or [string]::IsNullOrWhiteSpace($native)) {
            continue
        }

        if (Get-Command -Name $wrapperName -ErrorAction SilentlyContinue) {
            continue
        }

        $wrapperBody = if ($operation -eq 'Clear-Host') {
            "function global:$wrapperName { Invoke-AlloyedDecoratedCommand -Operation '$operation' -Parameters @{} -Action { $native } }"
        } else {
            "function global:$wrapperName { Invoke-AlloyedDecoratedCommand -Operation '$operation' -Arguments `$args -InputObjects @(`$input) -Action { $native @args } }"
        }

        $null = Invoke-Expression $wrapperBody
    }
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
        Write-Host "SessionEnabled            : $enableSession"
        Write-Host "RuntimeMaxRetries         : $maxRetries"
        Write-Host "RuntimeExponentialBackoff : $enableBackoff"
        Write-Host "RuntimePreview            : $enablePreview"
    }

    [pscustomobject]@{
        ConfigPath = $configPath
        OutputMode = $outputMode
        EnableTransparency = $enableTransparency
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

function Apply-AlloyedRuntimeConfig {
    [CmdletBinding()]
    param(
        [Parameter()] [string]$BasePath = (Get-AlloyedProjectRoot),
        [Parameter()] [switch]$QuietTransparency
    )

    $effective = Get-AlloyedRuntimeConfiguration -BasePath $BasePath
    $profile = Resolve-AlloyedTransparencyProfileFromConfig -BasePath $BasePath

    if ($effective.Decoration.EnableTransparency) {
        $enableParams = @{
            SkipSessionMode = (-not [bool]$effective.Session.Enabled)
            Profile = $profile
        }
        if ($QuietTransparency.IsPresent) {
            $enableParams['Quiet'] = $true
        }
        $null = Enable-AlloyedTransparencyMode @enableParams
    } else {
        $null = Disable-AlloyedTransparencyMode
        if ($script:SessionModeEnabled) {
            $null = Disable-AlloyedSessionMode
        }
    }

    return Get-AlloyedTransparencyModeStatus
}

function Set-AlloyedTransparencyProfile {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [ValidateSet('minimal','standard','debug')] [string]$Profile
    )

    [System.Environment]::SetEnvironmentVariable('ALLOYED_TRANSPARENCY_PROFILE', $Profile, 'Process')
    return Get-AlloyedTransparencyModeStatus
}

function Get-AlloyedConsoleReporter {
    Initialize-AlloyedHostAssembly

    $isInteractive = -not [System.Console]::IsOutputRedirected
    $mode = Resolve-AlloyedConsoleOutputMode
    return [Alloyed.DevOps.Multitool.Host.PowerShell.Services.ConsoleReporterFactory]::Create($mode, $isInteractive, $null)
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

function Invoke-AlloyedCommandRuntime {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$Operation,
        [Parameter(Mandatory)] [scriptblock]$Action,
        [Parameter()] [object[]]$Arguments = @(),
        [Parameter()] [object[]]$InputObjects = @()
    )

    $policy = Get-AlloyedRuntimeExecutionPolicy
    $attempt = 0
    $delaySec = [int]$policy.RetryDelaySec

    while ($true) {
        $attempt++
        $sw = [System.Diagnostics.Stopwatch]::StartNew()

        try {
            if ($policy.Preview) {
                Write-Host ("[alloyed-runtime] phase=attempt op={0} attempt={1}" -f $Operation, $attempt)
            }

            $output = $null
            if ($policy.TimeoutSec -gt 0 -and $InputObjects.Count -eq 0) {
                $job = Start-Job -ScriptBlock {
                    param([scriptblock]$InnerAction, [object[]]$InnerArgs)
                    & $InnerAction @InnerArgs
                } -ArgumentList $Action, $Arguments

                $completed = Wait-Job -Job $job -Timeout $policy.TimeoutSec
                if (-not $completed) {
                    Stop-Job -Job $job -ErrorAction SilentlyContinue
                    Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
                    throw "Operation '$Operation' exceeded timeout of $($policy.TimeoutSec) seconds."
                }

                try {
                    $output = Receive-Job -Job $job -ErrorAction Stop
                } finally {
                    Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
                }
            } else {
                if ($policy.TimeoutSec -gt 0 -and $InputObjects.Count -gt 0 -and $policy.Preview) {
                    Write-Host ("[alloyed-runtime] phase=timeout-fallback op={0} reason=input-pipeline" -f $Operation)
                }

                $output = if ($InputObjects.Count -gt 0) {
                    $InputObjects | & $Action @Arguments
                } else {
                    & $Action @Arguments
                }
            }

            $sw.Stop()
            $script:LastRuntimeExecution = [pscustomobject]@{
                Operation = $Operation
                Success = $true
                Attempts = $attempt
                DurationMs = $sw.ElapsedMilliseconds
                TimeoutSec = [int]$policy.TimeoutSec
                MaxRetries = [int]$policy.MaxRetries
            }

            return $output
        } catch {
            $sw.Stop()
            $canRetry = $attempt -le [int]$policy.MaxRetries

            if (-not $canRetry) {
                $script:LastRuntimeExecution = [pscustomobject]@{
                    Operation = $Operation
                    Success = $false
                    Attempts = $attempt
                    DurationMs = $sw.ElapsedMilliseconds
                    TimeoutSec = [int]$policy.TimeoutSec
                    MaxRetries = [int]$policy.MaxRetries
                    Error = $_.Exception.Message
                }

                throw
            }

            if ($delaySec -gt 0) {
                Start-Sleep -Seconds $delaySec
            }

            if ($policy.ExponentialBackoff) {
                $delaySec = [Math]::Min($delaySec * 2, 300)
            }
        }
    }
}

function Write-AlloyedPipelineResultSummary {
    param(
        [Parameter(Mandatory)] [object]$Result,
        [Parameter(Mandatory)] [string]$Operation
    )

    $reporter = Get-AlloyedConsoleReporter
    $reporter.WriteHeader($Operation)

    if ($Result.Success) {
        $reporter.WriteMessage([Alloyed.DevOps.Multitool.Host.PowerShell.Contracts.ConsoleMessageLevel]::Info, 'Pipeline completed successfully.')
    } else {
        $reporter.WriteMessage([Alloyed.DevOps.Multitool.Host.PowerShell.Contracts.ConsoleMessageLevel]::Error, 'Pipeline failed.')
    }

    $reporter.WriteKeyValue('CommandsFound', [string]$Result.CommandsFound)
    $reporter.WriteKeyValue('CommandsReplaced', [string]$Result.CommandsReplaced)
    $reporter.WriteKeyValue('MissingCommands', [string]@($Result.MissingCommands).Count)

    if (-not [string]::IsNullOrWhiteSpace($Result.ModulePath)) {
        $reporter.WriteKeyValue('ModulePath', [string]$Result.ModulePath)
    }

    foreach ($diagnostic in @($Result.Diagnostics)) {
        $level = switch ($diagnostic.Severity.ToString()) {
            'Error' { [Alloyed.DevOps.Multitool.Host.PowerShell.Contracts.ConsoleMessageLevel]::Error; break }
            'Warning' { [Alloyed.DevOps.Multitool.Host.PowerShell.Contracts.ConsoleMessageLevel]::Warning; break }
            default { [Alloyed.DevOps.Multitool.Host.PowerShell.Contracts.ConsoleMessageLevel]::Info; break }
        }

        $reporter.WriteMessage($level, ("[{0}] {1}" -f @($diagnostic.Code, $diagnostic.Message)))
    }
}

function Invoke-AlloyedDecoratedCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$Operation,
        [Parameter(Mandatory)] [scriptblock]$Action,
        [hashtable]$Parameters = @{},
        [object[]]$Arguments = @(),
        [object[]]$InputObjects = @()
    )

    Initialize-AlloyedDecorationPipeline

    $tags = [System.Collections.Generic.Dictionary[string,string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $tags['operation'] = $Operation
    $tags['enableTransparency'] = (Resolve-AlloyedTransparencyEnabled).ToString().ToLowerInvariant()
    $tags['transparencyVerbose'] = [System.Environment]::GetEnvironmentVariable('ALLOYED_TRANSPARENCY_VERBOSE')
    $tags['transparencyProfile'] = [System.Environment]::GetEnvironmentVariable('ALLOYED_TRANSPARENCY_PROFILE')

    foreach ($key in $Parameters.Keys) {
        $value = $Parameters[$key]
        if ($null -eq $value) {
            $tags[[string]$key] = '<null>'
            continue
        }

        $tags[[string]$key] = [string]$value
    }

    $context = [Alloyed.DevOps.Multitool.Core.Decoration.Models.DecorationContext]::new($Operation, $tags)
    $invoke = [System.Func[object]] {
        return Invoke-AlloyedCommandRuntime -Operation $Operation -Action $Action -Arguments $Arguments -InputObjects $InputObjects
    }
    return $script:DecorationPipeline.Execute[object]($context, $invoke)
}

# ---------------------------------------------------------------------------
# Provider.FileSystem wrappers
# ---------------------------------------------------------------------------

# <auto-generated:wrappers>
function Clear-AlloyedHost { Invoke-AlloyedDecoratedCommand -Operation 'Clear-Host' -Parameters @{} -Action { Microsoft.PowerShell.Core\Clear-Host } }
function Compress-AlloyedArchive { Invoke-AlloyedDecoratedCommand -Operation 'Compress-Archive' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Archive\Compress-Archive @args } }
function ConvertFrom-AlloyedJson { Invoke-AlloyedDecoratedCommand -Operation 'ConvertFrom-Json' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Utility\ConvertFrom-Json @args } }
function ConvertFrom-AlloyedSecureString { Invoke-AlloyedDecoratedCommand -Operation 'ConvertFrom-SecureString' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Security\ConvertFrom-SecureString @args } }
function ConvertTo-AlloyedJson { Invoke-AlloyedDecoratedCommand -Operation 'ConvertTo-Json' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Utility\ConvertTo-Json @args } }
function ConvertTo-AlloyedSecureString { Invoke-AlloyedDecoratedCommand -Operation 'ConvertTo-SecureString' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Security\ConvertTo-SecureString @args } }
function ConvertTo-AlloyedXml { Invoke-AlloyedDecoratedCommand -Operation 'ConvertTo-Xml' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Utility\ConvertTo-Xml @args } }
function Copy-AlloyedItem { Invoke-AlloyedDecoratedCommand -Operation 'Copy-Item' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Copy-Item @args } }
function Expand-AlloyedArchive { Invoke-AlloyedDecoratedCommand -Operation 'Expand-Archive' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Archive\Expand-Archive @args } }
function Export-AlloyedPfxCertificate { Invoke-AlloyedDecoratedCommand -Operation 'Export-PfxCertificate' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Security\Export-PfxCertificate @args } }
function Get-AlloyedAcl { Invoke-AlloyedDecoratedCommand -Operation 'Get-Acl' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Security\Get-Acl @args } }
function Get-AlloyedAuthenticodeSignature { Invoke-AlloyedDecoratedCommand -Operation 'Get-AuthenticodeSignature' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Security\Get-AuthenticodeSignature @args } }
function Get-AlloyedChildItem { Invoke-AlloyedDecoratedCommand -Operation 'Get-ChildItem' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Get-ChildItem @args } }
function Get-AlloyedContent { Invoke-AlloyedDecoratedCommand -Operation 'Get-Content' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Get-Content @args } }
function Get-AlloyedCredential { Invoke-AlloyedDecoratedCommand -Operation 'Get-Credential' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Security\Get-Credential @args } }
function Get-AlloyedItem { Invoke-AlloyedDecoratedCommand -Operation 'Get-Item' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Get-Item @args } }
function Get-AlloyedLocation { Invoke-AlloyedDecoratedCommand -Operation 'Get-Location' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Get-Location @args } }
function Get-AlloyedPfxCertificate { Invoke-AlloyedDecoratedCommand -Operation 'Get-PfxCertificate' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Security\Get-PfxCertificate @args } }
function Get-AlloyedProcess { Invoke-AlloyedDecoratedCommand -Operation 'Get-Process' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Get-Process @args } }
function Get-AlloyedRandom { Invoke-AlloyedDecoratedCommand -Operation 'Get-Random' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Utility\Get-Random @args } }
function Get-AlloyedService { Invoke-AlloyedDecoratedCommand -Operation 'Get-Service' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Get-Service @args } }
function Group-AlloyedObject { Invoke-AlloyedDecoratedCommand -Operation 'Group-Object' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Utility\Group-Object @args } }
function Invoke-AlloyedCommand { Invoke-AlloyedDecoratedCommand -Operation 'Invoke-Command' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Core\Invoke-Command @args } }
function Join-AlloyedPath { Invoke-AlloyedDecoratedCommand -Operation 'Join-Path' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Join-Path @args } }
function Measure-AlloyedObject { Invoke-AlloyedDecoratedCommand -Operation 'Measure-Object' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Utility\Measure-Object @args } }
function Move-AlloyedItem { Invoke-AlloyedDecoratedCommand -Operation 'Move-Item' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Move-Item @args } }
function New-AlloyedItem { Invoke-AlloyedDecoratedCommand -Operation 'New-Item' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\New-Item @args } }
function New-AlloyedSelfSignedCertificate { Invoke-AlloyedDecoratedCommand -Operation 'New-SelfSignedCertificate' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Security\New-SelfSignedCertificate @args } }
function Pop-AlloyedLocation { Invoke-AlloyedDecoratedCommand -Operation 'Pop-Location' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Pop-Location @args } }
function Push-AlloyedLocation { Invoke-AlloyedDecoratedCommand -Operation 'Push-Location' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Push-Location @args } }
function Read-AlloyedHost { Invoke-AlloyedDecoratedCommand -Operation 'Read-Host' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Utility\Read-Host @args } }
function Remove-AlloyedItem { Invoke-AlloyedDecoratedCommand -Operation 'Remove-Item' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Remove-Item @args } }
function Resolve-AlloyedPath { Invoke-AlloyedDecoratedCommand -Operation 'Resolve-Path' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Resolve-Path @args } }
function Restart-AlloyedService { Invoke-AlloyedDecoratedCommand -Operation 'Restart-Service' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Restart-Service @args } }
function Select-AlloyedString { Invoke-AlloyedDecoratedCommand -Operation 'Select-String' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Utility\Select-String @args } }
function Set-AlloyedAcl { Invoke-AlloyedDecoratedCommand -Operation 'Set-Acl' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Security\Set-Acl @args } }
function Set-AlloyedAuthenticodeSignature { Invoke-AlloyedDecoratedCommand -Operation 'Set-AuthenticodeSignature' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Security\Set-AuthenticodeSignature @args } }
function Set-AlloyedContent { Invoke-AlloyedDecoratedCommand -Operation 'Set-Content' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Set-Content @args } }
function Set-AlloyedLocation { Invoke-AlloyedDecoratedCommand -Operation 'Set-Location' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Set-Location @args } }
function Sort-AlloyedObject { Invoke-AlloyedDecoratedCommand -Operation 'Sort-Object' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Utility\Sort-Object @args } }
function Split-AlloyedPath { Invoke-AlloyedDecoratedCommand -Operation 'Split-Path' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Split-Path @args } }
function Start-AlloyedProcess { Invoke-AlloyedDecoratedCommand -Operation 'Start-Process' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Start-Process @args } }
function Start-AlloyedService { Invoke-AlloyedDecoratedCommand -Operation 'Start-Service' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Start-Service @args } }
function Stop-AlloyedProcess { Invoke-AlloyedDecoratedCommand -Operation 'Stop-Process' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Stop-Process @args } }
function Stop-AlloyedService { Invoke-AlloyedDecoratedCommand -Operation 'Stop-Service' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Stop-Service @args } }
function Test-AlloyedConnection { Invoke-AlloyedDecoratedCommand -Operation 'Test-Connection' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Test-Connection @args } }
function Test-AlloyedPath { Invoke-AlloyedDecoratedCommand -Operation 'Test-Path' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Test-Path @args } }
function Wait-AlloyedProcess { Invoke-AlloyedDecoratedCommand -Operation 'Wait-Process' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Management\Wait-Process @args } }
function Write-AlloyedHost { Invoke-AlloyedDecoratedCommand -Operation 'Write-Host' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Utility\Write-Host @args } }
function Write-AlloyedProgress { Invoke-AlloyedDecoratedCommand -Operation 'Write-Progress' -Arguments $args -InputObjects @($input) -Action {Microsoft.PowerShell.Utility\Write-Progress @args } }
# </auto-generated:wrappers>

# ---------------------------------------------------------------------------
# Pipeline cmdlets
# ---------------------------------------------------------------------------

function New-AlloyedModuleTransform {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)] [string]$ScriptPath,
        [Parameter(Mandatory)] [string]$ModuleName,
        [Parameter()] [string]$OutputPath,
        [Parameter()] [switch]$Force,
        [Parameter()] [ValidateSet('Info','Warning','Error')] [string]$FailOnSeverity,
        [Parameter()] [switch]$FailOnWarnings,
        [Parameter()] [ValidateSet('Plain','Rich')] [string]$OutputMode
    )

    Initialize-AlloyedHostAssembly

    $basePath = (Get-Location).Path
    $configuration = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.PipelineBootstrap]::CreateRuntimeConfiguration($basePath, $null)

    if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        $defaultOutputPath = $configuration.Runtime.DefaultOutputPath
        if ([string]::IsNullOrWhiteSpace($defaultOutputPath)) {
            $defaultOutputPath = 'out'
        }

        $OutputPath = if ([System.IO.Path]::IsPathRooted($defaultOutputPath)) {
            $defaultOutputPath
        } else {
            Join-Path $basePath $defaultOutputPath
        }
    }

    if ($PSCmdlet.ShouldProcess($ModuleName, 'Transform script and build module')) {
        $prevMode = $script:ConsoleOutputModeOverride
        try {
            if (-not [string]::IsNullOrWhiteSpace($OutputMode)) {
                $script:ConsoleOutputModeOverride = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.ConsoleOutputMode]::$OutputMode
            }

            $pipeline = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.PipelineBootstrap]::CreateDefault($basePath, $null)
            $severity = Resolve-FailOnSeverity -FailOnSeverity $FailOnSeverity -FailOnWarnings:$FailOnWarnings
            $request = [Alloyed.DevOps.Multitool.Host.PowerShell.Models.PipelineRequest]::new($ScriptPath, $ModuleName, $OutputPath, $Force.IsPresent, $severity)
            $result = $pipeline.Execute($request)
            Write-AlloyedPipelineResultSummary -Result $result -Operation 'New-AlloyedModuleTransform'
            return $result
        } finally {
            $script:ConsoleOutputModeOverride = $prevMode
        }
    }
}

function Test-AlloyedTransform {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$ScriptPath,
        [Parameter()] [ValidateSet('Info','Warning','Error')] [string]$FailOnSeverity,
        [Parameter()] [switch]$FailOnWarnings,
        [Parameter()] [ValidateSet('Plain','Rich')] [string]$OutputMode
    )

    Initialize-AlloyedHostAssembly

    $prevMode = $script:ConsoleOutputModeOverride
    try {
        if (-not [string]::IsNullOrWhiteSpace($OutputMode)) {
            $script:ConsoleOutputModeOverride = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.ConsoleOutputMode]::$OutputMode
        }

        $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'alloyed-transform-test'
        $moduleName = 'AlloyedTransformValidation'

        $pipeline = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.PipelineBootstrap]::CreateDefault()
        $severity = Resolve-FailOnSeverity -FailOnSeverity $FailOnSeverity -FailOnWarnings:$FailOnWarnings
        $request = [Alloyed.DevOps.Multitool.Host.PowerShell.Models.PipelineRequest]::new($ScriptPath, $moduleName, $tempRoot, $true, $severity)
        $result = $pipeline.Execute($request)
        Write-AlloyedPipelineResultSummary -Result $result -Operation 'Test-AlloyedTransform'
    } finally {
        $script:ConsoleOutputModeOverride = $prevMode
    }

    [pscustomobject]@{
        Success = $result.Success
        CommandsFound = $result.CommandsFound
        CommandsReplaced = $result.CommandsReplaced
        MissingCommands = @($result.MissingCommands)
        Diagnostics = @($result.Diagnostics | ForEach-Object {
            [pscustomobject]@{
                Code = $_.Code
                Source = $_.Source
                Severity = $_.Severity.ToString()
                Message = $_.Message
                Line = $_.Line
                Column = $_.Column
            }
        })
        ErrorMessage = $result.ErrorMessage
    }
}

function Get-AlloyedCatalog {
    [CmdletBinding()]
    param()

    Initialize-AlloyedHostAssembly

    $catalog = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.PipelineBootstrap]::CreateCatalog()
    $mappings = $catalog.GetMappings()

    foreach ($item in $mappings.GetEnumerator() | Microsoft.PowerShell.Utility\Sort-Object Key) {
        [pscustomobject]@{
            Command = $item.Key
            Wrapper = $item.Value
        }
    }
}

function Get-AlloyedRuntimeConfiguration {
    [CmdletBinding()]
    param(
        [Parameter()] [string]$BasePath = (Get-Location).Path
    )

    Initialize-AlloyedHostAssembly

    $configuration = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.PipelineBootstrap]::CreateRuntimeConfiguration($BasePath, $null)

    [pscustomobject]@{
        Runtime = [pscustomobject]@{
            FailOnSeverity = if ($null -eq $configuration.Runtime.FailOnSeverity) { $null } else { $configuration.Runtime.FailOnSeverity.ToString() }
            DefaultOutputPath = $configuration.Runtime.DefaultOutputPath
        }
        Session = [pscustomobject]@{
            Enabled = $configuration.Session.Enabled
        }
        Decoration = [pscustomobject]@{
            EnableErrorHandling = $configuration.Decoration.EnableErrorHandling
            EnableObservability = $configuration.Decoration.EnableObservability
            EnableCorrelation = $configuration.Decoration.EnableCorrelation
            EnableTransparency = $configuration.Decoration.EnableTransparency
        }
        Mocking = [pscustomobject]@{
            Enabled = $configuration.Mocking.Enabled
            Mode = $configuration.Mocking.Mode.ToString()
        }
    }
}

function Enable-AlloyedSessionMode {
    [CmdletBinding()]
    param(
        [Parameter()] [switch]$Force
    )

    Initialize-AlloyedHostAssembly
    Initialize-AlloyedWrappersFromCatalog

    if ($script:SessionModeEnabled -and -not $Force.IsPresent) {
        return Get-AlloyedSessionModeStatus
    }

    $catalog = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.PipelineBootstrap]::CreateCatalog()
    $mappings = $catalog.GetMappings()
    $nativeMap = Get-AlloyedNativeCommandMap
    $protectedCommands = @(
        'Write-Host',
        'Write-Progress',
        'Read-Host',
        'Get-Command',
        'Set-Alias',
        'Remove-Item',
        'Set-Item'
    )

    $applied = New-Object System.Collections.Generic.List[string]
    $skipped = New-Object System.Collections.Generic.List[string]
    $script:SessionModeAliasBackup = @{}
    $script:SessionModeCommandBackup = @{}

    foreach ($entry in ($mappings.GetEnumerator() | Sort-Object Key)) {
        $name = $entry.Key
        if ($protectedCommands -contains $name) {
            $skipped.Add($name)
            continue
        }

        $sourceCommand = Get-Command -Name $name -ErrorAction SilentlyContinue
        if (-not $sourceCommand) {
            $skipped.Add($name)
            continue
        }

        if ($sourceCommand.CommandType -eq 'Function' -and $sourceCommand.Source -eq 'Alloyed.DevOps.Multitool') {
            $skipped.Add($name)
            continue
        }

        $nativeCommand = $null
        if ($nativeMap.ContainsKey($name)) {
            $nativeCommand = [string]$nativeMap[$name]
        } elseif ($sourceCommand.Source) {
            $nativeCommand = "{0}\{1}" -f $sourceCommand.Source, $sourceCommand.Name
        } else {
            $nativeCommand = [string]$sourceCommand.Name
        }

        try {
            $existingFunction = Get-Command -Name $name -CommandType Function -ErrorAction SilentlyContinue
            $existingAlias = Get-Alias -Name $name -ErrorAction SilentlyContinue

            $script:SessionModeCommandBackup[$name] = [pscustomobject]@{
                AliasDefinition = if ($existingAlias) { $existingAlias.Definition } else { $null }
                AliasOptions = if ($existingAlias) { $existingAlias.Options } else { $null }
                FunctionDefinition = if ($existingFunction) { (Get-Content -LiteralPath ("function:{0}" -f $name) -ErrorAction SilentlyContinue) } else { $null }
            }

            if ($existingAlias) {
                Remove-Item -LiteralPath ("Alias:{0}" -f $name) -Force -ErrorAction SilentlyContinue
            }

            $functionBody = if ($name -eq 'Clear-Host') {
                "function global:$name { Invoke-AlloyedDecoratedCommand -Operation '$name' -Parameters @{} -Action { $nativeCommand } }"
            } else {
                "function global:$name { Invoke-AlloyedDecoratedCommand -Operation '$name' -Arguments `$args -InputObjects @(`$input) -Action { $nativeCommand @args } }"
            }

            $null = Invoke-Expression $functionBody

            if ($existingAlias) {
                $script:SessionModeAliasBackup[$name] = $existingAlias.Definition
            } else {
                $script:SessionModeAliasBackup[$name] = $null
            }

            $applied.Add($name)
        } catch {
            if ($script:SessionModeCommandBackup.ContainsKey($name)) {
                $script:SessionModeCommandBackup.Remove($name)
            }
            $skipped.Add($name)
            continue
        }
    }

    $script:SessionModeAliases = @($applied.ToArray())
    $script:SessionModeEnabled = $true

    [pscustomobject]@{
        Enabled = $true
        AppliedAliases = @($applied.ToArray())
        SkippedCommands = @($skipped.ToArray())
    }
}

function Disable-AlloyedSessionMode {
    [CmdletBinding()]
    param()

    if (-not $script:SessionModeEnabled) {
        return Get-AlloyedSessionModeStatus
    }

    foreach ($name in @($script:SessionModeAliases)) {
        Remove-Item -LiteralPath ("Function:{0}" -f $name) -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath ("Alias:{0}" -f $name) -Force -ErrorAction SilentlyContinue

        $prior = $script:SessionModeCommandBackup[$name]
        if ($null -eq $prior) {
            continue
        }

        if (-not [string]::IsNullOrWhiteSpace($prior.FunctionDefinition)) {
            Set-Item -LiteralPath ("Function:global:{0}" -f $name) -Value $prior.FunctionDefinition -Force
        }

        if (-not [string]::IsNullOrWhiteSpace($prior.AliasDefinition)) {
            try {
                Remove-Item -LiteralPath ("Alias:{0}" -f $name) -Force -ErrorAction SilentlyContinue
                if ($null -ne $prior.AliasOptions) {
                    Set-Alias -Name $name -Value $prior.AliasDefinition -Scope Global -Option $prior.AliasOptions -Force
                } else {
                    Set-Alias -Name $name -Value $prior.AliasDefinition -Scope Global -Force
                }
            } catch {
                Write-Verbose ("Failed to restore alias '{0}': {1}" -f $name, $_.Exception.Message)
            }
        }
    }

    $script:SessionModeEnabled = $false
    $script:SessionModeAliases = @()
    $script:SessionModeAliasBackup = @{}
    $script:SessionModeCommandBackup = @{}

    Get-AlloyedSessionModeStatus
}

function Get-AlloyedSessionModeStatus {
    [CmdletBinding()]
    param()

    [pscustomobject]@{
        Enabled = $script:SessionModeEnabled
        ActiveAliasCount = @($script:SessionModeAliases).Count
        ActiveAliases = @($script:SessionModeAliases)
    }
}

function Enable-AlloyedTransparencyMode {
    [CmdletBinding()]
    param(
        [Parameter()] [switch]$SkipSessionMode,
        [Parameter()] [ValidateSet('Plain','Rich')] [string]$OutputMode,
        [Parameter()] [switch]$Quiet,
        [Parameter()] [ValidateSet('minimal','standard','debug')] [string]$Profile = 'standard'
    )

    if (-not $SkipSessionMode.IsPresent) {
        $null = Enable-AlloyedSessionMode -Force
    }

    if (-not [string]::IsNullOrWhiteSpace($OutputMode)) {
        $script:ConsoleOutputModeOverride = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.ConsoleOutputMode]::$OutputMode
    }

    $script:TransparencyModeOverride = $true
    if ($Quiet.IsPresent) {
        [System.Environment]::SetEnvironmentVariable('ALLOYED_TRANSPARENCY_VERBOSE', 'false', 'Process')
    } else {
        [System.Environment]::SetEnvironmentVariable('ALLOYED_TRANSPARENCY_VERBOSE', 'true', 'Process')
    }
    [System.Environment]::SetEnvironmentVariable('ALLOYED_TRANSPARENCY_PROFILE', $Profile, 'Process')
    Get-AlloyedTransparencyModeStatus
}

function Disable-AlloyedTransparencyMode {
    [CmdletBinding()]
    param()

    $script:TransparencyModeOverride = $false
    $script:ConsoleOutputModeOverride = $null
    [System.Environment]::SetEnvironmentVariable('ALLOYED_TRANSPARENCY_VERBOSE', $null, 'Process')
    [System.Environment]::SetEnvironmentVariable('ALLOYED_TRANSPARENCY_PROFILE', $null, 'Process')
    Get-AlloyedTransparencyModeStatus
}

function Invoke-AlloyedScript {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$ScriptPath,
        [Parameter()] [object[]]$ArgumentList = @(),
        [Parameter()] [switch]$SkipSessionMode,
        [Parameter()] [ValidateSet('Plain','Rich')] [string]$OutputMode
    )

    if (-not (Test-Path -LiteralPath $ScriptPath)) {
        throw "Script not found: '$ScriptPath'"
    }

    $resolvedPath = (Resolve-Path -LiteralPath $ScriptPath).Path
    $shouldDisableAfterRun = $false

    if (-not (Resolve-AlloyedTransparencyEnabled)) {
        $null = Enable-AlloyedTransparencyMode -SkipSessionMode:$SkipSessionMode -OutputMode $OutputMode
        $shouldDisableAfterRun = $true
    } elseif (-not $SkipSessionMode.IsPresent -and -not $script:SessionModeEnabled) {
        $null = Enable-AlloyedSessionMode -Force
    }

    try {
        & $resolvedPath @ArgumentList
    } finally {
        if ($shouldDisableAfterRun) {
            $null = Disable-AlloyedTransparencyMode
            if (-not $SkipSessionMode.IsPresent) {
                $null = Disable-AlloyedSessionMode
            }
        }
    }
}

function Get-AlloyedTransparencyModeStatus {
    [CmdletBinding()]
    param()

    [pscustomobject]@{
        Enabled = Resolve-AlloyedTransparencyEnabled
        Override = if ($null -eq $script:TransparencyModeOverride) { '<config>' } else { [bool]$script:TransparencyModeOverride }
        SessionModeEnabled = [bool]$script:SessionModeEnabled
        OutputMode = (Resolve-AlloyedConsoleOutputMode).ToString()
        Verbose = [System.Environment]::GetEnvironmentVariable('ALLOYED_TRANSPARENCY_VERBOSE')
        Profile = [System.Environment]::GetEnvironmentVariable('ALLOYED_TRANSPARENCY_PROFILE')
    }
}
