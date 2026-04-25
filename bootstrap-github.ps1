[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory)]
    [string]$Owner,
    [string]$RepoName = "alloyed-devops-multitool",
    [ValidateSet("public", "private")]
    [string]$Visibility = "private",
    [string]$DefaultBranch = "main",
    [switch]$CreateRemote,
    [switch]$Push
)

$ErrorActionPreference = "Stop"

function Assert-Command {
    param([Parameter(Mandatory)] [string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found: $Name"
    }
}

Assert-Command -Name "git"
if ($CreateRemote) {
    Assert-Command -Name "gh"
}

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectRoot

$gitDir = Join-Path $projectRoot ".git"
if (-not (Test-Path $gitDir)) {
    if ($PSCmdlet.ShouldProcess($projectRoot, "Initialize git repository")) {
        & git init -b $DefaultBranch | Out-Null
    }
}

$hasCommit = $false
try {
    & git rev-parse --verify HEAD 2>$null | Out-Null
    $hasCommit = ($LASTEXITCODE -eq 0)
}
catch {
    $hasCommit = $false
}

if (-not $hasCommit) {
    if ($PSCmdlet.ShouldProcess($projectRoot, "Create initial commit")) {
        & git add .
        & git commit -m "chore: bootstrap alloyed-devops-multitool repository"
    }
}

$remoteUrl = "git@github.com:$Owner/$RepoName.git"
$originExists = $false
try {
    & git remote get-url origin 2>$null | Out-Null
    $originExists = ($LASTEXITCODE -eq 0)
}
catch {
    $originExists = $false
}

if ($CreateRemote) {
    if ($PSCmdlet.ShouldProcess("$Owner/$RepoName", "Create GitHub repository")) {
        & gh repo create "$Owner/$RepoName" --$Visibility --source . --remote origin --description "Alloyed DevOps Multitool" --disable-issues
    }
}
elseif (-not $originExists) {
    if ($PSCmdlet.ShouldProcess("origin", "Add git remote")) {
        & git remote add origin $remoteUrl
    }
}

if ($Push) {
    if ($PSCmdlet.ShouldProcess("origin/$DefaultBranch", "Push branch")) {
        & git push -u origin $DefaultBranch
    }
}

Write-Host "Repository bootstrap complete."
Write-Host "Local path: $projectRoot"
Write-Host "Remote: $remoteUrl"
