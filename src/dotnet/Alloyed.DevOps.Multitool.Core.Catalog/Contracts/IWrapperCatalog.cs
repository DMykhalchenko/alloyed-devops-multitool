namespace Alloyed.DevOps.Multitool.Core.Catalog.Contracts;

using Alloyed.DevOps.Multitool.Core.Catalog.Models;

/// <summary>
/// Provides a lookup table that maps native PowerShell commands to their Alloyed wrapper equivalents
/// and determines which PowerShell modules must be imported to use those wrappers.
/// </summary>
public interface IWrapperCatalog
{
    /// <summary>
    /// Returns <see langword="true"/> when the catalog contains a wrapper mapping for
    /// <paramref name="commandName"/>.
    /// </summary>
    /// <param name="commandName">Command name to look up (case-insensitive).</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="commandName"/> is null or whitespace.</exception>
    bool HasWrapper(string commandName);

    /// <summary>
    /// Returns the wrapper name for <paramref name="commandName"/>.
    /// </summary>
    /// <param name="commandName">Command name to look up (case-insensitive).</param>
    /// <returns>The corresponding wrapper command name.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="commandName"/> is null or whitespace.</exception>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">Thrown when no wrapper exists for <paramref name="commandName"/>.</exception>
    string GetWrapperName(string commandName);

    /// <summary>
    /// Resolves a collection of command names against the catalog and returns a
    /// <see cref="ResolutionResult"/> containing the replacement map, any commands that had no
    /// wrapper entry, and the list of modules that must be imported.
    /// </summary>
    /// <param name="commands">Command names to resolve. Nulls, duplicates, and whitespace-only entries are ignored.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="commands"/> is <see langword="null"/>.</exception>
    ResolutionResult Resolve(IEnumerable<string> commands);

    /// <summary>
    /// Returns the set of PowerShell module names that must be imported when any of the supplied
    /// <paramref name="commands"/> has a catalog mapping. Returns an empty list when none of the
    /// commands are covered.
    /// </summary>
    /// <param name="commands">Command names to evaluate.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="commands"/> is <see langword="null"/>.</exception>
    IReadOnlyList<string> GetRequiredModules(IEnumerable<string> commands);

    /// <summary>
    /// Returns the full command-to-wrapper mapping stored in the catalog, sorted alphabetically by
    /// command name.
    /// </summary>
    IReadOnlyDictionary<string, string> GetMappings();
}
