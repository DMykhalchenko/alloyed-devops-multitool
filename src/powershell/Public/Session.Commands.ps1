function Apply-AlloyedRuntimeConfig {
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
    [CmdletBinding()]
    param(
        [Parameter()] [string]$BasePath = (Get-AlloyedProjectRoot),
        [Parameter()] [ValidateSet('minimal','standard','debug')] [string]$Profile,
        [Parameter()] [ValidateSet('Plain','Rich')] [string]$OutputMode,
        [Parameter()] [switch]$QuietTransparency
    )

    $status = Apply-AlloyedRuntimeConfig -BasePath $BasePath -QuietTransparency:$QuietTransparency
    $effectiveProfile = if (-not [string]::IsNullOrWhiteSpace($Profile)) { $Profile } else { [string]$status.Profile }
    $effectiveOutputMode = if (-not [string]::IsNullOrWhiteSpace($OutputMode)) { $OutputMode } else { [string]$status.OutputMode }

    $status = Set-AlloyedSessionState -EnableTransparency:$true `
        -EnableSession:$true `
        -Profile $effectiveProfile `
        -OutputMode $effectiveOutputMode `
        -QuietTransparency:$QuietTransparency

    Write-Host "Alloyed session is ready."
    Write-Host ("  Transparency: {0}" -f $status.Enabled)
    Write-Host ("  SessionMode : {0}" -f $status.SessionModeEnabled)
    Write-Host ("  Profile     : {0}" -f $status.Profile)
    Write-Host ("  OutputMode  : {0}" -f $status.OutputMode)
    Write-Host "Next:"
    Write-Host "  1) Run your script"
    Write-Host "     ./scripts/automation.ps1"
    Write-Host "  2) Check status"
    Write-Host "     Get-AlloyedTransparencyModeStatus"
    Write-Host "  3) Stop interception (if needed)"
    Write-Host "     Disable-AlloyedSessionMode; Disable-AlloyedTransparencyMode"

    return $status
}

function Stop-AlloyedSession {
    [CmdletBinding()]
    param()

    Set-AlloyedSessionState -EnableTransparency:$false -EnableSession:$false | Out-Null

    Write-Host "Alloyed session stopped."
    return Get-AlloyedTransparencyModeStatus
}

function Set-AlloyedSessionState {
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
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [ValidateSet('minimal','standard','debug')] [string]$Profile
    )

    [System.Environment]::SetEnvironmentVariable('ALLOYED_TRANSPARENCY_PROFILE', $Profile, 'Process')
    return Get-AlloyedTransparencyModeStatus
}

function Get-AlloyedSessionState {
    [CmdletBinding()]
    param(
        [Parameter()] [string]$BasePath = (Get-AlloyedProjectRoot)
    )

    $runtime = Get-AlloyedRuntimeConfiguration -BasePath $BasePath
    $transparency = Get-AlloyedTransparencyModeStatus
    $session = Get-AlloyedSessionModeStatus

    [pscustomobject]@{
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
