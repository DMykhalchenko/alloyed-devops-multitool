$script:AssemblyLoaded = $false
$script:SessionModeEnabled = $false
$script:SessionModeAliases = @()
$script:SessionModeAliasBackup = @{}
$script:DecorationPipeline = $null
$script:TransparencyModeOverride = $null

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

function Invoke-AlloyedDecoratedCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$Operation,
        [Parameter(Mandatory)] [hashtable]$Parameters,
        [Parameter(Mandatory)] [scriptblock]$Action
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
    $invoke = [System.Func[object]] { & $Action }
    return $script:DecorationPipeline.Execute[object]($context, $invoke)
}

# ---------------------------------------------------------------------------
# Provider.FileSystem wrappers
# ---------------------------------------------------------------------------

function Get-AlloyedChildItem { Invoke-AlloyedDecoratedCommand -Operation 'Get-ChildItem' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Get-ChildItem @PSBoundParameters } }
function Get-AlloyedItem       { Invoke-AlloyedDecoratedCommand -Operation 'Get-Item' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Get-Item @PSBoundParameters } }
function Test-AlloyedPath      { Invoke-AlloyedDecoratedCommand -Operation 'Test-Path' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Test-Path @PSBoundParameters } }
function Copy-AlloyedItem      { Invoke-AlloyedDecoratedCommand -Operation 'Copy-Item' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Copy-Item @PSBoundParameters } }
function Move-AlloyedItem      { Invoke-AlloyedDecoratedCommand -Operation 'Move-Item' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Move-Item @PSBoundParameters } }
function Remove-AlloyedItem    { Invoke-AlloyedDecoratedCommand -Operation 'Remove-Item' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Remove-Item @PSBoundParameters } }
function New-AlloyedItem       { Invoke-AlloyedDecoratedCommand -Operation 'New-Item' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\New-Item @PSBoundParameters } }
function Get-AlloyedContent    { Invoke-AlloyedDecoratedCommand -Operation 'Get-Content' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Get-Content @PSBoundParameters } }
function Set-AlloyedContent    { Invoke-AlloyedDecoratedCommand -Operation 'Set-Content' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Set-Content @PSBoundParameters } }

# ---------------------------------------------------------------------------
# System.Utility wrappers
# ---------------------------------------------------------------------------

function Select-AlloyedString    { Invoke-AlloyedDecoratedCommand -Operation 'Select-String' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Utility\Select-String @PSBoundParameters } }
function ConvertTo-AlloyedJson   { Invoke-AlloyedDecoratedCommand -Operation 'ConvertTo-Json' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Utility\ConvertTo-Json @PSBoundParameters } }
function ConvertFrom-AlloyedJson { Invoke-AlloyedDecoratedCommand -Operation 'ConvertFrom-Json' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Utility\ConvertFrom-Json @PSBoundParameters } }
function ConvertTo-AlloyedXml    { Invoke-AlloyedDecoratedCommand -Operation 'ConvertTo-Xml' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Utility\ConvertTo-Xml @PSBoundParameters } }
function Get-AlloyedRandom       { Invoke-AlloyedDecoratedCommand -Operation 'Get-Random' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Utility\Get-Random @PSBoundParameters } }
function Measure-AlloyedObject   { Invoke-AlloyedDecoratedCommand -Operation 'Measure-Object' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Utility\Measure-Object @PSBoundParameters } }
function Sort-AlloyedObject      { Invoke-AlloyedDecoratedCommand -Operation 'Sort-Object' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Utility\Sort-Object @PSBoundParameters } }
function Group-AlloyedObject     { Invoke-AlloyedDecoratedCommand -Operation 'Group-Object' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Utility\Group-Object @PSBoundParameters } }

# ---------------------------------------------------------------------------
# System.Diagnostics wrappers
# ---------------------------------------------------------------------------

