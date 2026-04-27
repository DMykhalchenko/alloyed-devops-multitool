namespace Alloyed.DevOps.Multitool.Tests.Unit.Ast;

using Alloyed.DevOps.Multitool.Core.Ast.Models;
using Alloyed.DevOps.Multitool.Core.Ast.Services;
using FluentAssertions;

public class PowerShellScriptAnalyzerTests
{
    private readonly PowerShellScriptAnalyzer _analyzer = new();

    [Fact]
    public void AnalyzeContent_Should_ExtractVerbNounCommands()
    {
        var script = "Get-ChildItem -Path .\nGet-Item -Path .\nTest-Path -Path .";

        var result = _analyzer.AnalyzeContent("sample.ps1", script);

        result.Commands.Select(c => c.CommandName)
            .Should()
            .Contain(new[] { "Get-ChildItem", "Get-Item", "Test-Path" });
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void AnalyzeContent_Should_ExtractSingleWordAliases()
    {
        var script = "measure\nsort\ngroup\nps\nkill\ncls\nclear\nsls";

        var result = _analyzer.AnalyzeContent("sample.ps1", script);

        result.Commands.Select(c => c.CommandName)
            .Should()
            .Contain(new[] { "measure", "sort", "group", "ps", "kill", "cls", "clear", "sls" });
    }

    [Fact]
    public void AnalyzeContent_Should_ExtractDiagnosticsGroupAliases()
    {
        var script = "gps\nsaps\nstart something\nspps\nicm { }\ngci\ngi\ntp -Path .";

        var result = _analyzer.AnalyzeContent("sample.ps1", script);

        var names = result.Commands.Select(c => c.CommandName).ToList();
        names.Should().Contain("gps");
        names.Should().Contain("saps");
        names.Should().Contain("spps");
        names.Should().Contain("icm");
        names.Should().Contain("gci");
        names.Should().Contain("gi");
        names.Should().Contain("tp");
    }

    [Fact]
    public void AnalyzeContent_Should_ExtractCommandsInsideScriptBlock()
    {
        var script = "Invoke-Command -ScriptBlock { Get-Process; Stop-Process -Id 1 }";

        var result = _analyzer.AnalyzeContent("sample.ps1", script);

        var names = result.Commands.Select(c => c.CommandName).ToList();
        names.Should().Contain("Invoke-Command");
        names.Should().Contain("Get-Process");
        names.Should().Contain("Stop-Process");
    }

    [Fact]
    public void AnalyzeContent_Should_NotExtractCommandsInsideStrings()
    {
        var script = "Write-Host \"Get-ChildItem\" 'Test-Path'";

        var result = _analyzer.AnalyzeContent("sample.ps1", script);

        result.Commands.Select(c => c.CommandName)
            .Should()
            .Equal("Write-Host");
    }

    [Fact]
    public void AnalyzeContent_Should_NotExtractCommandsInsideHereString()
    {
        var script = "@\"\nGet-ChildItem -Path .\n\"@\nGet-ChildItem -Path .";

        var result = _analyzer.AnalyzeContent("sample.ps1", script);

        result.Commands.Select(c => c.CommandName)
            .Should()
            .Equal("Get-ChildItem");
    }

    [Fact]
    public void AnalyzeContent_Should_NotExtractCommandsInsideLineComment()
    {
        var script = "Get-ChildItem -Path . # Get-Item";

        var result = _analyzer.AnalyzeContent("sample.ps1", script);

        result.Commands.Select(c => c.CommandName)
            .Should()
            .Equal("Get-ChildItem");
    }

    [Fact]
    public void AnalyzeContent_Should_ReportParseErrorsAsWarningDiagnostics()
    {
        var script = "function { }";

        var result = _analyzer.AnalyzeContent("sample.ps1", script);

        result.Diagnostics.Should().NotBeEmpty();
        result.Diagnostics.Should().AllSatisfy(d => d.Severity.Should().Be(ParseDiagnosticSeverity.Warning));
    }

    [Fact]
    public void AnalyzeContent_Should_ExtractModuleQualifiedCommand()
    {
        var script = "Microsoft.PowerShell.Utility\\Select-String -Pattern foo -Path .";

        var result = _analyzer.AnalyzeContent("sample.ps1", script);

        var cmd = result.Commands.Should().ContainSingle().Subject;
        cmd.CommandName.Should().Be("Select-String");
        cmd.ModuleName.Should().Be("Microsoft.PowerShell.Utility");
        cmd.IsQualified.Should().BeTrue();
    }

    [Fact]
    public void AnalyzeContent_Should_RecordCorrectLineNumbers()
    {
        var script = "Get-ChildItem -Path .\nGet-Item -Path .";

        var result = _analyzer.AnalyzeContent("sample.ps1", script);

        result.Commands.First(c => c.CommandName == "Get-ChildItem").Line.Should().Be(1);
        result.Commands.First(c => c.CommandName == "Get-Item").Line.Should().Be(2);
    }

    [Fact]
    public void AnalyzeContent_Should_ExtractManagementGroupAliases()
    {
        var script = "gsv\nsasv\nspsv";

        var result = _analyzer.AnalyzeContent("sample.ps1", script);

        result.Commands.Select(c => c.CommandName)
            .Should()
            .Contain(new[] { "gsv", "sasv", "spsv" });
    }

    [Fact]
    public void AnalyzeContent_Should_ExtractHostGroupAliases()
    {
        var script = "cls\nclear";

        var result = _analyzer.AnalyzeContent("sample.ps1", script);

        result.Commands.Select(c => c.CommandName)
            .Should()
            .Contain(new[] { "cls", "clear" });
    }
}
