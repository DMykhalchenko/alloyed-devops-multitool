namespace Alloyed.DevOps.Multitool.Host.PowerShell.Models;

/// <summary>
/// Represents a single diagnostic message emitted during pipeline execution. Diagnostics
/// originate from the AST analyzer, the catalog resolver, the module builder, or the pipeline
/// orchestrator itself and are aggregated in <see cref="PipelineResult.Diagnostics"/>.
/// </summary>
/// <param name="Code">
/// A short machine-readable identifier (e.g. <c>AST-UNTERMINATED-STRING</c>,
/// <c>PIPELINE-FAIL-ON-SEVERITY</c>) that callers can use for programmatic branching.
/// </param>
/// <param name="Source">
/// Human-readable label identifying the pipeline stage that produced the diagnostic
/// (e.g. <c>ast-analyzer</c>, <c>pipeline</c>, <c>module-builder</c>).
/// </param>
/// <param name="Message">Full human-readable description of the diagnostic.</param>
/// <param name="Line">One-based source line number, or 0 when not applicable.</param>
/// <param name="Column">One-based source column number, or 0 when not applicable.</param>
/// <param name="Severity">Severity level of the diagnostic.</param>
public sealed record PipelineDiagnostic(
    string Code,
    string Source,
    string Message,
    int Line,
    int Column,
    PipelineDiagnosticSeverity Severity
);
