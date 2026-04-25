namespace Alloyed.DevOps.Multitool.Core.Builders.Models;

public sealed record ModuleBuildResult(
    bool Success,
    string ModulePath,
    IReadOnlyList<string> Files,
    string? ErrorMessage
);
