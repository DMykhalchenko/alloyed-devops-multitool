namespace Alloyed.DevOps.Multitool.Host.PowerShell.Models;

/// <summary>
/// Severity level of a <see cref="PipelineDiagnostic"/> produced during pipeline execution.
/// Also used by <see cref="PipelineRequest.FailOnSeverity"/> to define an early-exit threshold.
/// </summary>
public enum PipelineDiagnosticSeverity
{
    /// <summary>Informational message that does not affect the pipeline outcome.</summary>
    Info = 0,

    /// <summary>A potential problem that allows the pipeline to continue.</summary>
    Warning = 1,

    /// <summary>A critical failure that stops the pipeline.</summary>
    Error = 2,
}
