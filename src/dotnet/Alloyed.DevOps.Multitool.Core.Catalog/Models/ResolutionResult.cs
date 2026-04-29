namespace Alloyed.DevOps.Multitool.Core.Catalog.Models;

/// <summary>
/// The immutable output produced by <see cref="Contracts.IWrapperCatalog.Resolve"/> for a set of
/// command names.
/// </summary>
/// <param name="Replacements">
/// A map of <c>originalCommand → wrapperCommand</c> for every command that was resolved.
/// Commands with no catalog entry are still present in this map but map to themselves, so callers
/// can safely pass this directly to a transformer.
/// </param>
/// <param name="MissingCommands">
/// Commands from the input set that had no wrapper entry in the catalog and therefore remain
/// unchanged in <see cref="Replacements"/>.
/// </param>
/// <param name="RequiredModules">
/// PowerShell module names that must be imported at runtime to satisfy the wrappers referenced in
/// <see cref="Replacements"/>. Empty when none of the resolved commands have catalog mappings.
/// </param>
public sealed record ResolutionResult(
    IReadOnlyDictionary<string, string> Replacements,
    IReadOnlyList<string> MissingCommands,
    IReadOnlyList<string> RequiredModules
);
