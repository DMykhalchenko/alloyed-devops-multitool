namespace Alloyed.DevOps.Multitool.Core.Ast.Models;

/// <summary>
/// The immutable output produced by an <see cref="Contracts.IScriptAnalyzer"/> after analyzing a single script.
/// </summary>
/// <param name="ScriptPath">
/// The path (or logical identifier) of the analyzed script, as passed to the analyzer.
/// </param>
/// <param name="Commands">
/// All command invocations detected in the script, in source order.
/// </param>
/// <param name="Diagnostics">
/// Parse warnings or errors collected during analysis. An empty list means the script was parsed cleanly.
/// </param>
/// <param name="SourceText">Original, unmodified text of the script.</param>
public sealed record ScriptAnalysisResult(
    string ScriptPath,
    IReadOnlyList<CommandUsage> Commands,
    IReadOnlyList<ParseDiagnostic> Diagnostics,
    string SourceText
);
