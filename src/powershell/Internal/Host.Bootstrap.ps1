function Initialize-AlloyedHostAssembly {
    if ($script:AssemblyLoaded) { return }

    $moduleBasePath = if (-not [string]::IsNullOrWhiteSpace($script:AlloyedModuleRoot)) {
        $script:AlloyedModuleRoot
    } else {
        Split-Path -Parent $PSScriptRoot
    }

    $packagedDllPath = Join-Path $moduleBasePath 'lib/Alloyed.DevOps.Multitool.Host.PowerShell.dll'
    $packagedDecorationDllPath = Join-Path $moduleBasePath 'lib/Alloyed.DevOps.Multitool.Core.Decoration.dll'

    $moduleRoot = Split-Path -Parent $moduleBasePath
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

function Resolve-AlloyedTransparencyEnabled {
    Initialize-AlloyedHostAssembly

    if ($null -ne $script:TransparencyModeOverride) {
        return [bool]$script:TransparencyModeOverride
    }

    $configuration = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.PipelineBootstrap]::CreateRuntimeConfiguration((Get-Location).Path, $null)
    return [bool]$configuration.Decoration.EnableTransparency
}

function Resolve-AlloyedConsoleOutputMode {
    Initialize-AlloyedHostAssembly

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
