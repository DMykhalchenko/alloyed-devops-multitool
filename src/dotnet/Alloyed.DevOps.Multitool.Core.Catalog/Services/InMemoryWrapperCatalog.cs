namespace Alloyed.DevOps.Multitool.Core.Catalog.Services;

using System.Text.Json;
using Alloyed.DevOps.Multitool.Core.Catalog.Contracts;
using Alloyed.DevOps.Multitool.Core.Catalog.Models;

public sealed class InMemoryWrapperCatalog : IWrapperCatalog
{
    private const string WrapperModuleName = "Alloyed.DevOps.Multitool";
    private const string EmbeddedCatalogResourceName = "Alloyed.DevOps.Multitool.Core.Catalog.Resources.ports.catalog.json";
    private static readonly JsonSerializerOptions CatalogJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IReadOnlyDictionary<string, string> wrapperMap;

    public InMemoryWrapperCatalog()
        : this(catalogSourcePath: null)
    {
    }

    public InMemoryWrapperCatalog(string? catalogSourcePath)
    {
        wrapperMap = string.IsNullOrWhiteSpace(catalogSourcePath)
            ? LoadMappingsFromEmbeddedCatalog()
            : LoadMappingsFromFile(catalogSourcePath);
    }

    public bool HasWrapper(string commandName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);
        return wrapperMap.ContainsKey(commandName);
    }

    public string GetWrapperName(string commandName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);

        if (!wrapperMap.TryGetValue(commandName, out var wrapperName))
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
            if (wrapperMap.TryGetValue(command, out var wrapper))
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

        var hasMappedCommands = commands.Any(c =>
            !string.IsNullOrWhiteSpace(c) && wrapperMap.ContainsKey(c.Trim()));

        if (!hasMappedCommands)
        {
            return Array.Empty<string>();
        }

        return new[] { WrapperModuleName };
    }

    public IReadOnlyDictionary<string, string> GetMappings()
    {
        return wrapperMap
            .OrderBy(static kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static kv => kv.Key, static kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, string> LoadMappingsFromEmbeddedCatalog()
    {
        var assembly = typeof(InMemoryWrapperCatalog).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedCatalogResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Embedded ports catalog resource '{EmbeddedCatalogResourceName}' was not found in '{assembly.FullName}'.");
        }

        return BuildMapping(stream, $"embedded resource '{EmbeddedCatalogResourceName}'");
    }

    private static IReadOnlyDictionary<string, string> LoadMappingsFromFile(string catalogSourcePath)
    {
        if (!File.Exists(catalogSourcePath))
        {
            throw new InvalidOperationException(
                $"Ports catalog file was not found: '{catalogSourcePath}'.");
        }

        using var stream = File.OpenRead(catalogSourcePath);
        return BuildMapping(stream, $"file '{catalogSourcePath}'");
    }

    private static IReadOnlyDictionary<string, string> BuildMapping(Stream stream, string sourceDescription)
    {
        List<PortCatalogEntry>? entries;
        try
        {
            entries = JsonSerializer.Deserialize<List<PortCatalogEntry>>(stream, CatalogJsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Ports catalog from {sourceDescription} could not be parsed as JSON.", ex);
        }

        if (entries is null || entries.Count == 0)
        {
            throw new InvalidOperationException($"Ports catalog from {sourceDescription} is empty.");
        }

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Command) || string.IsNullOrWhiteSpace(entry.Wrapper))
            {
                throw new InvalidOperationException(
                    $"Ports catalog from {sourceDescription} contains an entry with empty 'command' or 'wrapper'.");
            }

            map[entry.Command] = entry.Wrapper;

            foreach (var alias in entry.Aliases)
            {
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    map[alias] = entry.Wrapper;
                }
            }
        }

        return map;
    }

    private sealed class PortCatalogEntry
    {
        public string Command { get; init; } = string.Empty;

        public string Wrapper { get; init; } = string.Empty;

        public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();
    }
}
