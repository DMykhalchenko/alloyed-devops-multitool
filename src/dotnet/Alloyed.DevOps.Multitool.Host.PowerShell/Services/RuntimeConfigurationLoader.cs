namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using System.Text.Json;
using Alloyed.DevOps.Multitool.Host.PowerShell.Models;

/// <summary>
/// Loads <see cref="RuntimeConfiguration"/> from a layered hierarchy of sources applied in
/// ascending precedence order:
/// <list type="number">
///   <item>Hard-coded defaults.</item>
///   <item>JSON file at <c>config/appsettings.json</c> relative to the base path.</item>
///   <item>YAML file at <c>config/appsettings.yml</c> (or <c>.yaml</c>) relative to the base path.</item>
///   <item>Environment variables prefixed with <c>ALLOYED__</c> or the legacy <c>TAF__</c> prefix.</item>
/// </list>
/// All sources use the colon-separated key path convention (e.g.
/// <c>Alloyed:Decoration:EnableTransparency</c>). Double-underscore (<c>__</c>) in environment
/// variable names is treated as a path separator.
/// </summary>
public sealed class RuntimeConfigurationLoader
{
    private const string JsonConfigRelativePath = "config/appsettings.json";
    private const string YamlConfigRelativePath = "config/appsettings.yml";
    private const string YamlAltConfigRelativePath = "config/appsettings.yaml";

    /// <summary>
    /// Loads and returns a <see cref="RuntimeConfiguration"/> by merging all available
    /// configuration sources.
    /// </summary>
    /// <param name="basePath">
    /// Base directory used to locate config files. Defaults to the current working directory when
    /// <see langword="null"/> or whitespace.
    /// </param>
    /// <param name="environment">
    /// Environment variable map to apply. When <see langword="null"/>, the process environment
    /// variables are read directly.
    /// </param>
    /// <returns>A fully bound <see cref="RuntimeConfiguration"/> instance.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when a config value cannot be parsed to its target type.
    /// </exception>
    public RuntimeConfiguration Load(
        string? basePath = null,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var effectiveBasePath = string.IsNullOrWhiteSpace(basePath)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(basePath);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        ApplyDefaults(values);
        ApplyJson(values, Path.Combine(effectiveBasePath, JsonConfigRelativePath));
        ApplyYaml(values, ResolveYamlPath(effectiveBasePath));
        ApplyEnvironment(values, environment ?? ReadProcessEnvironment());

        return Bind(values);
    }

    /// <summary>
    /// Seeds <paramref name="values"/> with the hard-coded default settings.
    /// </summary>
    private static void ApplyDefaults(IDictionary<string, string> values)
    {
        values["Alloyed:Runtime:DefaultOutputPath"] = "out";
        values["Alloyed:Session:Enabled"] = "false";
        values["Alloyed:Decoration:EnableErrorHandling"] = "true";
        values["Alloyed:Decoration:EnableObservability"] = "true";
        values["Alloyed:Decoration:EnableCorrelation"] = "true";
        values["Alloyed:Decoration:EnableTransparency"] = "false";
        values["Alloyed:Decoration:TransparencyProfile"] = "Standard";
        values["Alloyed:Mocking:Enabled"] = "false";
        values["Alloyed:Mocking:Mode"] = "InMemory";
        values["Alloyed:Catalog:SourcePath"] = string.Empty;
    }

    /// <summary>
    /// Returns the resolved YAML config path (<c>.yml</c> preferred over <c>.yaml</c>), or
    /// <see langword="null"/> when neither file exists.
    /// </summary>
    private static string? ResolveYamlPath(string basePath)
    {
        var yml = Path.Combine(basePath, YamlConfigRelativePath);
        if (File.Exists(yml))
        {
            return yml;
        }

        var yaml = Path.Combine(basePath, YamlAltConfigRelativePath);
        return File.Exists(yaml) ? yaml : null;
    }

