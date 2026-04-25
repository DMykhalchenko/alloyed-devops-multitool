namespace Alloyed.DevOps.Multitool.Host.PowerShell.Models;

public sealed record PipelineDiagnostic(
    string Code,
    string Source,
    string Message,
    int Line,
    int Column,
    PipelineDiagnosticSeverity Severity
);
