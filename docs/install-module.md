# Install Module from GitHub Packages

Use PowerShell package tooling (`PSResourceGet`) to install this module.

## Why not `dotnet add package`

`Alloyed.DevOps.Multitool` is a PowerShell module, not a NuGet library for project references.
Use `Install-PSResource` / `Import-Module` instead.

## Prerequisites

- PowerShell 7+
- `Microsoft.PowerShell.PSResourceGet`
- GitHub PAT with:
  - `read:packages`
  - `repo` (if repository/package visibility requires it)

## One-command install (helper script)

```powershell
$env:LIGARE_GITHUB_PAT_TOKEN = "<YOUR_PAT>"

pwsh -NoProfile -File ./tools/publishing/Install-PreviewModule.ps1 `
  -Owner "Ligare-Method" `
  -Repository "alloyed-devops-multitool" `
  -Version "0.3.0-preview1" `
  -GitHubUser "DMykhalchenko"
```

Then import:

```powershell
Import-Module Alloyed.DevOps.Multitool -Force
Get-Command Invoke-AlloyedScript
```

## Manual install (without helper script)

```powershell
$token = "<YOUR_PAT>"
$sec = ConvertTo-SecureString $token -AsPlainText -Force
$cred = [pscredential]::new("DMykhalchenko", $sec)

Register-PSResourceRepository `
  -Name LigareGithub `
  -Uri "https://nuget.pkg.github.com/Ligare-Method/index.json" `
  -Trusted `
  -Credential $cred

Install-PSResource Alloyed.DevOps.Multitool `
  -Repository LigareGithub `
  -Version "0.3.0-preview1" `
  -Credential $cred `
  -TrustRepository
```
