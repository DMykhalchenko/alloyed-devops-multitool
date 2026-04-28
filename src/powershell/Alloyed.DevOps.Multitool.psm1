$script:AssemblyLoaded = $false
$script:SessionModeEnabled = $false
$script:SessionModeAliases = @()
$script:SessionModeAliasBackup = @{}
$script:DecorationPipeline = $null
$script:TransparencyModeOverride = $null
$script:ConsoleOutputModeOverride = $null

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

function Get-AlloyedConsoleReporter {
    Initialize-AlloyedHostAssembly

    $isInteractive = -not [System.Console]::IsOutputRedirected
    $mode = Resolve-AlloyedConsoleOutputMode
    return [Alloyed.DevOps.Multitool.Host.PowerShell.Services.ConsoleReporterFactory]::Create($mode, $isInteractive, $null)
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

        $reporter.WriteMessage($level, "[{0}] {1}" -f $diagnostic.Code, $diagnostic.Message)
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
        if ($InputObjects.Count -gt 0) {
            return $InputObjects | & $Action @Arguments
        }

        return & $Action @Arguments
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

    if ($script:SessionModeEnabled -and -not $Force.IsPresent) {
        return Get-AlloyedSessionModeStatus
    }

    $catalog = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.PipelineBootstrap]::CreateCatalog()
    $mappings = $catalog.GetMappings()

    $applied = New-Object System.Collections.Generic.List[string]
    $skipped = New-Object System.Collections.Generic.List[string]
    $script:SessionModeAliasBackup = @{}

    foreach ($entry in ($mappings.GetEnumerator() | Sort-Object Key)) {
        $name = $entry.Key
        $wrapper = $entry.Value

        $wrapperCommand = Get-Command -Name $wrapper -ErrorAction SilentlyContinue
        $sourceCommand = Get-Command -Name $name -ErrorAction SilentlyContinue
        if (-not $wrapperCommand -or -not $sourceCommand) {
            $skipped.Add($name)
            continue
        }

        $existingAlias = Get-Alias -Name $name -ErrorAction SilentlyContinue
        $previousDefinition = if ($existingAlias) { $existingAlias.Definition } else { $null }
        if (
            $existingAlias -and
            (($existingAlias.Options -band [System.Management.Automation.ScopedItemOptions]::AllScope) -ne [System.Management.Automation.ScopedItemOptions]::None)
        ) {
            $skipped.Add($name)
            continue
        }

        try {
            if ($existingAlias) {
                Set-Alias -Name $name -Value $wrapper -Scope Global -Option $existingAlias.Options -Force
            } else {
                Set-Alias -Name $name -Value $wrapper -Scope Global -Force
            }
        } catch {
            $skipped.Add($name)
            continue
        }

        $script:SessionModeAliasBackup[$name] = $previousDefinition
        $applied.Add($name)
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
        $prior = $script:SessionModeAliasBackup[$name]
        if ($null -eq $prior) {
            Microsoft.PowerShell.Management\Remove-Item -LiteralPath "Alias:$name" -Force -ErrorAction SilentlyContinue
            continue
        }

        Set-Alias -Name $name -Value $prior -Scope Global -Force
    }

    $script:SessionModeEnabled = $false
    $script:SessionModeAliases = @()
    $script:SessionModeAliasBackup = @{}

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
        [Parameter()] [ValidateSet('Plain','Rich')] [string]$OutputMode
    )

    if (-not $SkipSessionMode.IsPresent) {
        $null = Enable-AlloyedSessionMode -Force
    }

    if (-not [string]::IsNullOrWhiteSpace($OutputMode)) {
        $script:ConsoleOutputModeOverride = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.ConsoleOutputMode]::$OutputMode
    }

    $script:TransparencyModeOverride = $true
    Get-AlloyedTransparencyModeStatus
}

function Disable-AlloyedTransparencyMode {
    [CmdletBinding()]
    param()

    $script:TransparencyModeOverride = $false
    $script:ConsoleOutputModeOverride = $null
    Get-AlloyedTransparencyModeStatus
}

function Get-AlloyedTransparencyModeStatus {
    [CmdletBinding()]
    param()

    [pscustomobject]@{
        Enabled = Resolve-AlloyedTransparencyEnabled
        Override = if ($null -eq $script:TransparencyModeOverride) { '<config>' } else { [bool]$script:TransparencyModeOverride }
        SessionModeEnabled = [bool]$script:SessionModeEnabled
        OutputMode = (Resolve-AlloyedConsoleOutputMode).ToString()
    }
}
