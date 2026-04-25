namespace Alloyed.DevOps.Multitool.Core.Catalog.Services;

using Alloyed.DevOps.Multitool.Core.Catalog.Contracts;
using Alloyed.DevOps.Multitool.Core.Catalog.Models;

public sealed class InMemoryWrapperCatalog : IWrapperCatalog
{
    private const string WrapperModuleName = "Alloyed.DevOps.Multitool";

    private static readonly IReadOnlyDictionary<string, string> WrapperMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Get-ChildItem"] = "Get-AlloyedChildItem",
            ["Get-Item"] = "Get-AlloyedItem",
            ["Test-Path"] = "Test-AlloyedPath",
            ["gci"] = "Get-AlloyedChildItem",
            ["gi"] = "Get-AlloyedItem",
            ["tp"] = "Test-AlloyedPath",
        };

    public bool HasWrapper(string commandName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);
        return WrapperMap.ContainsKey(commandName);
    }

    public string GetWrapperName(string commandName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);

        if (!WrapperMap.TryGetValue(commandName, out var wrapperName))
        {
            throw new KeyNotFoundException($"Wrapper mapping was not found for command '{commandName}'.");
        }

        return wrapperName;
    }

    public ResolutionResult Resolve(IEnumerable<string> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        var normalized = commands
            .Where(static c => !string.IsNullOrWhiteSpace(c))
            .Select(static c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var missing = new List<string>();

        foreach (var command in normalized)
        {
            if (WrapperMap.TryGetValue(command, out var wrapper))
            {
                replacements[command] = wrapper;
                continue;
            }

            replacements[command] = command;
            missing.Add(command);
        }

        var requiredModules = GetRequiredModules(normalized);

        return new ResolutionResult(
            Replacements: replacements,
            MissingCommands: missing,
            RequiredModules: requiredModules);
    }

    public IReadOnlyList<string> GetRequiredModules(IEnumerable<string> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        var hasMappedCommands = commands.Any(static c =>
            !string.IsNullOrWhiteSpace(c) && WrapperMap.ContainsKey(c.Trim()));

        if (!hasMappedCommands)
        {
            return Array.Empty<string>();
        }

        return new[] { WrapperModuleName };
    }

    public IReadOnlyDictionary<string, string> GetMappings()
    {
        return WrapperMap
            .OrderBy(static kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static kv => kv.Key, static kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    }
}
