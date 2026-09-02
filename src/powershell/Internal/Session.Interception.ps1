function Get-AlloyedPortsCatalogPath {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot 'tools/ports/ports.catalog.json'
}

function Get-AlloyedNativeCommandMap {
    $catalogPath = Get-AlloyedPortsCatalogPath
    $nativeMap = @{}

    if (-not (Test-Path -LiteralPath $catalogPath)) {
        return $nativeMap
    }

    $entries = @(Get-Content -Raw -LiteralPath $catalogPath | ConvertFrom-Json)
    foreach ($entry in $entries) {
        if ([string]::IsNullOrWhiteSpace($entry.command) -or [string]::IsNullOrWhiteSpace($entry.native)) {
            continue
        }

        $nativeMap[[string]$entry.command] = [string]$entry.native
        foreach ($alias in @($entry.aliases)) {
            if (-not [string]::IsNullOrWhiteSpace($alias)) {
                $nativeMap[[string]$alias] = [string]$entry.native
            }
        }
    }

    return $nativeMap
}

function Initialize-AlloyedWrappersFromCatalog {
    [CmdletBinding()]
    param()

    $catalogPath = Get-AlloyedPortsCatalogPath
    if (-not (Test-Path -LiteralPath $catalogPath)) {
        return
    }

    $entries = @(Get-Content -Raw -LiteralPath $catalogPath | ConvertFrom-Json)
    foreach ($entry in $entries) {
        $wrapperName = [string]$entry.wrapper
        $operation = [string]$entry.command
        $native = [string]$entry.native

        if ([string]::IsNullOrWhiteSpace($wrapperName) -or [string]::IsNullOrWhiteSpace($operation) -or [string]::IsNullOrWhiteSpace($native)) {
            continue
        }

        if (Get-Command -Name $wrapperName -ErrorAction SilentlyContinue) {
            continue
        }

        Set-AlloyedCommandProxyFunction -Name $wrapperName -Operation $operation -NativeCommand $native
    }
}

function Set-AlloyedCommandProxyFunction {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [string]$Operation,
        [Parameter(Mandatory)] [string]$NativeCommand
    )

    $operationCopy = $Operation
    $nativeCopy = $NativeCommand
    $isClearHost = $Operation -eq 'Clear-Host'
    $decoratedInvoker = ${function:Invoke-AlloyedDecoratedCommand}

    $proxy = {
        # Reassign closure-captured variables to genuinely local ones before nesting another
        # GetNewClosure(): a nested GetNewClosure() only snapshots the local scope, so it would
        # otherwise silently drop variables that only exist via this scriptblock's own closure.
        $localNative = $nativeCopy
        if ($isClearHost) {
            $action = { & $localNative }.GetNewClosure()
            & $decoratedInvoker -Operation $operationCopy -Parameters @{} -Action $action
        } else {
            $capturedInput = @($input)
            # Piping an empty array still suppresses the native cmdlet's ProcessRecord (it runs
            # zero times), so only pipe when there is real pipeline input to forward.
            $action = {
                end {
                    if ($capturedInput.Count -gt 0) {
                        $capturedInput | & $localNative @args
                    } else {
                        & $localNative @args
                    }
                }
            }.GetNewClosure()
            & $decoratedInvoker -Operation $operationCopy -Arguments $args -InputObjects $capturedInput -Action $action
        }
    }.GetNewClosure()

    Set-Item -LiteralPath ("Function:global:{0}" -f $Name) -Value $proxy -Force
}

