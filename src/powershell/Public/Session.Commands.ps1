function Set-AlloyedRuntimeConfig {
<#
.SYNOPSIS
    Reads the runtime configuration from disk and applies it to the current session state.
.DESCRIPTION
    Loads the merged runtime configuration from BasePath and calls Set-AlloyedSessionState to
    synchronize the in-process transparency and session flags with the configured values. This
    is called automatically by Start-AlloyedSession and can also be called standalone to
    re-apply configuration after a config file change.
.PARAMETER BasePath
    Base directory from which config files are resolved. Defaults to Get-AlloyedProjectRoot.
.PARAMETER QuietTransparency
    When specified, transparency logging is activated without setting the verbose flag, reducing
    decorator event output to operation names only.
.OUTPUTS
    PSCustomObject from Get-AlloyedTransparencyModeStatus.
.EXAMPLE
    PS> Set-AlloyedRuntimeConfig
.EXAMPLE
    PS> Set-AlloyedRuntimeConfig -BasePath ./myproject -QuietTransparency
#>
    [CmdletBinding()]
    param(
        [Parameter()] [string]$BasePath = (Get-AlloyedProjectRoot),
        [Parameter()] [switch]$QuietTransparency
    )

    $effective = Get-AlloyedRuntimeConfiguration -BasePath $BasePath
    $profile = [string]$effective.Decoration.TransparencyProfile
    if ([string]::IsNullOrWhiteSpace($profile)) {
        $profile = 'standard'
    }
    $profile = $profile.Trim().ToLowerInvariant()

    Set-AlloyedSessionState -EnableTransparency:$effective.Decoration.EnableTransparency `
        -EnableSession:$effective.Session.Enabled `
        -Profile $profile `
        -QuietTransparency:$QuietTransparency | Out-Null

    return Get-AlloyedTransparencyModeStatus
}

function Start-AlloyedSession {
<#
.SYNOPSIS
    Starts a full Alloyed session with transparency and command interception enabled.
.DESCRIPTION
    Loads the runtime configuration, applies it, then forces both transparency mode and session
    interception on regardless of the configured defaults. Prints a status banner with the active
    settings and next-step guidance. Use Stop-AlloyedSession to tear down the session.
.PARAMETER BasePath
    Base directory from which config files are resolved. Defaults to Get-AlloyedProjectRoot.
.PARAMETER Profile
    Transparency profile to activate: minimal, standard, or debug. When omitted, the profile
    from the runtime configuration is used.
.PARAMETER OutputMode
    Selects the console rendering back-end (Plain or Rich). When omitted, the value from the
    runtime configuration is used.
.PARAMETER QuietTransparency
    Activates transparency without enabling verbose decorator output.
.OUTPUTS
    PSCustomObject from Get-AlloyedTransparencyModeStatus.
.EXAMPLE
    PS> Start-AlloyedSession
.EXAMPLE
    PS> Start-AlloyedSession -Profile debug -OutputMode Rich
#>
    [CmdletBinding()]
    param(
        [Parameter()] [string]$BasePath = (Get-AlloyedProjectRoot),
        [Parameter()] [ValidateSet('minimal','standard','debug')] [string]$Profile,
        [Parameter()] [ValidateSet('Plain','Rich')] [string]$OutputMode,
        [Parameter()] [switch]$QuietTransparency
    )

    $status = Set-AlloyedRuntimeConfig -BasePath $BasePath -QuietTransparency:$QuietTransparency
    $effectiveProfile = if (-not [string]::IsNullOrWhiteSpace($Profile)) { $Profile } else { [string]$status.Profile }
    $effectiveOutputMode = if (-not [string]::IsNullOrWhiteSpace($OutputMode)) { $OutputMode } else { [string]$status.OutputMode }

    $status = Set-AlloyedSessionState -EnableTransparency:$true `
        -EnableSession:$true `
        -Profile $effectiveProfile `
        -OutputMode $effectiveOutputMode `
        -QuietTransparency:$QuietTransparency

    $reporter = Get-AlloyedConsoleReporter
    [Alloyed.DevOps.Multitool.Host.PowerShell.Services.SessionConsolePresenter]::WriteSessionReady(
        $reporter,
        [bool]$status.Enabled,
        [bool]$status.SessionModeEnabled,
        [string]$status.Profile,
        [string]$status.OutputMode)

    return $status
}

function Stop-AlloyedSession {
<#
.SYNOPSIS
    Stops the current Alloyed session, disabling transparency and command interception.
.DESCRIPTION
    Calls Set-AlloyedSessionState to disable both transparency mode and session mode in a
    single atomic operation, then prints a confirmation message.
.OUTPUTS
    PSCustomObject from Get-AlloyedTransparencyModeStatus.
.EXAMPLE
    PS> Stop-AlloyedSession
#>
    [CmdletBinding()]
    param()

    Set-AlloyedSessionState -EnableTransparency:$false -EnableSession:$false | Out-Null

    $reporter = Get-AlloyedConsoleReporter
    [Alloyed.DevOps.Multitool.Host.PowerShell.Services.SessionConsolePresenter]::WriteSessionStopped($reporter)
    return Get-AlloyedTransparencyModeStatus
}

function Set-AlloyedSessionState {
<#
.SYNOPSIS
    Atomically applies transparency and session interception enable/disable state.
.DESCRIPTION
    Single entry point for toggling both transparency mode and session mode together. When
    EnableTransparency is false, both modes are disabled and the transparency profile
    environment variable is cleared. When true, Enable-AlloyedTransparencyMode is called with
    the supplied options and session mode is activated unless EnableSession is false.
.PARAMETER EnableTransparency
    True to activate transparency logging; false to disable it and session mode.
.PARAMETER EnableSession
    True to also enable command-interception session mode alongside transparency.
.PARAMETER Profile
    Transparency profile: minimal, standard, or debug. Defaults to standard.
.PARAMETER OutputMode
    Console rendering back-end (Plain or Rich). When omitted the current override is preserved.
.PARAMETER QuietTransparency
    When specified, transparency is enabled without the verbose flag.
.OUTPUTS
    PSCustomObject from Get-AlloyedTransparencyModeStatus.
.EXAMPLE
    PS> Set-AlloyedSessionState -EnableTransparency $true -EnableSession $true -Profile debug
.EXAMPLE
    PS> Set-AlloyedSessionState -EnableTransparency $false -EnableSession $false
#>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [bool]$EnableTransparency,
        [Parameter(Mandatory)] [bool]$EnableSession,
        [Parameter()] [ValidateSet('minimal','standard','debug')] [string]$Profile = 'standard',
        [Parameter()] [ValidateSet('Plain','Rich')] [string]$OutputMode,
        [Parameter()] [switch]$QuietTransparency
    )

    if (-not $EnableTransparency) {
        $null = Disable-AlloyedTransparencyMode
        if ($script:SessionModeEnabled) {
            $null = Disable-AlloyedSessionMode
        }
        [System.Environment]::SetEnvironmentVariable('ALLOYED_TRANSPARENCY_PROFILE', $null, 'Process')
        return Get-AlloyedTransparencyModeStatus
    }

    $enableParams = @{
        SkipSessionMode = (-not $EnableSession)
        Profile = $Profile
    }

    if (-not [string]::IsNullOrWhiteSpace($OutputMode)) {
        $enableParams['OutputMode'] = $OutputMode
    }

    if ($QuietTransparency.IsPresent) {
        $enableParams['Quiet'] = $true
    }

    $null = Enable-AlloyedTransparencyMode @enableParams
    return Get-AlloyedTransparencyModeStatus
}

function Set-AlloyedTransparencyProfile {
<#
.SYNOPSIS
    Sets the active transparency profile for the current process.
.DESCRIPTION
    Writes the profile name to the ALLOYED_TRANSPARENCY_PROFILE environment variable at
    process scope. Takes effect immediately for all subsequent decorated command executions.
    Does not alter whether transparency mode is enabled or disabled.
.PARAMETER Profile
    Profile name: minimal (operation name only), standard (high-signal tags), or
    debug (all tags including low-signal ones).
.OUTPUTS
    PSCustomObject from Get-AlloyedTransparencyModeStatus.
.EXAMPLE
    PS> Set-AlloyedTransparencyProfile -Profile debug
.EXAMPLE
    PS> Set-AlloyedTransparencyProfile -Profile minimal
#>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [ValidateSet('minimal','standard','debug')] [string]$Profile
    )

    [System.Environment]::SetEnvironmentVariable('ALLOYED_TRANSPARENCY_PROFILE', $Profile, 'Process')
    return Get-AlloyedTransparencyModeStatus
}

function Get-AlloyedSessionState {
<#
.SYNOPSIS
    Returns a combined snapshot of runtime configuration and live in-process session state.
.DESCRIPTION
    Merges values from the runtime configuration file (what is configured) with the live
    module-scope variables (what is actually active right now). Useful for diagnosing
    mismatches between the config file and the running session.
.PARAMETER BasePath
    Base directory from which config files are resolved. Defaults to Get-AlloyedProjectRoot.
.OUTPUTS
    PSCustomObject with properties: RuntimeConfigPath, RuntimeSessionEnabled,
    RuntimeTransparencyEnabled, RuntimeTransparencyProfile, CurrentSessionModeEnabled,
    CurrentTransparencyEnabled, CurrentProfile, CurrentOutputMode, CurrentVerbose.
.EXAMPLE
    PS> Get-AlloyedSessionState
.EXAMPLE
    PS> Get-AlloyedSessionState | Select-Object Current*
#>
    [CmdletBinding()]
    param(
        [Parameter()] [string]$BasePath = (Get-AlloyedProjectRoot)
    )

    $runtime = Get-AlloyedRuntimeConfiguration -BasePath $BasePath
    $transparency = Get-AlloyedTransparencyModeStatus
    $session = Get-AlloyedSessionModeStatus

    [pscustomobject]@{
        PSTypeName = 'Alloyed.SessionState'
        RuntimeConfigPath = Get-AlloyedRuntimeConfigFilePath -BasePath $BasePath
        RuntimeSessionEnabled = [bool]$runtime.Session.Enabled
        RuntimeTransparencyEnabled = [bool]$runtime.Decoration.EnableTransparency
        RuntimeTransparencyProfile = [string]$runtime.Decoration.TransparencyProfile
        CurrentSessionModeEnabled = [bool]$session.Enabled
        CurrentTransparencyEnabled = [bool]$transparency.Enabled
        CurrentProfile = [string]$transparency.Profile
        CurrentOutputMode = [string]$transparency.OutputMode
        CurrentVerbose = [string]$transparency.Verbose
    }
}
