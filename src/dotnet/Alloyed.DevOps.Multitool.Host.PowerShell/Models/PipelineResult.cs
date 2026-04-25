namespace Alloyed.DevOps.Multitool.Host.PowerShell.Models;

public sealed record PipelineResult(
    bool Success,
    string ModulePath,
    int CommandsFound,
    int CommandsReplaced,
    IReadOnlyList<string> MissingCommands,
    IReadOnlyList<PipelineDiagnostic> Diagnostics,
    string? ErrorMessage
);
