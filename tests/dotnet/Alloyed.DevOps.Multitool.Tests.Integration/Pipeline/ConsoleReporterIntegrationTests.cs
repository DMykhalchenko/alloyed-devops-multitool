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
}

