namespace Alloyed.DevOps.Multitool.Core.Catalog.Models;

public sealed record ResolutionResult(
    IReadOnlyDictionary<string, string> Replacements,
    IReadOnlyList<string> MissingCommands,
    IReadOnlyList<string> RequiredModules
);
