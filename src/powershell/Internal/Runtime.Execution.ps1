function Initialize-AlloyedDecorationPipeline {
    if ($null -ne $script:DecorationPipeline) { return }

    Initialize-AlloyedHostAssembly

    $nullSink = [Alloyed.DevOps.Multitool.Core.Decoration.Services.NullDecorationSink]::new()
    $reporter = Get-AlloyedConsoleReporter
    $consoleSink = [Alloyed.DevOps.Multitool.Host.PowerShell.Services.ReporterDecorationSink]::new($reporter)

    $decorators = [System.Collections.Generic.List[Alloyed.DevOps.Multitool.Core.Decoration.Contracts.IDecorator]]::new()
    $decorators.Add([Alloyed.DevOps.Multitool.Core.Decoration.Decorators.ErrorHandlingDecorator]::new())
    $decorators.Add([Alloyed.DevOps.Multitool.Core.Decoration.Decorators.CorrelationDecorator]::new())
    $decorators.Add([Alloyed.DevOps.Multitool.Core.Decoration.Decorators.ObservabilityDecorator]::new($nullSink))
    $decorators.Add([Alloyed.DevOps.Multitool.Core.Decoration.Decorators.TransparencyDecorator]::new($consoleSink))

    $script:DecorationPipeline = [Alloyed.DevOps.Multitool.Core.Decoration.Services.DecorationPipeline]::new($decorators)
}

function Get-AlloyedConsoleReporter {
    Initialize-AlloyedHostAssembly

    $isInteractive = -not [System.Console]::IsOutputRedirected
    $mode = Resolve-AlloyedConsoleOutputMode
    return [Alloyed.DevOps.Multitool.Host.PowerShell.Services.ConsoleReporterFactory]::Create($mode, $isInteractive, $null)
}

function Write-AlloyedRuntimePreviewMessage {
    param(
        [Parameter(Mandatory)] [ValidateSet('Info','Warning','Error')] [string]$Level,
        [Parameter(Mandatory)] [string]$Message
    )

    $reporter = Get-AlloyedConsoleReporter
    $reporter.WriteMessage(
        [Alloyed.DevOps.Multitool.Host.PowerShell.Contracts.ConsoleMessageLevel]::$Level,
        $Message)
}

function Invoke-AlloyedCommandRuntime {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$Operation,
        [Parameter(Mandatory)] [scriptblock]$Action,
        [Parameter()] [object[]]$Arguments = @(),
        [Parameter()] [object[]]$InputObjects = @()
    )

    $policy = Get-AlloyedRuntimeExecutionPolicy
    $attempt = 0
    $delaySec = [int]$policy.RetryDelaySec

    while ($true) {
        $attempt++
        $sw = [System.Diagnostics.Stopwatch]::StartNew()

        try {
            if ($policy.Preview) {
                Write-AlloyedRuntimePreviewMessage -Level Info -Message ("runtime-preview phase=attempt op={0} attempt={1}" -f $Operation, $attempt)
            }

            $output = $null
            if ($policy.TimeoutSec -gt 0 -and $InputObjects.Count -eq 0) {
                $job = Start-Job -ScriptBlock {
                    param([scriptblock]$InnerAction, [object[]]$InnerArgs)
                    & $InnerAction @InnerArgs
                } -ArgumentList $Action, $Arguments

                $completed = Wait-Job -Job $job -Timeout $policy.TimeoutSec
                if (-not $completed) {
                    Stop-Job -Job $job -ErrorAction SilentlyContinue
                    Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
                    throw "Operation '$Operation' exceeded timeout of $($policy.TimeoutSec) seconds."
                }

                try {
                    $output = Receive-Job -Job $job -ErrorAction Stop
                } finally {
                    Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
                }
            } else {
                if ($policy.TimeoutSec -gt 0 -and $InputObjects.Count -gt 0 -and $policy.Preview) {
                    Write-AlloyedRuntimePreviewMessage -Level Warning -Message ("runtime-preview phase=timeout-fallback op={0} reason=input-pipeline" -f $Operation)
                }

                $output = if ($InputObjects.Count -gt 0) {
                    $InputObjects | & $Action @Arguments
                } else {
                    & $Action @Arguments
                }
            }

            $sw.Stop()
            $script:LastRuntimeExecution = [pscustomobject]@{
                Operation = $Operation
                Success = $true
                Attempts = $attempt
                DurationMs = $sw.ElapsedMilliseconds
                TimeoutSec = [int]$policy.TimeoutSec
                MaxRetries = [int]$policy.MaxRetries
            }

            return $output
        } catch {
            $sw.Stop()
            $canRetry = $attempt -le [int]$policy.MaxRetries

            if (-not $canRetry) {
                $script:LastRuntimeExecution = [pscustomobject]@{
                    Operation = $Operation
                    Success = $false
                    Attempts = $attempt
                    DurationMs = $sw.ElapsedMilliseconds
                    TimeoutSec = [int]$policy.TimeoutSec
                    MaxRetries = [int]$policy.MaxRetries
                    Error = $_.Exception.Message
                }

                throw
            }

            if ($delaySec -gt 0) {
                Start-Sleep -Seconds $delaySec
            }

            if ($policy.ExponentialBackoff) {
                $delaySec = [Math]::Min($delaySec * 2, 300)
            }
        }
    }
}

function Write-AlloyedPipelineResultSummary {
    param(
        [Parameter(Mandatory)] [object]$Result,
        [Parameter(Mandatory)] [string]$Operation
    )

    $reporter = Get-AlloyedConsoleReporter
    [Alloyed.DevOps.Multitool.Host.PowerShell.Services.PipelineResultConsolePresenter]::WriteSummary($reporter, $Result, $Operation)
}

function Invoke-AlloyedDecoratedCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$Operation,
        [Parameter(Mandatory)] [scriptblock]$Action,
        [hashtable]$Parameters = @{},
        [object[]]$Arguments = @(),
        [object[]]$InputObjects = @()
    )

    Initialize-AlloyedDecorationPipeline

    $tags = [System.Collections.Generic.Dictionary[string,string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $tags['operation'] = $Operation
    $tags['enableTransparency'] = (Resolve-AlloyedTransparencyEnabled).ToString().ToLowerInvariant()
    $tags['transparencyVerbose'] = [System.Environment]::GetEnvironmentVariable('ALLOYED_TRANSPARENCY_VERBOSE')
    $tags['transparencyProfile'] = [System.Environment]::GetEnvironmentVariable('ALLOYED_TRANSPARENCY_PROFILE')

    foreach ($key in $Parameters.Keys) {
        $value = $Parameters[$key]
        if ($null -eq $value) {
            $tags[[string]$key] = '<null>'
            continue
        }

        $tags[[string]$key] = [string]$value
    }

    $context = [Alloyed.DevOps.Multitool.Core.Decoration.Models.DecorationContext]::new($Operation, $tags)
    $invoke = [System.Func[object]] {
        return Invoke-AlloyedCommandRuntime -Operation $Operation -Action $Action -Arguments $Arguments -InputObjects $InputObjects
    }
    return $script:DecorationPipeline.Execute[object]($context, $invoke)
}
