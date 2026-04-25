namespace Alloyed.DevOps.Multitool.Tests.Unit.Builders;

using Alloyed.DevOps.Multitool.Core.Builders.Services;
using FluentAssertions;

public class TextCommandTransformerTests
{
    [Fact]
    public void Transform_Should_ReplaceOnlyWholeCommandTokens()
    {
        var transformer = new TextCommandTransformer();
        var source = "Get-ChildItem -Path .\nGet-ChildItemExtra";
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Get-ChildItem"] = "Get-AlloyedChildItem",
        };

        var output = transformer.Transform(source, map);

        output.Should().Contain("Get-AlloyedChildItem -Path .");
        output.Should().Contain("Get-ChildItemExtra");
    }

    [Fact]
    public void Transform_Should_NotReplaceInsideQuotes_OrComments()
    {
        var transformer = new TextCommandTransformer();
        var source = "Write-Host \"Get-ChildItem\"\nGet-ChildItem -Path . # Get-ChildItem";
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Get-ChildItem"] = "Get-AlloyedChildItem",
        };

        var output = transformer.Transform(source, map);

        output.Should().Contain("Write-Host \"Get-ChildItem\"");
        output.Should().Contain("Get-AlloyedChildItem -Path . # Get-ChildItem");
        output.Should().NotContain("\"Get-AlloyedChildItem\"");
    }

    [Fact]
    public void Transform_Should_NotReplaceInsideHereString()
    {
        var transformer = new TextCommandTransformer();
        var source = "@\"\nGet-ChildItem -Path .\n\"@\nGet-ChildItem -Path .";
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Get-ChildItem"] = "Get-AlloyedChildItem",
        };

        var output = transformer.Transform(source, map);

        output.Should().Contain("@\"\nGet-ChildItem -Path .\n\"@");
        output.Should().Contain("Get-AlloyedChildItem -Path .");
    }

    [Fact]
    public void Transform_Should_MatchGoldenFixtureOutput()
    {
        var transformer = new TextCommandTransformer();
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Transform");
        var inputPath = Path.Combine(fixtureDir, "golden-input.ps1");
        var expectedPath = Path.Combine(fixtureDir, "golden-expected.ps1");

        var source = File.ReadAllText(inputPath);
        var expected = File.ReadAllText(expectedPath);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Get-ChildItem"] = "Get-AlloyedChildItem",
            ["Get-Item"] = "Get-AlloyedItem",
            ["Test-Path"] = "Test-AlloyedPath",
        };

        var output = transformer.Transform(source, map);

        output.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Should()
            .Be(expected.Replace("\r\n", "\n", StringComparison.Ordinal));
    }
}
