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
                    NormalizeMessage(entry)));
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

    private static string NormalizeMessage(DecorationEvent entry)
    {
        var message = string.IsNullOrWhiteSpace(entry.Message) ? "-" : entry.Message.Trim();
        if (!entry.Decorator.Equals("TransparencyDecorator", StringComparison.Ordinal))
        {
            return message;
        }

        var bracketPrefix = $"[{entry.Stage.ToString().ToLowerInvariant()}] {entry.Operation}";
        if (message.StartsWith(bracketPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = message[bracketPrefix.Length..].TrimStart();
            return string.IsNullOrWhiteSpace(remainder) ? "activity" : remainder;
        }

        var structuredPrefix =
            $"phase={entry.Stage.ToString().ToLowerInvariant()} op={entry.Operation} corr={entry.CorrelationId ?? "-"}";
        if (message.StartsWith(structuredPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = message[structuredPrefix.Length..].TrimStart();
            return string.IsNullOrWhiteSpace(remainder) ? "activity" : remainder;
        }

        return message;
    }
}
