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
Register-PSResourceRepository @registerParams

Write-Host "[2/3] Installing module: $ModuleName $Version"
$installParams = @{
    Name = $ModuleName
    Repository = $RepositoryName
    Version = $Version
    TrustRepository = $true
}
$installCommand = Get-Command Install-PSResource
if ($installCommand.Parameters.ContainsKey('Credential')) {
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

Write-Host "Done. Installed $ModuleName $Version from $feedUri"
