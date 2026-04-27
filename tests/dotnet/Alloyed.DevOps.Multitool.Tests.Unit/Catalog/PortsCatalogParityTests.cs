namespace Alloyed.DevOps.Multitool.Tests.Unit.Catalog;

using System.Text.Json;
using Alloyed.DevOps.Multitool.Core.Catalog.Services;
using FluentAssertions;

public sealed class PortsCatalogParityTests
{
    [Fact]
    public void Resolve_Should_MapEveryCatalogCommandAndAlias()
    {
        var entries = LoadCatalogEntries();
        var catalog = new InMemoryWrapperCatalog();

        var allTokens = entries
            .Select(static e => e.Command)
            .Concat(entries.SelectMany(static e => e.Aliases))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var result = catalog.Resolve(allTokens);

        foreach (var entry in entries)
        {
            result.Replacements[entry.Command].Should().Be(entry.Wrapper);
            foreach (var alias in entry.Aliases)
            {
                result.Replacements[alias].Should().Be(entry.Wrapper);
            }
        }

        result.MissingCommands.Should().BeEmpty();
        result.RequiredModules.Should().Contain("Alloyed.DevOps.Multitool");
    }

    [Fact]
    public void GetMappings_Should_BeInParityWithCatalogEntries()
    {
        var entries = LoadCatalogEntries();
        var catalog = new InMemoryWrapperCatalog();

        var expected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            expected[entry.Command] = entry.Wrapper;
            foreach (var alias in entry.Aliases)
            {
                expected[alias] = entry.Wrapper;
            }
        }

        var actual = catalog.GetMappings();
        actual.Should().BeEquivalentTo(expected);
    }

    private static IReadOnlyList<PortCatalogEntry> LoadCatalogEntries()
    {
        var catalogPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ports.catalog.json");
        File.Exists(catalogPath).Should().BeTrue($"catalog fixture was not found at '{catalogPath}'");

        var json = File.ReadAllText(catalogPath);
        var entries = JsonSerializer.Deserialize<List<PortCatalogEntry>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        entries.Should().NotBeNull();
        entries!.Should().NotBeEmpty();
        return entries!;
    }

    private sealed class PortCatalogEntry
    {
        public string Command { get; init; } = string.Empty;
        public string Wrapper { get; init; } = string.Empty;
        public string Native { get; init; } = string.Empty;
        public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();
    }
}