function Enable-AlloyedSessionMode {
    [CmdletBinding()]
    param(
        [Parameter()] [switch]$Force
    )

    Initialize-AlloyedHostAssembly
    Initialize-AlloyedWrappersFromCatalog

    if ($script:SessionModeEnabled -and -not $Force.IsPresent) {
        return Get-AlloyedSessionModeStatus
    }

    $catalog = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.PipelineBootstrap]::CreateCatalog()
    $mappings = $catalog.GetMappings()
    $nativeMap = Get-AlloyedNativeCommandMap
    $protectedCommands = @(
        'Write-Host',
        'Write-Progress',
        'Read-Host',
        'Get-Command',
        'Set-Alias',
        'Remove-Item',
        'Set-Item'
    )

    $applied = New-Object System.Collections.Generic.List[string]
    $skipped = New-Object System.Collections.Generic.List[string]
    $script:SessionModeAliasBackup = @{}
    $script:SessionModeCommandBackup = @{}

    foreach ($entry in ($mappings.GetEnumerator() | Sort-Object Key)) {
        $name = $entry.Key
        if ($protectedCommands -contains $name) {
            $skipped.Add($name)
            continue
        }

        $sourceCommand = Get-Command -Name $name -ErrorAction SilentlyContinue
        if (-not $sourceCommand) {
            $skipped.Add($name)
            continue
        }

        if ($sourceCommand.CommandType -eq 'Function' -and $sourceCommand.Source -eq 'Alloyed.DevOps.Multitool') {
            $skipped.Add($name)
            continue
        }

        $nativeCommand = $null
        if ($nativeMap.ContainsKey($name)) {
            $nativeCommand = [string]$nativeMap[$name]
        } elseif ($sourceCommand.Source) {
            $nativeCommand = "{0}\{1}" -f $sourceCommand.Source, $sourceCommand.Name
        } else {
            $nativeCommand = [string]$sourceCommand.Name
        }

        try {
            $existingFunction = Get-Command -Name $name -CommandType Function -ErrorAction SilentlyContinue
            $existingAlias = Get-Alias -Name $name -ErrorAction SilentlyContinue

            $script:SessionModeCommandBackup[$name] = [pscustomobject]@{
                AliasDefinition = if ($existingAlias) { $existingAlias.Definition } else { $null }
                AliasOptions = if ($existingAlias) { $existingAlias.Options } else { $null }
                FunctionDefinition = if ($existingFunction) { (Get-Content -LiteralPath ("function:{0}" -f $name) -ErrorAction SilentlyContinue) } else { $null }
            }

            if ($existingAlias) {
                Remove-Item -LiteralPath ("Alias:{0}" -f $name) -Force -ErrorAction SilentlyContinue
            }

            Set-AlloyedCommandProxyFunction -Name $name -Operation $name -NativeCommand $nativeCommand

            if ($existingAlias) {
                $script:SessionModeAliasBackup[$name] = $existingAlias.Definition
            } else {
                $script:SessionModeAliasBackup[$name] = $null
            }

            $applied.Add($name)
        } catch {
            if ($script:SessionModeCommandBackup.ContainsKey($name)) {
                $script:SessionModeCommandBackup.Remove($name)
            }
            $skipped.Add($name)
            continue
        }
    }

    $script:SessionModeAliases = @($applied.ToArray())
    $script:SessionModeEnabled = $true

    [pscustomobject]@{
        Enabled = $true
        AppliedAliases = @($applied.ToArray())
        SkippedCommands = @($skipped.ToArray())
    }
}

function Disable-AlloyedSessionMode {
    [CmdletBinding()]
    param()

    if (-not $script:SessionModeEnabled) {
        return Get-AlloyedSessionModeStatus
    }

    foreach ($name in @($script:SessionModeAliases)) {
        Remove-Item -LiteralPath ("Function:{0}" -f $name) -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath ("Alias:{0}" -f $name) -Force -ErrorAction SilentlyContinue

        $prior = $script:SessionModeCommandBackup[$name]
        if ($null -eq $prior) {
            continue
        }

        if (-not [string]::IsNullOrWhiteSpace($prior.FunctionDefinition)) {
            Set-Item -LiteralPath ("Function:global:{0}" -f $name) -Value $prior.FunctionDefinition -Force
        }

        if (-not [string]::IsNullOrWhiteSpace($prior.AliasDefinition)) {
            try {
                Remove-Item -LiteralPath ("Alias:{0}" -f $name) -Force -ErrorAction SilentlyContinue
                if ($null -ne $prior.AliasOptions) {
                    Set-Alias -Name $name -Value $prior.AliasDefinition -Scope Global -Option $prior.AliasOptions -Force
                } else {
                    Set-Alias -Name $name -Value $prior.AliasDefinition -Scope Global -Force
                }
            } catch {
                Write-Verbose ("Failed to restore alias '{0}': {1}" -f $name, $_.Exception.Message)
            }
        }
    }

    $script:SessionModeEnabled = $false
    $script:SessionModeAliases = @()
    $script:SessionModeAliasBackup = @{}
    $script:SessionModeCommandBackup = @{}

    Get-AlloyedSessionModeStatus
}

function Get-AlloyedSessionModeStatus {
    [CmdletBinding()]
    param()

    [pscustomobject]@{
        PSTypeName = 'Alloyed.SessionModeStatus'
        Enabled = $script:SessionModeEnabled
        ActiveAliasCount = @($script:SessionModeAliases).Count
        ActiveAliases = @($script:SessionModeAliases)
    }
}
