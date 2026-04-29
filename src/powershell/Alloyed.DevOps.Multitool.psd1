@{
    RootModule = 'Alloyed.DevOps.Multitool.psm1'
    ModuleVersion = '0.1.0'
    GUID = '7df0249a-f47f-4f56-a813-7e2ebcdb0b4a'
    Author = 'Alloyed'
    CompanyName = 'Alloyed'
    Copyright = '(c) Alloyed'
    Description = 'PowerShell wrappers for Alloyed transformation pipeline.'
    PowerShellVersion = '7.0'
    CompatiblePSEditions = @('Core')
    FunctionsToExport = @(
        # pipeline cmdlets
        'New-AlloyedModuleTransform',
        'Test-AlloyedTransform',
        'Get-AlloyedCatalog',
        'Get-AlloyedRuntimeConfiguration',
        'Enable-AlloyedSessionMode',
        'Disable-AlloyedSessionMode',
        'Get-AlloyedSessionModeStatus',
        'Enable-AlloyedTransparencyMode',
        'Disable-AlloyedTransparencyMode',
        'Set-AlloyedTransparencyProfile',
        'Get-AlloyedTransparencyModeStatus',
        'Invoke-AlloyedScript',
        'Start-AlloyedSession',
        'Stop-AlloyedSession',
        'Initialize-AlloyedRuntimeConfig',
        'Apply-AlloyedRuntimeConfig',
        'Test-AlloyedRuntimeConfig'
    )
    CmdletsToExport = @()
    VariablesToExport = @()
    AliasesToExport = @()
}
