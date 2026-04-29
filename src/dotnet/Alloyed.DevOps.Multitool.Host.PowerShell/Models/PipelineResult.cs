namespace Alloyed.DevOps.Multitool.Host.PowerShell.Models;

/// <summary>
/// The immutable result returned by <see cref="Contracts.ITransformationPipeline.Execute"/> after
/// a pipeline run, regardless of success or failure.
/// </summary>
/// <param name="Success"><see langword="true"/> when the module was produced without fatal errors.</param>
/// <param name="ModulePath">
/// Absolute path to the generated module directory. Empty string when
/// <paramref name="Success"/> is <see langword="false"/>.
/// </param>
/// <param name="CommandsFound">Total number of command invocations detected in the script.</param>
/// <param name="CommandsReplaced">
/// Number of commands actually substituted with a wrapper (i.e. entries in the resolution map
/// where key differs from value).
/// </param>
/// <param name="MissingCommands">
/// Commands that were detected in the script but had no catalog entry and therefore remain
/// unchanged in the output.
/// </param>
/// <param name="Diagnostics">
/// All diagnostic messages collected during the run, from any pipeline stage.
/// </param>
/// <param name="ErrorMessage">
/// Top-level human-readable failure reason, or <see langword="null"/> when
/// <paramref name="Success"/> is <see langword="true"/>.
/// </param>
public sealed record PipelineResult(
    bool Success,
    string ModulePath,
    int CommandsFound,
    int CommandsReplaced,
    IReadOnlyList<string> MissingCommands,
    IReadOnlyList<PipelineDiagnostic> Diagnostics,
    string? ErrorMessage
);
