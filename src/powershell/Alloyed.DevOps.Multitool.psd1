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
        'Get-AlloyedTransparencyModeStatus',
        # Provider.FileSystem wrappers
        'Get-AlloyedChildItem',
        'Get-AlloyedItem',
        'Test-AlloyedPath',
        'Copy-AlloyedItem',
        'Move-AlloyedItem',
        'Remove-AlloyedItem',
        'New-AlloyedItem',
        'Get-AlloyedContent',
        'Set-AlloyedContent',
        'Get-AlloyedLocation',
        'Set-AlloyedLocation',
        'Push-AlloyedLocation',
        'Pop-AlloyedLocation',
        'Join-AlloyedPath',
        'Split-AlloyedPath',
        'Resolve-AlloyedPath',
        # System.Utility wrappers
        'Select-AlloyedString',
        'ConvertTo-AlloyedJson',
        'ConvertFrom-AlloyedJson',
        'ConvertTo-AlloyedXml',
        'Get-AlloyedRandom',
        'Measure-AlloyedObject',
        'Sort-AlloyedObject',
        'Group-AlloyedObject',
        # System.Diagnostics wrappers
        'Get-AlloyedProcess',
        'Start-AlloyedProcess',
        'Stop-AlloyedProcess',
        'Wait-AlloyedProcess',
        'Test-AlloyedConnection',
        'Invoke-AlloyedCommand',
        # System.Archive wrappers
        'Compress-AlloyedArchive',
        'Expand-AlloyedArchive',
        # System.Management wrappers
        'Get-AlloyedService',
        'Start-AlloyedService',
        'Stop-AlloyedService',
        'Restart-AlloyedService',
        # System.Security wrappers
        'Get-AlloyedAcl',
        'Set-AlloyedAcl',
        'Get-AlloyedCredential',
        'ConvertTo-AlloyedSecureString',
        'ConvertFrom-AlloyedSecureString',
        'Get-AlloyedAuthenticodeSignature',
        'Set-AlloyedAuthenticodeSignature',
        'New-AlloyedSelfSignedCertificate',
        'Get-AlloyedPfxCertificate',
        'Export-AlloyedPfxCertificate',
        # System.Host wrappers
        'Write-AlloyedHost',
        'Read-AlloyedHost',
        'Write-AlloyedProgress',
        'Clear-AlloyedHost'
    )
    CmdletsToExport = @()
    VariablesToExport = @()
    AliasesToExport = @()
}
