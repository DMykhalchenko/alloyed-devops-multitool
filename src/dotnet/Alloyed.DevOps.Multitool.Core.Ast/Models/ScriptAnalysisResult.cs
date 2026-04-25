namespace Alloyed.DevOps.Multitool.Core.Ast.Models;

public sealed record ScriptAnalysisResult(
    string ScriptPath,
    IReadOnlyList<CommandUsage> Commands,
    IReadOnlyList<ParseDiagnostic> Diagnostics,
    string SourceText
);
