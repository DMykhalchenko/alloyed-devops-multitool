$script:AssemblyLoaded = $false
$script:SessionModeEnabled = $false
$script:SessionModeAliases = @()
$script:SessionModeAliasBackup = @{}
$script:SessionModeCommandBackup = @{}
$script:DecorationPipeline = $null
$script:TransparencyModeOverride = $null
$script:ConsoleOutputModeOverride = $null
$script:LastRuntimeExecution = $null
$script:AlloyedModuleRoot = $PSScriptRoot

# Layered command composition: keep public session/runtime orchestration
# decoupled from low-level implementation helpers in this file.
. (Join-Path $PSScriptRoot 'Public/Session.Commands.ps1')
. (Join-Path $PSScriptRoot 'Public/Pipeline.Commands.ps1')
. (Join-Path $PSScriptRoot 'Public/Transparency.Commands.ps1')
. (Join-Path $PSScriptRoot 'Internal/Host.Bootstrap.ps1')
. (Join-Path $PSScriptRoot 'Internal/Runtime.Configuration.ps1')
. (Join-Path $PSScriptRoot 'Internal/Session.Interception.ps1')
. (Join-Path $PSScriptRoot 'Internal/Runtime.Execution.ps1')

# ---------------------------------------------------------------------------
# Provider.FileSystem wrappers
# ---------------------------------------------------------------------------

# Auto-generated wrappers are maintained in a separate layer.
. (Join-Path $PSScriptRoot 'Public/Ports.Wrappers.ps1')