function Get-AlloyedProcess      { Invoke-AlloyedDecoratedCommand -Operation 'Get-Process' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Get-Process @PSBoundParameters } }
function Start-AlloyedProcess    { Invoke-AlloyedDecoratedCommand -Operation 'Start-Process' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Start-Process @PSBoundParameters } }
function Stop-AlloyedProcess     { Invoke-AlloyedDecoratedCommand -Operation 'Stop-Process' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Stop-Process @PSBoundParameters } }
function Wait-AlloyedProcess     { Invoke-AlloyedDecoratedCommand -Operation 'Wait-Process' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Wait-Process @PSBoundParameters } }
function Test-AlloyedConnection  { Invoke-AlloyedDecoratedCommand -Operation 'Test-Connection' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Test-Connection @PSBoundParameters } }
function Invoke-AlloyedCommand   { Invoke-AlloyedDecoratedCommand -Operation 'Invoke-Command' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Core\Invoke-Command @PSBoundParameters } }

# ---------------------------------------------------------------------------
# System.Archive wrappers
# ---------------------------------------------------------------------------

function Compress-AlloyedArchive { Invoke-AlloyedDecoratedCommand -Operation 'Compress-Archive' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Archive\Compress-Archive @PSBoundParameters } }
function Expand-AlloyedArchive   { Invoke-AlloyedDecoratedCommand -Operation 'Expand-Archive' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Archive\Expand-Archive @PSBoundParameters } }

# ---------------------------------------------------------------------------
# System.Management wrappers
# ---------------------------------------------------------------------------

function Get-AlloyedService     { Invoke-AlloyedDecoratedCommand -Operation 'Get-Service' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Get-Service @PSBoundParameters } }
function Start-AlloyedService   { Invoke-AlloyedDecoratedCommand -Operation 'Start-Service' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Start-Service @PSBoundParameters } }
function Stop-AlloyedService    { Invoke-AlloyedDecoratedCommand -Operation 'Stop-Service' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Stop-Service @PSBoundParameters } }
function Restart-AlloyedService { Invoke-AlloyedDecoratedCommand -Operation 'Restart-Service' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Management\Restart-Service @PSBoundParameters } }

# ---------------------------------------------------------------------------
# System.Security wrappers
# ---------------------------------------------------------------------------

function Get-AlloyedAcl                   { Invoke-AlloyedDecoratedCommand -Operation 'Get-Acl' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Security\Get-Acl @PSBoundParameters } }
function Set-AlloyedAcl                   { Invoke-AlloyedDecoratedCommand -Operation 'Set-Acl' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Security\Set-Acl @PSBoundParameters } }
function Get-AlloyedCredential            { Invoke-AlloyedDecoratedCommand -Operation 'Get-Credential' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Security\Get-Credential @PSBoundParameters } }
function ConvertTo-AlloyedSecureString    { Invoke-AlloyedDecoratedCommand -Operation 'ConvertTo-SecureString' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Security\ConvertTo-SecureString @PSBoundParameters } }
function ConvertFrom-AlloyedSecureString  { Invoke-AlloyedDecoratedCommand -Operation 'ConvertFrom-SecureString' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Security\ConvertFrom-SecureString @PSBoundParameters } }
function Get-AlloyedAuthenticodeSignature { Invoke-AlloyedDecoratedCommand -Operation 'Get-AuthenticodeSignature' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Security\Get-AuthenticodeSignature @PSBoundParameters } }
function Set-AlloyedAuthenticodeSignature { Invoke-AlloyedDecoratedCommand -Operation 'Set-AuthenticodeSignature' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Security\Set-AuthenticodeSignature @PSBoundParameters } }
function New-AlloyedSelfSignedCertificate { Invoke-AlloyedDecoratedCommand -Operation 'New-SelfSignedCertificate' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Security\New-SelfSignedCertificate @PSBoundParameters } }
function Get-AlloyedPfxCertificate        { Invoke-AlloyedDecoratedCommand -Operation 'Get-PfxCertificate' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Security\Get-PfxCertificate @PSBoundParameters } }
function Export-AlloyedPfxCertificate     { Invoke-AlloyedDecoratedCommand -Operation 'Export-PfxCertificate' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Security\Export-PfxCertificate @PSBoundParameters } }

