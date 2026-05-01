function Enable-AlloyedTransparencyMode {
<#
.SYNOPSIS
    Enables Alloyed transparency mode for the current process.
.DESCRIPTION
    Activates decorator-level event logging by setting the in-process transparency override
    flag and writing the profile and verbose preference to process-scoped environment
    variables. Unless SkipSessionMode is specified, command-interception session mode is also
    enabled so that Alloyed wrapper functions intercept calls made during the session.
.PARAMETER SkipSessionMode
    When specified, command-interception (session mode) is not enabled; only the transparency
    logging flag is set.
.PARAMETER OutputMode
    Selects the console rendering back-end (Plain or Rich) to use while transparency is active.
    When omitted, the current session override is preserved.
.PARAMETER Quiet
    Activates transparency without setting the verbose flag. Decorator events are emitted at
    reduced verbosity (operation name only for the minimal profile).
.PARAMETER Profile
    Transparency profile: minimal (operation name only), standard (high-signal tags), or
    debug (all tags). Defaults to standard.
.OUTPUTS
    PSCustomObject from Get-AlloyedTransparencyModeStatus.
.EXAMPLE
    PS> Enable-AlloyedTransparencyMode
.EXAMPLE
    PS> Enable-AlloyedTransparencyMode -Profile debug -OutputMode Rich
.EXAMPLE
    PS> Enable-AlloyedTransparencyMode -SkipSessionMode -Quiet
#>
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
<#
.SYNOPSIS
    Disables Alloyed transparency mode for the current process.
.DESCRIPTION
    Clears the in-process transparency override flag and removes the ALLOYED_TRANSPARENCY_VERBOSE
    and ALLOYED_TRANSPARENCY_PROFILE environment variables from the process scope. The console
    output mode override is also cleared. Does not automatically disable session mode.
.OUTPUTS
    PSCustomObject from Get-AlloyedTransparencyModeStatus.
.EXAMPLE
    PS> Disable-AlloyedTransparencyMode
#>
    [CmdletBinding()]
    param()

    $script:TransparencyModeOverride = $false
    $script:ConsoleOutputModeOverride = $null
    [System.Environment]::SetEnvironmentVariable('ALLOYED_TRANSPARENCY_VERBOSE', $null, 'Process')
    [System.Environment]::SetEnvironmentVariable('ALLOYED_TRANSPARENCY_PROFILE', $null, 'Process')
    Get-AlloyedTransparencyModeStatus
}

function Get-AlloyedTransparencyModeStatus {
<#
.SYNOPSIS
    Returns the current Alloyed transparency mode status.
.DESCRIPTION
    Reads the live in-process state to report whether transparency is enabled, what triggered
    it (explicit override vs. configuration), whether session mode is active, the current
    console output mode, verbosity setting, and active transparency profile.
.OUTPUTS
    PSCustomObject with properties:
      Enabled         — [bool] whether transparency is currently active.
      Override        — [bool|string] $true/$false when set explicitly; '<config>' when driven by configuration.
      SessionModeEnabled — [bool] whether command-interception session mode is active.
      OutputMode      — [string] active console output mode (Plain or Rich).
      Verbose         — [string] value of ALLOYED_TRANSPARENCY_VERBOSE ('true', 'false', or $null).
      Profile         — [string] value of ALLOYED_TRANSPARENCY_PROFILE, or $null when unset.
.EXAMPLE
    PS> Get-AlloyedTransparencyModeStatus
.EXAMPLE
    PS> (Get-AlloyedTransparencyModeStatus).Enabled
#>
    [CmdletBinding()]
    param()

    [pscustomobject]@{
        PSTypeName = 'Alloyed.TransparencyModeStatus'
        Enabled = Resolve-AlloyedTransparencyEnabled
        Override = if ($null -eq $script:TransparencyModeOverride) { '<config>' } else { [bool]$script:TransparencyModeOverride }
        SessionModeEnabled = [bool]$script:SessionModeEnabled
        OutputMode = (Resolve-AlloyedConsoleOutputMode).ToString()
        Verbose = [System.Environment]::GetEnvironmentVariable('ALLOYED_TRANSPARENCY_VERBOSE')
        Profile = [System.Environment]::GetEnvironmentVariable('ALLOYED_TRANSPARENCY_PROFILE')
    }
}
