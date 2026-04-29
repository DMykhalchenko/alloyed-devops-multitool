namespace Alloyed.DevOps.Multitool.Core.Ast.Models;

/// <summary>
/// Indicates the severity level of a <see cref="ParseDiagnostic"/> produced during script analysis.
/// </summary>
public enum ParseDiagnosticSeverity
{
    /// <summary>Informational message that does not affect transformation.</summary>
    Info = 0,

    /// <summary>Potential problem that may lead to unexpected transformation output.</summary>
    Warning = 1,

    /// <summary>Critical parse failure that prevents reliable command extraction.</summary>
    Error = 2,
}
