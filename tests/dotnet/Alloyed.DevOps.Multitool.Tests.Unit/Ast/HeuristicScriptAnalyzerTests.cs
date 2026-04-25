namespace Alloyed.DevOps.Multitool.Tests.Unit.Ast;

using Alloyed.DevOps.Multitool.Core.Ast.Services;
using FluentAssertions;

public class HeuristicScriptAnalyzerTests
{
    [Fact]
    public void AnalyzeContent_Should_ExtractKnownCommands()
    {
        var analyzer = new HeuristicScriptAnalyzer();
        var script = "Get-ChildItem -Path .\nGet-Item -Path .\nTest-Path -Path .";

        var result = analyzer.AnalyzeContent("sample.ps1", script);

        result.Commands.Select(c => c.CommandName)
            .Should()
            .Contain(new[] { "Get-ChildItem", "Get-Item", "Test-Path" });
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void AnalyzeContent_Should_IgnoreCommandsInsideQuotes_AndComments()
    {
        var analyzer = new HeuristicScriptAnalyzer();
        var script = "Write-Host \"Get-ChildItem\" # Get-Item\nGet-ChildItem -Path .";

        var result = analyzer.AnalyzeContent("sample.ps1", script);

        result.Commands.Select(c => c.CommandName)
            .Should()
            .Equal("Write-Host", "Get-ChildItem");
    }

    [Fact]
    public void AnalyzeContent_Should_IgnoreCommandsInsideHereString()
    {
        var analyzer = new HeuristicScriptAnalyzer();
        var script = "@\"\nGet-ChildItem -Path .\n\"@\nGet-ChildItem -Path .";

        var result = analyzer.AnalyzeContent("sample.ps1", script);

        result.Commands.Select(c => c.CommandName)
            .Should()
            .Equal("Get-ChildItem");
    }

    [Fact]
    public void AnalyzeContent_Should_ExtractSupportedAliases()
    {
        var analyzer = new HeuristicScriptAnalyzer();
        var script = "gci -Path .\ngi -Path .\ntp -Path .";

        var result = analyzer.AnalyzeContent("sample.ps1", script);

        result.Commands.Select(c => c.CommandName)
            .Should()
            .Equal("gci", "gi", "tp");
    }
}
