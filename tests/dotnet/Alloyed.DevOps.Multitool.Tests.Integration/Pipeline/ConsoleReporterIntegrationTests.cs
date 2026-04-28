namespace Alloyed.DevOps.Multitool.Tests.Integration.Pipeline;

using System.Text;
using Alloyed.DevOps.Multitool.Host.PowerShell.Contracts;
using Alloyed.DevOps.Multitool.Host.PowerShell.Services;
using FluentAssertions;

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
}
