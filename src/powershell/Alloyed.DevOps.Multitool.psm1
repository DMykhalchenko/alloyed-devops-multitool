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
        [Parameter(Mandatory)] [scriptblock]$Action,
        [hashtable]$Parameters = @{},
        [object[]]$Arguments = @()
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
    $invoke = [System.Func[object]] { & $Action @Arguments }
    return $script:DecorationPipeline.Execute[object]($context, $invoke)
}

# ---------------------------------------------------------------------------
# Provider.FileSystem wrappers
# ---------------------------------------------------------------------------

function Get-AlloyedChildItem { Invoke-AlloyedDecoratedCommand -Operation 'Get-ChildItem' -Arguments $args -Action {Microsoft.PowerShell.Management\Get-ChildItem @args } }
function Get-AlloyedItem       { Invoke-AlloyedDecoratedCommand -Operation 'Get-Item' -Arguments $args -Action {Microsoft.PowerShell.Management\Get-Item @args } }
function Test-AlloyedPath      { Invoke-AlloyedDecoratedCommand -Operation 'Test-Path' -Arguments $args -Action {Microsoft.PowerShell.Management\Test-Path @args } }
function Copy-AlloyedItem      { Invoke-AlloyedDecoratedCommand -Operation 'Copy-Item' -Arguments $args -Action {Microsoft.PowerShell.Management\Copy-Item @args } }
function Move-AlloyedItem      { Invoke-AlloyedDecoratedCommand -Operation 'Move-Item' -Arguments $args -Action {Microsoft.PowerShell.Management\Move-Item @args } }
function Remove-AlloyedItem    { Invoke-AlloyedDecoratedCommand -Operation 'Remove-Item' -Arguments $args -Action {Microsoft.PowerShell.Management\Remove-Item @args } }
function New-AlloyedItem       { Invoke-AlloyedDecoratedCommand -Operation 'New-Item' -Arguments $args -Action {Microsoft.PowerShell.Management\New-Item @args } }
function Get-AlloyedContent    { Invoke-AlloyedDecoratedCommand -Operation 'Get-Content' -Arguments $args -Action {Microsoft.PowerShell.Management\Get-Content @args } }
function Set-AlloyedContent    { Invoke-AlloyedDecoratedCommand -Operation 'Set-Content' -Arguments $args -Action {Microsoft.PowerShell.Management\Set-Content @args } }
function Get-AlloyedLocation   { Invoke-AlloyedDecoratedCommand -Operation 'Get-Location' -Arguments $args -Action {Microsoft.PowerShell.Management\Get-Location @args } }
function Set-AlloyedLocation   { Invoke-AlloyedDecoratedCommand -Operation 'Set-Location' -Arguments $args -Action {Microsoft.PowerShell.Management\Set-Location @args } }
function Push-AlloyedLocation  { Invoke-AlloyedDecoratedCommand -Operation 'Push-Location' -Arguments $args -Action {Microsoft.PowerShell.Management\Push-Location @args } }
function Pop-AlloyedLocation   { Invoke-AlloyedDecoratedCommand -Operation 'Pop-Location' -Arguments $args -Action {Microsoft.PowerShell.Management\Pop-Location @args } }
function Join-AlloyedPath      { Invoke-AlloyedDecoratedCommand -Operation 'Join-Path' -Arguments $args -Action {Microsoft.PowerShell.Management\Join-Path @args } }
function Split-AlloyedPath     { Invoke-AlloyedDecoratedCommand -Operation 'Split-Path' -Arguments $args -Action {Microsoft.PowerShell.Management\Split-Path @args } }
function Resolve-AlloyedPath   { Invoke-AlloyedDecoratedCommand -Operation 'Resolve-Path' -Arguments $args -Action {Microsoft.PowerShell.Management\Resolve-Path @args } }

# ---------------------------------------------------------------------------
# System.Utility wrappers
# ---------------------------------------------------------------------------

