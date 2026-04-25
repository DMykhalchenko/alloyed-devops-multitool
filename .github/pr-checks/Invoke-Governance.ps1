[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ConfigPath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$EventPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ApiHeader {
    if ([string]::IsNullOrWhiteSpace($env:GITHUB_TOKEN)) {
        return $null
    }
    return @{
        Authorization        = "Bearer $($env:GITHUB_TOKEN)"
        Accept               = 'application/vnd.github+json'
        'X-GitHub-Api-Version' = '2022-11-28'
    }
}

function Get-PullRequestCommitMessage {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommitsUrl,

        [Parameter(Mandatory = $true)]
        [hashtable]$Headers
    )

    try {
        $commits = @(Invoke-RestMethod -Method Get -Uri "${CommitsUrl}?per_page=100" -Headers $Headers)
        return @($commits | ForEach-Object { [string]$_.commit.message })
    }
    catch {
        Write-Warning "Could not fetch commit messages: $($_.Exception.Message)"
        return @()
    }
}

function Publish-GovernanceComment {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Body,

        [Parameter(Mandatory = $true)]
        [int]$IssueNumber,

        [Parameter(Mandatory = $true)]
        [hashtable]$Headers
    )

    if ([string]::IsNullOrWhiteSpace($env:GITHUB_API_URL) -or [string]::IsNullOrWhiteSpace($env:GITHUB_REPOSITORY)) {
        return
    }

    $owner, $repo = $env:GITHUB_REPOSITORY -split '/', 2
    $commentsUri = "$($env:GITHUB_API_URL)/repos/$owner/$repo/issues/$IssueNumber/comments"
    $payloadJson = @{ body = $Body } | ConvertTo-Json -Compress
    $marker = '<!-- pr-governance -->'

    try {
        $comments = @(Invoke-RestMethod -Method Get -Uri $commentsUri -Headers $Headers)
        $existing = $comments | Where-Object { $_.body -like "*$marker*" } | Select-Object -First 1

        if ($null -ne $existing) {
            $commentUri = "$($env:GITHUB_API_URL)/repos/$owner/$repo/issues/comments/$($existing.id)"
            Invoke-RestMethod -Method Patch -Uri $commentUri -Headers $Headers -Body $payloadJson -ContentType 'application/json' | Out-Null
            Write-Output "Updated governance comment $($existing.id)."
        }
        else {
            Invoke-RestMethod -Method Post -Uri $commentsUri -Headers $Headers -Body $payloadJson -ContentType 'application/json' | Out-Null
            Write-Output 'Created governance comment.'
        }
    }
    catch {
        Write-Warning "Could not publish governance comment: $($_.Exception.Message)"
    }
}

function Format-GovernanceFailureComment {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Detail
    )

    return @(
        '<!-- pr-governance -->'
        '## PR governance check failed'
        ''
        $Detail
        ''
        '> Push a new commit or edit the PR title — the check will re-run automatically.'
    ) -join "`n"
}

$config = Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
$eventPayload = Get-Content -LiteralPath $EventPath -Raw | ConvertFrom-Json
$pullRequest = $eventPayload.pull_request

if ($null -eq $pullRequest) {
    throw 'pull_request payload is required.'
}

$title = [string]$pullRequest.title
$body = [string]$pullRequest.body
$isDraft = [bool]$pullRequest.draft
$action = [string]$eventPayload.action

if ($config.options.skipValidationOnDraft -and $isDraft) {
    Write-Output "PR is draft. Governance validation skipped on action '$action'."
    return
}

if ([string]::IsNullOrWhiteSpace($title)) {
    throw 'PR title is required.'
}

$trimmedTitle = $title.Trim()
$titleLength = $trimmedTitle.Length

if ($titleLength -lt [int]$config.title.minLength) {
    throw "PR title is too short. Minimum length: $($config.title.minLength)."
}

if ($titleLength -gt [int]$config.title.maxLength) {
    throw "PR title is too long. Maximum length: $($config.title.maxLength)."
}

if ($config.title.pattern -and $trimmedTitle -notmatch [string]$config.title.pattern) {
    throw "PR title does not match the required pattern: $($config.title.description)"
}

