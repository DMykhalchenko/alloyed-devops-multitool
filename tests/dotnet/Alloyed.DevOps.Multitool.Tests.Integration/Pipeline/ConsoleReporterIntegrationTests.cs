namespace Alloyed.DevOps.Multitool.Tests.Integration.Pipeline;

using System.Text;
using Alloyed.DevOps.Multitool.Host.PowerShell.Contracts;
using Alloyed.DevOps.Multitool.Host.PowerShell.Models;
using Alloyed.DevOps.Multitool.Host.PowerShell.Services;
using FluentAssertions;
using Spectre.Console;

public class ConsoleReporterIntegrationTests
{
    [Fact]
    public void Factory_Should_ReturnPlainReporter_ForPlainMode()
    {
        var writer = new StringWriter(new StringBuilder());

        var reporter = ConsoleReporterFactory.Create(ConsoleOutputMode.Plain, isInteractive: true, writer);

        reporter.Should().BeOfType<PlainTextConsoleReporter>();
    }

    [Fact]
    public void Factory_Should_ReturnSpectreReporter_ForRichInteractiveMode()
    {
        var reporter = ConsoleReporterFactory.Create(ConsoleOutputMode.Rich, isInteractive: true);

        reporter.Should().BeOfType<SpectreConsoleReporter>();
    }

    [Fact]
    public void Factory_Should_FallbackToPlainReporter_ForRichNonInteractiveMode()
    {
        var writer = new StringWriter(new StringBuilder());
        var reporter = ConsoleReporterFactory.Create(ConsoleOutputMode.Rich, isInteractive: false, writer);

        reporter.Should().BeOfType<PlainTextConsoleReporter>();
    }

    [Fact]
    public void PlainReporter_Should_WriteExpectedFormat()
    {
        var writer = new StringWriter(new StringBuilder());
        IConsoleReporter reporter = new PlainTextConsoleReporter(writer);

        reporter.WriteHeader("Pipeline");
        reporter.WriteMessage(ConsoleMessageLevel.Warning, "be careful");
        reporter.WriteKeyValue("CommandsFound", "3");

        var output = writer.ToString();
        output.Should().Contain("== Pipeline ==");
        output.Should().Contain("[WARN] be careful");
        output.Should().Contain("CommandsFound: 3");
    }

    [Fact]
    public void PlainReporter_Should_FormatInfoAndErrorLevels()
    {
        var writer = new StringWriter(new StringBuilder());
        IConsoleReporter reporter = new PlainTextConsoleReporter(writer);

        reporter.WriteMessage(ConsoleMessageLevel.Info, "pipeline completed successfully.");
        reporter.WriteMessage(ConsoleMessageLevel.Error, "pipeline failed.");

        var output = writer.ToString();
        output.Should().Contain("[INFO] pipeline completed successfully.");
        output.Should().Contain("[ERROR] pipeline failed.");
    }

    [Fact]
    public void PlainReporter_Should_FormatFullFailureSummary()
    {
        var writer = new StringWriter(new StringBuilder());
        IConsoleReporter reporter = new PlainTextConsoleReporter(writer);

        reporter.WriteHeader("New-AlloyedModuleTransform");
        reporter.WriteMessage(ConsoleMessageLevel.Error, "Pipeline failed.");
        reporter.WriteKeyValue("CommandsFound", "2");
        reporter.WriteKeyValue("CommandsReplaced", "0");
        reporter.WriteKeyValue("MissingCommands", "2");
        reporter.WriteMessage(ConsoleMessageLevel.Error, "[PIPELINE-FAIL-ON-SEVERITY] Pipeline stopped because analyzer diagnostics met fail policy (Warning or higher).");

        var output = writer.ToString();
        output.Should().Contain("== New-AlloyedModuleTransform ==");
        output.Should().Contain("[ERROR] Pipeline failed.");
        output.Should().Contain("CommandsFound: 2");
        output.Should().Contain("MissingCommands: 2");
        output.Should().Contain("PIPELINE-FAIL-ON-SEVERITY");
    }

    [Fact]
    public void PlainReporter_Should_RenderStructuredKeyValueTable()
    {
        var writer = new StringWriter(new StringBuilder());
        IConsoleReporter reporter = new PlainTextConsoleReporter(writer);

        reporter.WriteKeyValueTable(
            "Summary",
            new[]
            {
                new ConsoleKeyValueRow("CommandsFound", "3"),
                new ConsoleKeyValueRow("CommandsReplaced", "2"),
            });

        var output = writer.ToString();
        output.Should().Contain("Summary:");
        output.Should().Contain("CommandsFound");
        output.Should().Contain("CommandsReplaced");
        output.Should().Contain("3");
        output.Should().Contain("2");
    }

    [Fact]
    public void SpectreReporter_Should_RenderStructuredKeyValueTable()
    {
        var writer = new StringWriter(new StringBuilder());
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Out = new AnsiConsoleOutput(writer),
        });

        IConsoleReporter reporter = new SpectreConsoleReporter(console);

        reporter.WriteKeyValueTable(
            "Summary",
            new[]
            {
                new ConsoleKeyValueRow("CommandsFound", "3"),
                new ConsoleKeyValueRow("CommandsReplaced", "2"),
            });

        var output = writer.ToString();
        output.Should().Contain("Summary");
        output.Should().Contain("CommandsFound");
        output.Should().Contain("CommandsReplaced");
        output.Should().Contain("Property");
        output.Should().Contain("Value");
    }

    [Fact]
    public void PipelineResultPresenter_Should_RenderSummaryBlock()
    {
        var writer = new StringWriter(new StringBuilder());
        IConsoleReporter reporter = new PlainTextConsoleReporter(writer);
        var result = new PipelineResult(
            Success: true,
            ModulePath: "C:\\out\\DemoModule",
            CommandsFound: 4,
            CommandsReplaced: 3,
            MissingCommands: new[] { "Write-Thing" },
            Diagnostics: Array.Empty<PipelineDiagnostic>(),
            ErrorMessage: null);

        PipelineResultConsolePresenter.WriteSummary(reporter, result, "New-AlloyedModuleTransform");

        var output = writer.ToString();
        output.Should().Contain("== New-AlloyedModuleTransform ==");
        output.Should().Contain("[INFO] Pipeline completed successfully.");
        output.Should().Contain("Summary:");
        output.Should().Contain("CommandsFound");
        output.Should().Contain("CommandsReplaced");
        output.Should().Contain("MissingCommands");
        output.Should().Contain("ModulePath");
    }
}
