namespace Alloyed.DevOps.Multitool.Host.PowerShell.Models;

public sealed record PipelineRequest(
    string ScriptPath,
    string ModuleName,
    string OutputPath,
    bool Force,
    PipelineDiagnosticSeverity? FailOnSeverity = null
);