# ---------------------------------------------------------------------------
# System.Host wrappers
# ---------------------------------------------------------------------------

function Write-AlloyedHost     { Invoke-AlloyedDecoratedCommand -Operation 'Write-Host' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Utility\Write-Host @PSBoundParameters } }
function Read-AlloyedHost      { Invoke-AlloyedDecoratedCommand -Operation 'Read-Host' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Utility\Read-Host @PSBoundParameters } }
function Write-AlloyedProgress { Invoke-AlloyedDecoratedCommand -Operation 'Write-Progress' -Parameters $PSBoundParameters -Action { Microsoft.PowerShell.Utility\Write-Progress @PSBoundParameters } }
function Clear-AlloyedHost     { Invoke-AlloyedDecoratedCommand -Operation 'Clear-Host' -Parameters @{} -Action { Microsoft.PowerShell.Core\Clear-Host } }

# ---------------------------------------------------------------------------
# Pipeline cmdlets
# ---------------------------------------------------------------------------

function New-AlloyedModuleTransform {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)] [string]$ScriptPath,
        [Parameter(Mandatory)] [string]$ModuleName,
        [Parameter()] [string]$OutputPath = (Join-Path (Get-Location) 'out'),
        [Parameter()] [switch]$Force,
        [Parameter()] [ValidateSet('Info','Warning','Error')] [string]$FailOnSeverity,
        [Parameter()] [switch]$FailOnWarnings
    )

    Initialize-AlloyedHostAssembly

    if ($PSCmdlet.ShouldProcess($ModuleName, 'Transform script and build module')) {
        $pipeline = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.PipelineBootstrap]::CreateDefault()
        $severity = Resolve-FailOnSeverity -FailOnSeverity $FailOnSeverity -FailOnWarnings:$FailOnWarnings
        $request = [Alloyed.DevOps.Multitool.Host.PowerShell.Models.PipelineRequest]::new($ScriptPath, $ModuleName, $OutputPath, $Force.IsPresent, $severity)
        $result = $pipeline.Execute($request)
        return $result
    }
}

function Test-AlloyedTransform {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$ScriptPath,
        [Parameter()] [ValidateSet('Info','Warning','Error')] [string]$FailOnSeverity,
        [Parameter()] [switch]$FailOnWarnings
    )

    Initialize-AlloyedHostAssembly

    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'alloyed-transform-test'
    $moduleName = 'AlloyedTransformValidation'

    $pipeline = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.PipelineBootstrap]::CreateDefault()
    $severity = Resolve-FailOnSeverity -FailOnSeverity $FailOnSeverity -FailOnWarnings:$FailOnWarnings
    $request = [Alloyed.DevOps.Multitool.Host.PowerShell.Models.PipelineRequest]::new($ScriptPath, $moduleName, $tempRoot, $true, $severity)
    $result = $pipeline.Execute($request)

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

    foreach ($item in $mappings.GetEnumerator() | Sort-Object Key) {
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
    param()

    $script:TransparencyModeOverride = $true
    Get-AlloyedTransparencyModeStatus
}

function Disable-AlloyedTransparencyMode {
    [CmdletBinding()]
    param()

    $script:TransparencyModeOverride = $false
    Get-AlloyedTransparencyModeStatus
}

function Get-AlloyedTransparencyModeStatus {
    [CmdletBinding()]
    param()

    [pscustomobject]@{
        Enabled = Resolve-AlloyedTransparencyEnabled
        Override = if ($null -eq $script:TransparencyModeOverride) { '<config>' } else { [bool]$script:TransparencyModeOverride }
    }
}
