namespace Alloyed.DevOps.Multitool.Tests.Integration.Pipeline;

using System.Text;
using Alloyed.DevOps.Multitool.Core.Decoration.Models;
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
        output.Should().Contain("── Pipeline ");
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
        output.Should().Contain("── New-AlloyedModuleTransform ");
        output.Should().Contain("[ERROR] Pipeline failed.");
        output.Should().Contain("CommandsFound: 2");
        output.Should().Contain("MissingCommands: 2");
        output.Should().Contain("[ERROR] [PIPELINE-FAIL-ON-SEVERITY]");
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
    }

    [Fact]
    public void PlainReporter_Should_RenderDiagnosticsBlock()
    {
        var writer = new StringWriter(new StringBuilder());
        IConsoleReporter reporter = new PlainTextConsoleReporter(writer);

        reporter.WriteDiagnostics(
            "Diagnostics",
            new[]
            {
                new ConsoleDiagnosticEntry(ConsoleMessageLevel.Warning, "CATALOG-MISS", "Missing wrapper mapping.", "catalog", "Line 12, Col 3"),
            });

        var output = writer.ToString();
        output.Should().Contain("Diagnostics:");
        output.Should().Contain("[WARN] [CATALOG-MISS] Missing wrapper mapping.");
        output.Should().Contain("catalog @ Line 12, Col 3");
    }

    [Fact]
    public void PlainReporter_Should_RenderStructuredActivity()
    {
        var writer = new StringWriter(new StringBuilder());
        IConsoleReporter reporter = new PlainTextConsoleReporter(writer);

        reporter.WriteActivity(new ConsoleActivityEntry(
            ConsoleMessageLevel.Info,
            "TransparencyDecorator",
            "Enter",
            "Get-ChildItem",
            "abc123",
            0,
            "[enter] Get-ChildItem"));

        var output = writer.ToString();
        output.Should().Contain("[INFO] TransparencyDecorator Enter op=Get-ChildItem corr=abc123 elapsedMs=0 msg=[enter] Get-ChildItem");
    }

    [Fact]
    public void SpectreReporter_Should_RenderDiagnosticsTable()
    {
        var writer = new StringWriter(new StringBuilder());
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Out = new AnsiConsoleOutput(writer),
        });

        IConsoleReporter reporter = new SpectreConsoleReporter(console);

        reporter.WriteDiagnostics(
            "Diagnostics",
            new[]
            {
                new ConsoleDiagnosticEntry(ConsoleMessageLevel.Error, "PIPELINE-FAIL", "Pipeline failed.", "pipeline", "Line 1"),
            });

        var output = writer.ToString();
        output.Should().Contain("Diagnostics");
        output.Should().Contain("Level");
        output.Should().Contain("Code");
        output.Should().Contain("PIPELINE-FAIL");
        output.Should().Contain("pipeline");
    }

    [Fact]
    public void SpectreReporter_Should_RenderStructuredActivity()
    {
        var writer = new StringWriter(new StringBuilder());
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Out = new AnsiConsoleOutput(writer),
        });

        IConsoleReporter reporter = new SpectreConsoleReporter(console);

        reporter.WriteActivity(new ConsoleActivityEntry(
            ConsoleMessageLevel.Error,
            "TransparencyDecorator",
            "Error",
            "Get-ChildItem",
            "abc123",
            12,
            "[error] Get-ChildItem ex=InvalidOperationException"));

        var output = writer.ToString();
        output.Should().Contain("Error");
        output.Should().Contain("Get-ChildItem");
        output.Should().Contain("12ms");
        output.Should().Contain("error");
        output.Should().Contain("ex=In");
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
        output.Should().Contain("── New-AlloyedModuleTransform ");
        output.Should().Contain("[INFO] Pipeline completed successfully.");
        output.Should().Contain("Summary:");
        output.Should().Contain("CommandsFound");
        output.Should().Contain("CommandsReplaced");
        output.Should().Contain("MissingCommands");
        output.Should().Contain("ModulePath");
    }

    [Fact]
    public void SessionPresenter_Should_RenderReadyBanner()
    {
        var writer = new StringWriter(new StringBuilder());
        IConsoleReporter reporter = new PlainTextConsoleReporter(writer);

        SessionConsolePresenter.WriteSessionReady(
            reporter,
            transparencyEnabled: true,
            sessionModeEnabled: true,
            profile: "debug",
            outputMode: "Rich");

        var output = writer.ToString();
        output.Should().Contain("── Alloyed session is ready ");
        output.Should().Contain("Session status:");
        output.Should().Contain("Transparency");
        output.Should().Contain("SessionMode");
        output.Should().Contain("Profile");
        output.Should().Contain("OutputMode");
        output.Should().Contain("Next:");
    }

    [Fact]
    public void SessionPresenter_Should_RenderStoppedBanner()
    {
        var writer = new StringWriter(new StringBuilder());
        IConsoleReporter reporter = new PlainTextConsoleReporter(writer);

        SessionConsolePresenter.WriteSessionStopped(reporter);

        var output = writer.ToString();
        output.Should().Contain("── Alloyed session stopped ");
        output.Should().Contain("Transparency mode and session interception are now disabled.");
    }

    [Fact]
    public void RuntimeConfigurationPresenter_Should_RenderInitializationSummary()
    {
        var writer = new StringWriter(new StringBuilder());
        IConsoleReporter reporter = new PlainTextConsoleReporter(writer);

        RuntimeConfigurationConsolePresenter.WriteInitializationSummary(
            reporter,
            configPath: "C:\\repo\\config\\appsettings.json",
            outputMode: "Rich",
            enableTransparency: true,
            transparencyProfile: "standard",
            sessionEnabled: true,
            runtimeMaxRetries: 2,
            runtimeExponentialBackoff: true,
            runtimePreview: false,
            applyToCurrentSession: true);

        var output = writer.ToString();
        output.Should().Contain("── Alloyed runtime config initialized ");
        output.Should().Contain("Config summary:");
        output.Should().Contain("ConfigPath");
        output.Should().Contain("OutputMode");
        output.Should().Contain("ApplyToCurrentSession");
    }

    [Fact]
    public void RuntimeConfigurationPresenter_Should_RenderValidationSummary()
    {
        var writer = new StringWriter(new StringBuilder());
        IConsoleReporter reporter = new PlainTextConsoleReporter(writer);

        RuntimeConfigurationConsolePresenter.WriteValidationSummary(
            reporter,
            basePath: "C:\\repo",
            configPath: "C:\\repo\\config\\appsettings.json",
            runtimeDefaultOutputPath: "out",
            sessionEnabled: true,
            transparencyEnabled: true,
            consoleOutputMode: "Plain",
            runtimeMaxRetries: 1,
            runtimeRetryDelaySec: 2,
            runtimeExponentialBackoff: false,
            runtimePreview: true,
            runtimeTimeoutSec: 30);

        var output = writer.ToString();
        output.Should().Contain("── Alloyed runtime config validation ");
        output.Should().Contain("Effective runtime:");
        output.Should().Contain("RuntimeDefaultOutputPath");
        output.Should().Contain("RuntimeRetryDelaySec");
        output.Should().Contain("RuntimeTimeoutSec");
    }

    [Fact]
    public void ReporterDecorationSink_Should_RenderDecorationEvents_ThroughReporter()
    {
        var writer = new StringWriter(new StringBuilder());
        IConsoleReporter reporter = new PlainTextConsoleReporter(writer);
        var sink = new ReporterDecorationSink(reporter);

        sink.Write(new DecorationEvent(
            Operation: "Get-ChildItem",
            Decorator: "TransparencyDecorator",
            Stage: DecorationStage.Enter,
            ElapsedMilliseconds: 0,
            CorrelationId: "abc123",
            Message: "[enter] Get-ChildItem"));

        sink.Write(new DecorationEvent(
            Operation: "Get-ChildItem",
            Decorator: "TransparencyDecorator",
            Stage: DecorationStage.Error,
            ElapsedMilliseconds: 12,
            CorrelationId: "abc123",
            Message: "[error] Get-ChildItem ex=InvalidOperationException"));

        var output = writer.ToString();
        output.Should().Contain("[INFO] TransparencyDecorator Enter op=Get-ChildItem corr=abc123 elapsedMs=0 msg=activity");
        output.Should().Contain("[ERROR] TransparencyDecorator Error op=Get-ChildItem corr=abc123 elapsedMs=12 msg=ex=InvalidOperationException");
    }

    [Fact]
    public void ReporterDecorationSink_Should_TrimStructuredTransparencyMessagePrefix()
    {
        var writer = new StringWriter(new StringBuilder());
        IConsoleReporter reporter = new PlainTextConsoleReporter(writer);
        var sink = new ReporterDecorationSink(reporter);

        sink.Write(new DecorationEvent(
            Operation: "Split-Path",
            Decorator: "TransparencyDecorator",
            Stage: DecorationStage.Enter,
            ElapsedMilliseconds: 0,
            CorrelationId: "corr-1",
            Message: "phase=enter op=Split-Path corr=corr-1 profile=standard tags.count=5 tags.preview=correlationId=corr-1"));

        var output = writer.ToString();
        output.Should().Contain("msg=profile=standard tags.count=5 tags.preview=correlationId=corr-1");
    }
}
