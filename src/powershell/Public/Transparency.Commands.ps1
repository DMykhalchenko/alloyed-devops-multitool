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