    /// <summary>
    /// Reads all process environment variables into a case-insensitive dictionary.
    /// </summary>
    private static IReadOnlyDictionary<string, string?> ReadProcessEnvironment()
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key = entry.Key?.ToString();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            result[key] = entry.Value?.ToString();
        }

        return result;
    }

    /// <summary>
    /// Parses the JSON file at <paramref name="jsonPath"/> (if it exists) and flattens its
    /// properties into <paramref name="values"/> using colon-separated key paths.
    /// </summary>
    private static void ApplyJson(IDictionary<string, string> values, string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(jsonPath));
        FlattenJson(document.RootElement, parentPath: string.Empty, values);
    }

    /// <summary>
    /// Recursively flattens a <see cref="JsonElement"/> into colon-separated key paths stored in
    /// <paramref name="values"/>. Object properties are traversed; leaf values are serialised to
    /// strings.
    /// </summary>
    private static void FlattenJson(JsonElement element, string parentPath, IDictionary<string, string> values)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var childPath = string.IsNullOrEmpty(parentPath)
                    ? property.Name
                    : $"{parentPath}:{property.Name}";

                FlattenJson(property.Value, childPath, values);
            }

            return;
        }

        if (string.IsNullOrEmpty(parentPath))
        {
            return;
        }

        values[parentPath] = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => element.GetRawText(),
        };
    }

    /// <summary>
    /// Parses the YAML file at <paramref name="yamlPath"/> (if it exists) using a minimal
    /// line-by-line parser that handles indented key-value pairs and inline comments.
    /// Does not support YAML lists, multi-document streams, or block scalars.
    /// </summary>
    private static void ApplyYaml(IDictionary<string, string> values, string? yamlPath)
    {
        if (string.IsNullOrWhiteSpace(yamlPath) || !File.Exists(yamlPath))
        {
            return;
        }

        var stack = new Stack<(int Indent, string Path)>();
        foreach (var rawLine in File.ReadAllLines(yamlPath))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var line = rawLine.Replace("\t", "  ", StringComparison.Ordinal);
            var commentIndex = line.IndexOf('#');
            if (commentIndex >= 0)
            {
                line = line[..commentIndex];
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var indent = CountLeadingSpaces(line);
            var trimmed = line.Trim();
            var split = trimmed.IndexOf(':');
            if (split <= 0)
            {
                continue;
            }

            var key = trimmed[..split].Trim();
            var value = trimmed[(split + 1)..].Trim();

            while (stack.Count > 0 && indent <= stack.Peek().Indent)
            {
                stack.Pop();
            }

            var parentPath = stack.Count == 0 ? string.Empty : stack.Peek().Path;
            var fullPath = string.IsNullOrEmpty(parentPath) ? key : $"{parentPath}:{key}";

            if (string.IsNullOrEmpty(value))
            {
                stack.Push((indent, fullPath));
                continue;
            }

            values[fullPath] = Unquote(value);
        }
    }

    /// <summary>
    /// Returns the number of leading space characters in <paramref name="value"/>.
    /// Tab characters should be expanded before calling this method.
    /// </summary>
    private static int CountLeadingSpaces(string value)
    {
        var count = 0;
        while (count < value.Length && value[count] == ' ')
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Strips surrounding single or double quotes from <paramref name="value"/> when both the
    /// opening and closing characters match. Returns the original string otherwise.
    /// </summary>
    private static string Unquote(string value)
    {
        if (value.Length < 2)
        {
            return value;
        }

        if ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\''))
        {
            return value[1..^1];
        }

        return value;
    }

    /// <summary>
    /// Applies environment variables from <paramref name="environment"/> to <paramref name="values"/>,
    /// supporting both the <c>ALLOYED__</c> prefix (higher precedence) and the legacy
    /// <c>TAF__</c> prefix. <c>TAF__</c> values are mirrored under the <c>Alloyed:</c> root for
    /// migration compatibility.
    /// </summary>
    private static void ApplyEnvironment(IDictionary<string, string> values, IReadOnlyDictionary<string, string?> environment)
    {
        // Migration compatibility: allow both prefixes. Alloyed has higher precedence.
        ApplyEnvironmentPrefix(values, environment, prefix: "TAF__", targetRoot: "TAF", mirrorToAlloyed: true);
        ApplyEnvironmentPrefix(values, environment, prefix: "ALLOYED__", targetRoot: "Alloyed", mirrorToAlloyed: false);
    }

    /// <summary>
    /// Iterates environment variables that start with <paramref name="prefix"/>, converts
    /// double-underscore separators to colons, and stores them under
    /// <c><paramref name="targetRoot"/>:suffix</c>. When <paramref name="mirrorToAlloyed"/> is
    /// <see langword="true"/>, also stores them under <c>Alloyed:suffix</c>.
    /// </summary>
    private static void ApplyEnvironmentPrefix(
        IDictionary<string, string> values,
        IReadOnlyDictionary<string, string?> environment,
        string prefix,
        string targetRoot,
        bool mirrorToAlloyed)
    {
        foreach (var (key, value) in environment.OrderBy(static kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var pathSuffix = key[prefix.Length..].Replace("__", ":", StringComparison.Ordinal);
            if (string.IsNullOrWhiteSpace(pathSuffix))
            {
                continue;
            }

            var path = $"{targetRoot}:{pathSuffix}";
            values[path] = value ?? string.Empty;

            if (mirrorToAlloyed)
            {
                values[$"Alloyed:{pathSuffix}"] = value ?? string.Empty;
            }
        }
    }

    /// <summary>
    /// Binds the flat key/value <paramref name="values"/> dictionary to a
    /// <see cref="RuntimeConfiguration"/> record by parsing each typed configuration entry.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when a value cannot be parsed to its expected type.
    /// </exception>
    private static RuntimeConfiguration Bind(IReadOnlyDictionary<string, string> values)
    {
        var runtime = new RuntimeOptions(
            FailOnSeverity: ParseOptionalEnum<PipelineDiagnosticSeverity>(
                values,
                "Alloyed:Runtime:FailOnSeverity",
                "TAF:Runtime:FailOnSeverity"),
            DefaultOutputPath: ParseString(values, defaultValue: "out", "Alloyed:Runtime:DefaultOutputPath", "TAF:Runtime:DefaultOutputPath"));

        var session = new SessionOptions(
            Enabled: ParseBool(values, defaultValue: false, "Alloyed:Session:Enabled", "TAF:Session:Enabled"));

        var decoration = new DecorationOptions(
            EnableErrorHandling: ParseBool(values, defaultValue: true, "Alloyed:Decoration:EnableErrorHandling", "TAF:Decoration:EnableErrorHandling"),
            EnableObservability: ParseBool(values, defaultValue: true, "Alloyed:Decoration:EnableObservability", "TAF:Decoration:EnableObservability"),
            EnableCorrelation: ParseBool(values, defaultValue: true, "Alloyed:Decoration:EnableCorrelation", "TAF:Decoration:EnableCorrelation"),
            EnableTransparency: ParseBool(values, defaultValue: false, "Alloyed:Decoration:EnableTransparency", "TAF:Decoration:EnableTransparency"),
            TransparencyProfile: ParseEnum<TransparencyProfile>(values, defaultValue: TransparencyProfile.Standard, "Alloyed:Decoration:TransparencyProfile", "TAF:Decoration:TransparencyProfile"));

        var mocking = new MockingOptions(
            Enabled: ParseBool(values, defaultValue: false, "Alloyed:Mocking:Enabled", "TAF:Mocking:Enabled"),
            Mode: ParseEnum<MockingMode>(values, defaultValue: MockingMode.InMemory, "Alloyed:Mocking:Mode", "TAF:Mocking:Mode"));

        var catalog = new CatalogOptions(
            SourcePath: ParseOptionalString(values, "Alloyed:Catalog:SourcePath", "TAF:Catalog:SourcePath"));

        return new RuntimeConfiguration(runtime, session, decoration, mocking, catalog);
    }

    /// <summary>
    /// Reads the first matching key from <paramref name="values"/> and parses it as a boolean.
    /// Returns <paramref name="defaultValue"/> when no key is found.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown when the raw value is not a valid boolean.</exception>
    private static bool ParseBool(IReadOnlyDictionary<string, string> values, bool defaultValue, params string[] keys)
    {
        var raw = GetValue(values, keys);
        if (raw is null)
        {
            return defaultValue;
        }

        if (bool.TryParse(raw, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Invalid boolean value '{raw}' for configuration key(s): {string.Join(", ", keys)}");
    }

    /// <summary>
    /// Reads the first matching key and parses it as a case-insensitive enum value.
    /// Returns <paramref name="defaultValue"/> when no key is found.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown when the raw value is not a valid enum member name.</exception>
    private static TEnum ParseEnum<TEnum>(IReadOnlyDictionary<string, string> values, TEnum defaultValue, params string[] keys)
        where TEnum : struct, Enum
    {
        var raw = GetValue(values, keys);
        if (raw is null)
        {
            return defaultValue;
        }

        if (Enum.TryParse<TEnum>(raw, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Invalid value '{raw}' for configuration key(s): {string.Join(", ", keys)}");
    }

    /// <summary>
    /// Reads the first matching key and parses it as a nullable enum value. Returns
    /// <see langword="null"/> when no key is found or the value is whitespace.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown when the raw value is not a valid enum member name.</exception>
    private static TEnum? ParseOptionalEnum<TEnum>(IReadOnlyDictionary<string, string> values, params string[] keys)
        where TEnum : struct, Enum
    {
        var raw = GetValue(values, keys);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (Enum.TryParse<TEnum>(raw, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Invalid value '{raw}' for configuration key(s): {string.Join(", ", keys)}");
    }

    /// <summary>
    /// Reads the first matching key as a string. Returns <paramref name="defaultValue"/> when no
    /// key is found or the value is whitespace.
    /// </summary>
    private static string ParseString(IReadOnlyDictionary<string, string> values, string defaultValue, params string[] keys)
    {
        var raw = GetValue(values, keys);
        return string.IsNullOrWhiteSpace(raw) ? defaultValue : raw;
    }

    /// <summary>
    /// Reads the first matching key as an optional string. Returns <see langword="null"/> when no
    /// key is found or the value is whitespace.
    /// </summary>
    private static string? ParseOptionalString(IReadOnlyDictionary<string, string> values, params string[] keys)
    {
        var raw = GetValue(values, keys);
        return string.IsNullOrWhiteSpace(raw) ? null : raw;
    }

    /// <summary>
    /// Returns the value for the first key in <paramref name="keys"/> that exists in
    /// <paramref name="values"/>, or <see langword="null"/> when none match.
    /// </summary>
    private static string? GetValue(IReadOnlyDictionary<string, string> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return null;
    }
}
