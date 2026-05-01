namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Alloyed.DevOps.Multitool.Core.Decoration.Contracts;
using Alloyed.DevOps.Multitool.Core.Decoration.Models;
using Contracts;

/// <summary>
/// Bridges decoration events into the configured console reporter so transparency output follows
/// the same plain/rich rendering policy as the rest of the host UX.
/// </summary>
public sealed class ReporterDecorationSink : IDecorationSink
{
    private readonly IConsoleReporter _reporter;

    /// <summary>
    /// Initializes the sink with a reporter selected for the current host output mode.
    /// </summary>
    public ReporterDecorationSink(IConsoleReporter reporter)
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
    }

    /// <inheritdoc />
    public void Write(DecorationEvent entry)
    {
        if (entry is null)
        {
            return;
        }

        try
        {
            _reporter.WriteActivity(
                new ConsoleActivityEntry(
                    MapLevel(entry.Stage),
                    entry.Decorator,
                    entry.Stage.ToString(),
                    entry.Operation,
                    entry.CorrelationId,
                    entry.ElapsedMilliseconds,
                    entry.Message ?? "-"));
        }
        catch
        {
            // Decoration sinks must never interfere with the wrapped operation.
        }
    }

    private static ConsoleMessageLevel MapLevel(DecorationStage stage) =>
        stage switch
        {
            DecorationStage.Error => ConsoleMessageLevel.Error,
            _ => ConsoleMessageLevel.Info,
        };
}
