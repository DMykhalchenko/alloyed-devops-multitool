namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using System.Text.Json;
using Alloyed.DevOps.Multitool.Host.PowerShell.Models;

public sealed class RuntimeConfigurationLoader
{
    private const string JsonConfigRelativePath = "config/appsettings.json";
    private const string YamlConfigRelativePath = "config/appsettings.yml";
    private const string YamlAltConfigRelativePath = "config/appsettings.yaml";

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

    private static void ApplyDefaults(IDictionary<string, string> values)
    {
        values["Alloyed:Runtime:DefaultOutputPath"] = "out";
        values["Alloyed:Session:Enabled"] = "false";
        values["Alloyed:Decoration:EnableErrorHandling"] = "true";
        values["Alloyed:Decoration:EnableObservability"] = "true";
        values["Alloyed:Decoration:EnableCorrelation"] = "true";
        values["Alloyed:Decoration:EnableTransparency"] = "false";
        values["Alloyed:Mocking:Enabled"] = "false";
        values["Alloyed:Mocking:Mode"] = "InMemory";
        values["Alloyed:Catalog:SourcePath"] = string.Empty;
    }

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

    private static void ApplyJson(IDictionary<string, string> values, string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(jsonPath));
        FlattenJson(document.RootElement, parentPath: string.Empty, values);
    }

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

    private static int CountLeadingSpaces(string value)
    {
        var count = 0;
        while (count < value.Length && value[count] == ' ')
        {
            count++;
        }

        return count;
    }

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

    private static void ApplyEnvironment(IDictionary<string, string> values, IReadOnlyDictionary<string, string?> environment)
    {
        // Migration compatibility: allow both prefixes. Alloyed has higher precedence.
        ApplyEnvironmentPrefix(values, environment, prefix: "TAF__", targetRoot: "TAF", mirrorToAlloyed: true);
        ApplyEnvironmentPrefix(values, environment, prefix: "ALLOYED__", targetRoot: "Alloyed", mirrorToAlloyed: false);
    }

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
            EnableTransparency: ParseBool(values, defaultValue: false, "Alloyed:Decoration:EnableTransparency", "TAF:Decoration:EnableTransparency"));

        var mocking = new MockingOptions(
            Enabled: ParseBool(values, defaultValue: false, "Alloyed:Mocking:Enabled", "TAF:Mocking:Enabled"),
            Mode: ParseEnum<MockingMode>(values, defaultValue: MockingMode.InMemory, "Alloyed:Mocking:Mode", "TAF:Mocking:Mode"));

        var catalog = new CatalogOptions(
            SourcePath: ParseOptionalString(values, "Alloyed:Catalog:SourcePath", "TAF:Catalog:SourcePath"));

        return new RuntimeConfiguration(runtime, session, decoration, mocking, catalog);
    }

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

    private static string ParseString(IReadOnlyDictionary<string, string> values, string defaultValue, params string[] keys)
    {
        var raw = GetValue(values, keys);
        return string.IsNullOrWhiteSpace(raw) ? defaultValue : raw;
    }

    private static string? ParseOptionalString(IReadOnlyDictionary<string, string> values, params string[] keys)
    {
        var raw = GetValue(values, keys);
        return string.IsNullOrWhiteSpace(raw) ? null : raw;
    }

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
