namespace Alloyed.DevOps.Multitool.Core.Catalog.Services;

using System.Text.Json;
using Contracts;
using Models;

/// <summary>
/// An <see cref="IWrapperCatalog"/> that loads the command-to-wrapper mapping once at construction
/// time and holds it entirely in memory. The catalog can be seeded from either the embedded
/// <c>ports.catalog.json</c> resource (default) or an external JSON file whose path is supplied
/// via <see cref="InMemoryWrapperCatalog(string?)"/>.
/// </summary>
/// <remarks>
/// The JSON schema is an array of objects with <c>command</c>, <c>wrapper</c>, and an optional
/// <c>aliases</c> string array. All aliases are registered with the same wrapper as the primary
/// command name.
/// </remarks>
public sealed class InMemoryWrapperCatalog : IWrapperCatalog
{
    private const string WrapperModuleName = "Alloyed.DevOps.Multitool";
    private const string EmbeddedCatalogResourceName = "Alloyed.DevOps.Multitool.Core.Catalog.Resources.ports.catalog.json";
    private static readonly JsonSerializerOptions CatalogJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IReadOnlyDictionary<string, string> wrapperMap;

    /// <summary>
    /// Initializes the catalog from the embedded <c>ports.catalog.json</c> resource.
    /// </summary>
    public InMemoryWrapperCatalog()
        : this(catalogSourcePath: null)
    {
    }

    /// <summary>
    /// Initializes the catalog from <paramref name="catalogSourcePath"/> when it is non-empty,
    /// or falls back to the embedded resource when it is <see langword="null"/> or whitespace.
    /// </summary>
    /// <param name="catalogSourcePath">
    /// Absolute path to an external <c>ports.catalog.json</c> file, or <see langword="null"/> to
    /// use the embedded catalog.
    /// </param>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the JSON resource or file cannot be found, is empty, or contains invalid entries.
    /// </exception>
    public InMemoryWrapperCatalog(string? catalogSourcePath)
    {
        wrapperMap = string.IsNullOrWhiteSpace(catalogSourcePath)
            ? LoadMappingsFromEmbeddedCatalog()
            : LoadMappingsFromFile(catalogSourcePath);
    }

    /// <inheritdoc/>
    public bool HasWrapper(string commandName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);
        return wrapperMap.ContainsKey(commandName);
    }

    /// <inheritdoc/>
    public string GetWrapperName(string commandName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);

        if (!wrapperMap.TryGetValue(commandName, out var wrapperName))
        {
            throw new KeyNotFoundException($"Wrapper mapping was not found for command '{commandName}'.");
        }

        return wrapperName;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> GetMappings()
    {
        return wrapperMap
            .OrderBy(static kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static kv => kv.Key, static kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Loads catalog mappings from the embedded assembly resource.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown when the embedded resource is missing.</exception>
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

    /// <summary>
    /// Loads catalog mappings from a JSON file at <paramref name="catalogSourcePath"/>.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown when the file does not exist.</exception>
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

    /// <summary>
    /// Deserializes <paramref name="stream"/> as a <c>PortCatalogEntry</c> array and constructs
    /// the case-insensitive command-to-wrapper dictionary, including all aliases.
    /// </summary>
    /// <param name="stream">Readable JSON stream.</param>
    /// <param name="sourceDescription">Human-readable label used in error messages.</param>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the JSON is malformed, the array is empty, or any entry has a blank command or wrapper.
    /// </exception>
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

    /// <summary>
    /// Internal DTO that mirrors one element of the catalog JSON array.
    /// </summary>
    private sealed class PortCatalogEntry
    {
        public string Command { get; init; } = string.Empty;

        public string Wrapper { get; init; } = string.Empty;

        public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();
    }
}