function Select-AlloyedString    { Invoke-AlloyedDecoratedCommand -Operation 'Select-String' -Arguments $args -Action {Microsoft.PowerShell.Utility\Select-String @args } }
function ConvertTo-AlloyedJson   { Invoke-AlloyedDecoratedCommand -Operation 'ConvertTo-Json' -Arguments $args -Action {Microsoft.PowerShell.Utility\ConvertTo-Json @args } }
function ConvertFrom-AlloyedJson { Invoke-AlloyedDecoratedCommand -Operation 'ConvertFrom-Json' -Arguments $args -Action {Microsoft.PowerShell.Utility\ConvertFrom-Json @args } }
function ConvertTo-AlloyedXml    { Invoke-AlloyedDecoratedCommand -Operation 'ConvertTo-Xml' -Arguments $args -Action {Microsoft.PowerShell.Utility\ConvertTo-Xml @args } }
function Get-AlloyedRandom       { Invoke-AlloyedDecoratedCommand -Operation 'Get-Random' -Arguments $args -Action {Microsoft.PowerShell.Utility\Get-Random @args } }
function Measure-AlloyedObject   { Invoke-AlloyedDecoratedCommand -Operation 'Measure-Object' -Arguments $args -Action {Microsoft.PowerShell.Utility\Measure-Object @args } }
function Sort-AlloyedObject      { Invoke-AlloyedDecoratedCommand -Operation 'Sort-Object' -Arguments $args -Action {Microsoft.PowerShell.Utility\Sort-Object @args } }
function Group-AlloyedObject     { Invoke-AlloyedDecoratedCommand -Operation 'Group-Object' -Arguments $args -Action {Microsoft.PowerShell.Utility\Group-Object @args } }

# ---------------------------------------------------------------------------
# System.Diagnostics wrappers
# ---------------------------------------------------------------------------

function Get-AlloyedProcess      { Invoke-AlloyedDecoratedCommand -Operation 'Get-Process' -Arguments $args -Action {Microsoft.PowerShell.Management\Get-Process @args } }
function Start-AlloyedProcess    { Invoke-AlloyedDecoratedCommand -Operation 'Start-Process' -Arguments $args -Action {Microsoft.PowerShell.Management\Start-Process @args } }
function Stop-AlloyedProcess     { Invoke-AlloyedDecoratedCommand -Operation 'Stop-Process' -Arguments $args -Action {Microsoft.PowerShell.Management\Stop-Process @args } }
function Wait-AlloyedProcess     { Invoke-AlloyedDecoratedCommand -Operation 'Wait-Process' -Arguments $args -Action {Microsoft.PowerShell.Management\Wait-Process @args } }
function Test-AlloyedConnection  { Invoke-AlloyedDecoratedCommand -Operation 'Test-Connection' -Arguments $args -Action {Microsoft.PowerShell.Management\Test-Connection @args } }
function Invoke-AlloyedCommand   { Invoke-AlloyedDecoratedCommand -Operation 'Invoke-Command' -Arguments $args -Action {Microsoft.PowerShell.Core\Invoke-Command @args } }

# ---------------------------------------------------------------------------
# System.Archive wrappers
# ---------------------------------------------------------------------------

function Compress-AlloyedArchive { Invoke-AlloyedDecoratedCommand -Operation 'Compress-Archive' -Arguments $args -Action {Microsoft.PowerShell.Archive\Compress-Archive @args } }
function Expand-AlloyedArchive   { Invoke-AlloyedDecoratedCommand -Operation 'Expand-Archive' -Arguments $args -Action {Microsoft.PowerShell.Archive\Expand-Archive @args } }

# ---------------------------------------------------------------------------
# System.Management wrappers
# ---------------------------------------------------------------------------

