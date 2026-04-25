namespace Alloyed.DevOps.Multitool.Core.Builders.Models;

public sealed record ModuleBuildRequest(
    string ModuleName,
    string OutputPath,
    string TransformedScript,
    IReadOnlyList<string> RequiredModules,
    string Author,
    string Description
);
