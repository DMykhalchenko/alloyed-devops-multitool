$script:AssemblyLoaded = $false

function Initialize-AlloyedHostAssembly {
    if ($script:AssemblyLoaded) { return }

    $moduleRoot = Split-Path -Parent $PSScriptRoot
    $dllPath = Join-Path $moduleRoot 'dotnet/Alloyed.DevOps.Multitool.Host.PowerShell/bin/Debug/net8.0/Alloyed.DevOps.Multitool.Host.PowerShell.dll'

    if (-not (Test-Path $dllPath)) {
        throw "Host assembly not found at '$dllPath'. Build solution first."
    }

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

# ---------------------------------------------------------------------------
# Provider.FileSystem wrappers
# ---------------------------------------------------------------------------

function Get-AlloyedChildItem { Get-ChildItem @PSBoundParameters }
function Get-AlloyedItem       { Get-Item @PSBoundParameters }
function Test-AlloyedPath      { Test-Path @PSBoundParameters }

# ---------------------------------------------------------------------------
# System.Utility wrappers
# ---------------------------------------------------------------------------

function Select-AlloyedString    { Select-String @PSBoundParameters }
function ConvertTo-AlloyedJson   { ConvertTo-Json @PSBoundParameters }
function ConvertFrom-AlloyedJson { ConvertFrom-Json @PSBoundParameters }
function ConvertTo-AlloyedXml    { ConvertTo-Xml @PSBoundParameters }
function Get-AlloyedRandom       { Get-Random @PSBoundParameters }
function Measure-AlloyedObject   { Measure-Object @PSBoundParameters }
function Sort-AlloyedObject      { Sort-Object @PSBoundParameters }
function Group-AlloyedObject     { Group-Object @PSBoundParameters }

# ---------------------------------------------------------------------------
# System.Diagnostics wrappers
# ---------------------------------------------------------------------------

function Get-AlloyedProcess      { Get-Process @PSBoundParameters }
function Start-AlloyedProcess    { Start-Process @PSBoundParameters }
function Stop-AlloyedProcess     { Stop-Process @PSBoundParameters }
function Wait-AlloyedProcess     { Wait-Process @PSBoundParameters }
function Test-AlloyedConnection  { Test-Connection @PSBoundParameters }
function Invoke-AlloyedCommand   { Invoke-Command @PSBoundParameters }

# ---------------------------------------------------------------------------
# System.Archive wrappers
# ---------------------------------------------------------------------------

function Compress-AlloyedArchive { Compress-Archive @PSBoundParameters }
function Expand-AlloyedArchive   { Expand-Archive @PSBoundParameters }

# ---------------------------------------------------------------------------
# System.Management wrappers
# ---------------------------------------------------------------------------

function Get-AlloyedService     { Get-Service @PSBoundParameters }
function Start-AlloyedService   { Start-Service @PSBoundParameters }
function Stop-AlloyedService    { Stop-Service @PSBoundParameters }
function Restart-AlloyedService { Restart-Service @PSBoundParameters }

# ---------------------------------------------------------------------------
# System.Security wrappers
# ---------------------------------------------------------------------------

function Get-AlloyedAcl                   { Get-Acl @PSBoundParameters }
function Set-AlloyedAcl                   { Set-Acl @PSBoundParameters }
function Get-AlloyedCredential            { Get-Credential @PSBoundParameters }
function ConvertTo-AlloyedSecureString    { ConvertTo-SecureString @PSBoundParameters }
function ConvertFrom-AlloyedSecureString  { ConvertFrom-SecureString @PSBoundParameters }
function Get-AlloyedAuthenticodeSignature { Get-AuthenticodeSignature @PSBoundParameters }
function Set-AlloyedAuthenticodeSignature { Set-AuthenticodeSignature @PSBoundParameters }
function New-AlloyedSelfSignedCertificate { New-SelfSignedCertificate @PSBoundParameters }
function Get-AlloyedPfxCertificate        { Get-PfxCertificate @PSBoundParameters }
function Export-AlloyedPfxCertificate     { Export-PfxCertificate @PSBoundParameters }

# ---------------------------------------------------------------------------
# System.Host wrappers
# ---------------------------------------------------------------------------

function Write-AlloyedHost     { Write-Host @PSBoundParameters }
function Read-AlloyedHost      { Read-Host @PSBoundParameters }
function Write-AlloyedProgress { Write-Progress @PSBoundParameters }
function Clear-AlloyedHost     { Clear-Host }

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
