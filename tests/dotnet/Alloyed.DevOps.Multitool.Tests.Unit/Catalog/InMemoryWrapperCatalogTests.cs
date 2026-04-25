namespace Alloyed.DevOps.Multitool.Tests.Unit.Catalog;

using Alloyed.DevOps.Multitool.Core.Catalog.Services;
using FluentAssertions;

public class InMemoryWrapperCatalogTests
{
    [Fact]
    public void Resolve_Should_MapKnownCommands_AndMarkUnknownAsMissing()
    {
        var catalog = new InMemoryWrapperCatalog();

        var result = catalog.Resolve(new[] { "Get-ChildItem", "Unknown-Command" });

        result.Replacements["Get-ChildItem"].Should().Be("Get-AlloyedChildItem");
        result.Replacements["Unknown-Command"].Should().Be("Unknown-Command");
        result.MissingCommands.Should().ContainSingle().Which.Should().Be("Unknown-Command");
        result.RequiredModules.Should().Contain("Alloyed.DevOps.Multitool");
    }
}
