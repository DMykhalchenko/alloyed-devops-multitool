function New-AlloyedModuleTransform {
<#
.SYNOPSIS
    Transforms a PowerShell script into an Alloyed-wrapped module and writes it to disk.
.DESCRIPTION
    Analyzes the script at ScriptPath using the AST-based script analyzer, resolves every
    detected command against the wrapper catalog, rewrites the source text, and emits a
    self-contained module under OutputPath\ModuleName. When OutputPath is omitted the value
    is taken from the runtime configuration (DefaultOutputPath, default "out").
.PARAMETER ScriptPath
    Absolute or relative path to the source PowerShell script to transform.
.PARAMETER ModuleName
    Name of the output module directory and manifest created under OutputPath.
.PARAMETER OutputPath
    Directory where the module folder is created. Defaults to the DefaultOutputPath value
    in the runtime configuration, or "out" when that value is blank.
.PARAMETER Force
    Overwrites an existing output module directory without prompting.
.PARAMETER FailOnSeverity
    Stops the pipeline when any AST diagnostic meets or exceeds this severity level.
    Accepted values: Info, Warning, Error.
.PARAMETER FailOnWarnings
    Shorthand for -FailOnSeverity Warning. Ignored when FailOnSeverity is also specified.
.PARAMETER OutputMode
    Selects the console rendering back-end. Plain emits plain text; Rich uses Spectre.Console
    colour output. Defaults to the session override when set, otherwise Plain.
.OUTPUTS
    Alloyed.DevOps.Multitool.Host.PowerShell.Models.PipelineResult
.EXAMPLE
    PS> New-AlloyedModuleTransform -ScriptPath ./scripts/deploy.ps1 -ModuleName DeployModule
.EXAMPLE
    PS> New-AlloyedModuleTransform -ScriptPath ./scripts/deploy.ps1 -ModuleName DeployModule -Force -FailOnSeverity Error -WhatIf
#>
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
<#
.SYNOPSIS
    Performs a dry-run transformation of a PowerShell script without persisting the output module.
.DESCRIPTION
    Runs the full transformation pipeline against ScriptPath and writes the result into a
    temporary directory that is not intended for reuse. Useful for validating catalog coverage
    and detecting AST parse diagnostics before committing to a real transform.
.PARAMETER ScriptPath
    Absolute or relative path to the source PowerShell script to analyze.
.PARAMETER FailOnSeverity
    Stops the pipeline when any AST diagnostic meets or exceeds this severity level.
    Accepted values: Info, Warning, Error.
.PARAMETER FailOnWarnings
    Shorthand for -FailOnSeverity Warning. Ignored when FailOnSeverity is also specified.
.PARAMETER OutputMode
    Selects the console rendering back-end (Plain or Rich) for the result summary.
.OUTPUTS
    PSCustomObject with properties: Success, CommandsFound, CommandsReplaced, MissingCommands,
    Diagnostics (Code, Source, Severity, Message, Line, Column), ErrorMessage.
.EXAMPLE
    PS> Test-AlloyedTransform -ScriptPath ./scripts/deploy.ps1
.EXAMPLE
    PS> Test-AlloyedTransform -ScriptPath ./scripts/deploy.ps1 -FailOnWarnings
#>
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
<#
.SYNOPSIS
    Returns all command-to-wrapper mappings registered in the active wrapper catalog.
.DESCRIPTION
    Loads the wrapper catalog (from the configured source path or the embedded default) and
    emits one object per entry, sorted alphabetically by the original command name.
.OUTPUTS
    PSCustomObject with properties: Command (original command name), Wrapper (replacement name).
.EXAMPLE
    PS> Get-AlloyedCatalog
.EXAMPLE
    PS> Get-AlloyedCatalog | Where-Object Wrapper -like 'Get-Alloyed*'
#>
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
<#
.SYNOPSIS
    Loads and returns the merged Alloyed runtime configuration.
.DESCRIPTION
    Reads configuration from four sources in ascending precedence order: hard-coded defaults,
    config/appsettings.json, config/appsettings.yml (or .yaml), and environment variables
    prefixed with ALLOYED__ or the legacy TAF__ prefix. All sources are relative to BasePath.
.PARAMETER BasePath
    Base directory from which config files are resolved. Defaults to the current working directory.
.OUTPUTS
    PSCustomObject with sections: Runtime (FailOnSeverity, DefaultOutputPath), Session (Enabled),
    Decoration (EnableErrorHandling, EnableObservability, EnableCorrelation, EnableTransparency,
    TransparencyProfile), Mocking (Enabled, Mode).
.EXAMPLE
    PS> Get-AlloyedRuntimeConfiguration
.EXAMPLE
    PS> (Get-AlloyedRuntimeConfiguration).Decoration
#>
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
            TransparencyProfile = $configuration.Decoration.TransparencyProfile.ToString().ToLowerInvariant()
        }
        Mocking = [pscustomobject]@{
            Enabled = $configuration.Mocking.Enabled
            Mode = $configuration.Mocking.Mode.ToString()
        }
    }
}

function Invoke-AlloyedScript {
<#
.SYNOPSIS
    Executes a PowerShell script with Alloyed transparency and session interception active.
.DESCRIPTION
    Enables transparency mode (and optionally session interception) for the duration of the
    script run, then restores the previous state. If transparency is already enabled by the
    caller, only session mode is activated when needed. Both transparency and session mode are
    disabled after the script completes when they were not enabled before the call.
.PARAMETER ScriptPath
    Absolute or relative path to the PowerShell script to execute.
.PARAMETER ArgumentList
    Arguments forwarded to the script via splatting. Defaults to an empty array.
.PARAMETER SkipSessionMode
    When specified, command-interception (session mode) is not enabled; only transparency
    logging is activated for the run.
.PARAMETER OutputMode
    Selects the console rendering back-end (Plain or Rich) while the script runs.
.EXAMPLE
    PS> Invoke-AlloyedScript -ScriptPath ./scripts/deploy.ps1
.EXAMPLE
    PS> Invoke-AlloyedScript -ScriptPath ./scripts/deploy.ps1 -ArgumentList @('-Env', 'prod') -SkipSessionMode
#>
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
