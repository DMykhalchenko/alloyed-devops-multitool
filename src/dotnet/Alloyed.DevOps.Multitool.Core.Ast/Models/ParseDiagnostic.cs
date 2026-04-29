namespace Alloyed.DevOps.Multitool.Core.Ast.Models;

/// <summary>
/// Represents a single diagnostic message produced while parsing a PowerShell script.
/// </summary>
/// <param name="Message">Human-readable description of the diagnostic.</param>
/// <param name="Line">One-based line number where the issue was detected.</param>
/// <param name="Column">One-based column number where the issue was detected.</param>
/// <param name="Severity">Severity level of the diagnostic.</param>
public sealed record ParseDiagnostic(
    string Message,
    int Line,
    int Column,
    ParseDiagnosticSeverity Severity
);