function Get-AlloyedService     { Invoke-AlloyedDecoratedCommand -Operation 'Get-Service' -Arguments $args -Action {Microsoft.PowerShell.Management\Get-Service @args } }
function Start-AlloyedService   { Invoke-AlloyedDecoratedCommand -Operation 'Start-Service' -Arguments $args -Action {Microsoft.PowerShell.Management\Start-Service @args } }
function Stop-AlloyedService    { Invoke-AlloyedDecoratedCommand -Operation 'Stop-Service' -Arguments $args -Action {Microsoft.PowerShell.Management\Stop-Service @args } }
function Restart-AlloyedService { Invoke-AlloyedDecoratedCommand -Operation 'Restart-Service' -Arguments $args -Action {Microsoft.PowerShell.Management\Restart-Service @args } }

# ---------------------------------------------------------------------------
# System.Security wrappers
# ---------------------------------------------------------------------------

function Get-AlloyedAcl                   { Invoke-AlloyedDecoratedCommand -Operation 'Get-Acl' -Arguments $args -Action {Microsoft.PowerShell.Security\Get-Acl @args } }
function Set-AlloyedAcl                   { Invoke-AlloyedDecoratedCommand -Operation 'Set-Acl' -Arguments $args -Action {Microsoft.PowerShell.Security\Set-Acl @args } }
function Get-AlloyedCredential            { Invoke-AlloyedDecoratedCommand -Operation 'Get-Credential' -Arguments $args -Action {Microsoft.PowerShell.Security\Get-Credential @args } }
function ConvertTo-AlloyedSecureString    { Invoke-AlloyedDecoratedCommand -Operation 'ConvertTo-SecureString' -Arguments $args -Action {Microsoft.PowerShell.Security\ConvertTo-SecureString @args } }
function ConvertFrom-AlloyedSecureString  { Invoke-AlloyedDecoratedCommand -Operation 'ConvertFrom-SecureString' -Arguments $args -Action {Microsoft.PowerShell.Security\ConvertFrom-SecureString @args } }
function Get-AlloyedAuthenticodeSignature { Invoke-AlloyedDecoratedCommand -Operation 'Get-AuthenticodeSignature' -Arguments $args -Action {Microsoft.PowerShell.Security\Get-AuthenticodeSignature @args } }
function Set-AlloyedAuthenticodeSignature { Invoke-AlloyedDecoratedCommand -Operation 'Set-AuthenticodeSignature' -Arguments $args -Action {Microsoft.PowerShell.Security\Set-AuthenticodeSignature @args } }
function New-AlloyedSelfSignedCertificate { Invoke-AlloyedDecoratedCommand -Operation 'New-SelfSignedCertificate' -Arguments $args -Action {Microsoft.PowerShell.Security\New-SelfSignedCertificate @args } }
function Get-AlloyedPfxCertificate        { Invoke-AlloyedDecoratedCommand -Operation 'Get-PfxCertificate' -Arguments $args -Action {Microsoft.PowerShell.Security\Get-PfxCertificate @args } }
function Export-AlloyedPfxCertificate     { Invoke-AlloyedDecoratedCommand -Operation 'Export-PfxCertificate' -Arguments $args -Action {Microsoft.PowerShell.Security\Export-PfxCertificate @args } }

# ---------------------------------------------------------------------------
# System.Host wrappers
# ---------------------------------------------------------------------------

function Write-AlloyedHost     { Invoke-AlloyedDecoratedCommand -Operation 'Write-Host' -Arguments $args -Action {Microsoft.PowerShell.Utility\Write-Host @args } }
function Read-AlloyedHost      { Invoke-AlloyedDecoratedCommand -Operation 'Read-Host' -Arguments $args -Action {Microsoft.PowerShell.Utility\Read-Host @args } }
function Write-AlloyedProgress { Invoke-AlloyedDecoratedCommand -Operation 'Write-Progress' -Arguments $args -Action {Microsoft.PowerShell.Utility\Write-Progress @args } }
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
