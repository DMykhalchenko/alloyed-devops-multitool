[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter()] [string]$Owner = "Ligare-Method",
    [Parameter()] [string]$Repository = "alloyed-devops-multitool",
    [Parameter()] [string]$ModuleName = "Alloyed.DevOps.Multitool",
    [Parameter(Mandatory)] [string]$Version,
    [Parameter()] [string]$RepositoryName,
    [Parameter()] [string]$GitHubUser = "DMykhalchenko",
    [Parameter()] [string]$Token = $env:LIGARE_GITHUB_PAT_TOKEN,
    [Parameter()] [switch]$Force
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Token)) {
    throw "GitHub token is required. Pass -Token or set LIGARE_GITHUB_PAT_TOKEN."
}

Import-Module Microsoft.PowerShell.PSResourceGet -ErrorAction Stop

$feedUri = "https://nuget.pkg.github.com/$Owner/index.json"
if ([string]::IsNullOrWhiteSpace($RepositoryName)) {
    $RepositoryName = "GitHubPackages-$Owner-$Repository"
}

$secureToken = ConvertTo-SecureString $Token -AsPlainText -Force
$credential = [pscredential]::new($GitHubUser, $secureToken)
$credentialInfo = $null
$tempSecretName = $null

function Ensure-ModuleAvailable {
    param(
        [Parameter(Mandatory)] [string]$Name
    )

    if (Get-Module -ListAvailable -Name $Name) {
        return
    }

    Write-Host "Installing dependency module '$Name' for CurrentUser..."
    Install-Module -Name $Name -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop
}

Write-Host "[1/3] Registering PSResource repository: $RepositoryName -> $feedUri"
$existing = Get-PSResourceRepository -Name $RepositoryName -ErrorAction SilentlyContinue
if ($existing) {
    Unregister-PSResourceRepository -Name $RepositoryName
}

$registerParams = @{
    Name = $RepositoryName
    Uri = $feedUri
    Trusted = $true
}
$registerCommand = Get-Command Register-PSResourceRepository
if ($registerCommand.Parameters.ContainsKey('CredentialInfo')) {
    try {
        Ensure-ModuleAvailable -Name Microsoft.PowerShell.SecretManagement
        Ensure-ModuleAvailable -Name Microsoft.PowerShell.SecretStore

        Import-Module Microsoft.PowerShell.SecretManagement -ErrorAction Stop
        Import-Module Microsoft.PowerShell.SecretStore -ErrorAction Stop

        # Configure SecretStore for non-interactive automation in user scope.
        try {
            Set-SecretStoreConfiguration -Scope CurrentUser -Authentication None -Interaction None -Confirm:$false -ErrorAction Stop
        } catch {
            # If configuration is already set or command is unavailable in current version, continue.
        }

        $vault = Get-SecretVault -Name SecretStore -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $vault) {
            Register-SecretVault -Name SecretStore -ModuleName Microsoft.PowerShell.SecretStore -DefaultVault -ErrorAction Stop
            $vault = Get-SecretVault -Name SecretStore -ErrorAction SilentlyContinue | Select-Object -First 1
        }

        if (-not $vault) {
            $vault = Get-SecretVault | Select-Object -First 1
        }
        if (-not $vault) {
            throw "No SecretManagement vault is registered."
        }

        $tempSecretName = "alloyed-ghpkg-{0}" -f ([guid]::NewGuid().ToString("N"))
        Set-Secret -Name $tempSecretName -Vault $vault.Name -Secret $credential
        $credentialInfo = [Microsoft.PowerShell.PSResourceGet.UtilClasses.PSCredentialInfo]::new($vault.Name, $tempSecretName)
    } catch {
        throw "This PSResourceGet version requires CredentialInfo (SecretManagement-backed). Install/configure Microsoft.PowerShell.SecretManagement and a vault, then retry. Details: $($_.Exception.Message)"
    }

    $registerParams['CredentialInfo'] = $credentialInfo
} elseif ($registerCommand.Parameters.ContainsKey('Credential')) {
    $registerParams['Credential'] = $credential
}

Register-PSResourceRepository @registerParams

Write-Host "[2/3] Installing module: $ModuleName $Version"
$installParams = @{
    Name = $ModuleName
    Repository = $RepositoryName
    Version = $Version
    TrustRepository = $true
}
$installCommand = Get-Command Install-PSResource
if ($installCommand.Parameters.ContainsKey('CredentialInfo')) {
    if ($null -eq $credentialInfo) {
        throw "Install-PSResource expects CredentialInfo but none is available."
    }
    $installParams['CredentialInfo'] = $credentialInfo
} elseif ($installCommand.Parameters.ContainsKey('Credential')) {
    $installParams['Credential'] = $credential
}
if ($Force.IsPresent) {
    $installParams["Reinstall"] = $true
}

if ($PSCmdlet.ShouldProcess("$ModuleName $Version", "Install from $RepositoryName")) {
    Install-PSResource @installParams
}

Write-Host "[3/3] Validating module availability"
$targetVersion = $Version
$installed = Get-InstalledPSResource -Name $ModuleName -ErrorAction SilentlyContinue |
    Where-Object {
        $v = $_.Version.ToString()
        $p = $_.Prerelease
        $full = if ([string]::IsNullOrWhiteSpace($p)) { $v } else { "$v-$p" }
        ($v -eq $targetVersion) -or ($full -eq $targetVersion)
    } |
    Select-Object -First 1

if (-not $installed) {
    throw "Module $ModuleName version $Version was not found after installation."
}

if (-not [string]::IsNullOrWhiteSpace($tempSecretName)) {
    try {
        $vault = Get-SecretVault | Select-Object -First 1
        if ($vault) {
            Remove-Secret -Name $tempSecretName -Vault $vault.Name -ErrorAction SilentlyContinue
        }
    } catch {
        # best-effort cleanup
    }
}

Write-Host "Done. Installed $ModuleName $Version from $feedUri"
