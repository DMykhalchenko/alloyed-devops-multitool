namespace Alloyed.DevOps.Multitool.Core.Ast.Models;

public sealed record ParseDiagnostic(
    string Message,
    int Line,
    int Column,
    ParseDiagnosticSeverity Severity
);
