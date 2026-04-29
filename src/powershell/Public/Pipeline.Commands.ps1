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
            TransparencyProfile = $configuration.Decoration.TransparencyProfile.ToString().ToLowerInvariant()
        }
        Mocking = [pscustomobject]@{
            Enabled = $configuration.Mocking.Enabled
            Mode = $configuration.Mocking.Mode.ToString()
        }
    }
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