foreach ($prefix in @($config.title.disallowPrefixes)) {
    if ([string]::IsNullOrWhiteSpace($prefix)) {
        continue
    }

    if ($trimmedTitle.StartsWith([string]$prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "PR title must not start with '$prefix'."
    }
}

$jiraMode = [string]$config.jira.mode
$jiraPattern = [string]$config.jira.pattern
$prNumber = [int]$pullRequest.number
$headers = Get-ApiHeader

switch ($jiraMode) {
    'title' {
        if ($trimmedTitle -notmatch $jiraPattern) {
            if ($null -ne $headers -and $prNumber -gt 0) {
                $detail = @"
A Jira key (e.g. ``PROJ-123``) was not found in the PR title.

Add a Jira key to the title, for example: ``PROJ-123 My feature description``
"@
                Publish-GovernanceComment -Body (Format-GovernanceFailureComment -Detail $detail) -IssueNumber $prNumber -Headers $headers
            }
            throw 'A Jira key is required in the PR title.'
        }
    }
    'titleOrBody' {
        $titleHasJira = $trimmedTitle -match $jiraPattern
        $bodyHasJira = -not [string]::IsNullOrWhiteSpace($body) -and $body -match $jiraPattern
        if (-not ($titleHasJira -or $bodyHasJira)) {
            if ($null -ne $headers -and $prNumber -gt 0) {
                $detail = @"
A Jira key (e.g. ``PROJ-123``) was not found in the PR title or description.

Add a Jira key to the PR title or body.
"@
                Publish-GovernanceComment -Body (Format-GovernanceFailureComment -Detail $detail) -IssueNumber $prNumber -Headers $headers
            }
            throw 'A Jira key is required in the PR title or PR body.'
        }
    }
    'anyOf' {
        $titleHasJira = $trimmedTitle -match $jiraPattern

        $branchName = [string]$pullRequest.head.ref
        $branchHasJira = -not [string]::IsNullOrWhiteSpace($branchName) -and $branchName -match $jiraPattern

        $commitMessages = @()
        $commitsUrl = [string]$pullRequest.commits_url
        if ($null -ne $headers -and -not [string]::IsNullOrWhiteSpace($commitsUrl)) {
            $commitMessages = Get-PullRequestCommitMessage -CommitsUrl $commitsUrl -Headers $headers
        }
        $commitsHaveJira = [bool]@($commitMessages | Where-Object { $_ -match $jiraPattern }).Count

        Write-Output ("Jira key check: title={0}, branch={1}, commits={2}" -f $titleHasJira, $branchHasJira, $commitsHaveJira)

        if (-not ($titleHasJira -or $branchHasJira -or $commitsHaveJira)) {
            if ($null -ne $headers -and $prNumber -gt 0) {
                $detail = @"
A Jira key (e.g. ``PROJ-123``) was not found in any of the expected locations:

| Location | Example |
| --- | --- |
| PR title | ``PROJ-123 My feature description`` |
| Branch name | ``feature/PROJ-123-my-feature`` |
| Commit message | ``PROJ-123 Implement the feature`` |

Update at least one of the above and the check will re-run automatically.
"@
                Publish-GovernanceComment -Body (Format-GovernanceFailureComment -Detail $detail) -IssueNumber $prNumber -Headers $headers
            }
            throw 'A Jira key is required in the PR title, branch name, or at least one commit message.'
        }
    }
    'optional' {
        $titleHasJira = $trimmedTitle -match $jiraPattern
        $bodyHasJira = -not [string]::IsNullOrWhiteSpace($body) -and $body -match $jiraPattern
        if (-not ($titleHasJira -or $bodyHasJira)) {
            Write-Warning 'No Jira key found in PR title or body.'
        }
    }
    'disabled' {
    }
    default {
        throw "Unsupported jira.mode value '$jiraMode'."
    }
}

function Remove-StaleGovernanceComment {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Headers,

        [Parameter(Mandatory = $true)]
        [int]$IssueNumber
    )

    $repository = [string]$env:GITHUB_REPOSITORY
    if ([string]::IsNullOrWhiteSpace($repository)) {
        return
    }

    $apiUrl = [string]$env:GITHUB_API_URL
    if ([string]::IsNullOrWhiteSpace($apiUrl)) {
        $apiUrl = 'https://api.github.com'
    }

    $commentsUrl = '{0}/repos/{1}/issues/{2}/comments' -f $apiUrl.TrimEnd('/'), $repository, $IssueNumber

    try {
        $comments = Invoke-RestMethod -Uri $commentsUrl -Headers $Headers -Method Get
    }
    catch {
        Write-Warning "Unable to retrieve existing governance comments: $($_.Exception.Message)"
        return
    }

    foreach ($comment in @($comments)) {
        $body = [string]$comment.body
        if (-not [string]::IsNullOrWhiteSpace($body) -and $body -like '*<!-- pr-governance -->*') {
            $commentUrl = [string]$comment.url
            if (-not [string]::IsNullOrWhiteSpace($commentUrl)) {
                try {
                    if ($PSCmdlet.ShouldProcess($commentUrl, 'Delete stale governance comment')) {
                        Invoke-RestMethod -Uri $commentUrl -Headers $Headers -Method Delete | Out-Null
                    }
                }
                catch {
                    Write-Warning "Unable to delete stale governance comment: $($_.Exception.Message)"
                }
            }
        }
    }
}

if ($null -ne $headers -and $prNumber -gt 0) {
    Remove-StaleGovernanceComment -Headers $headers -IssueNumber $prNumber
}
Write-Output "PR governance checks passed for action '$action'."
