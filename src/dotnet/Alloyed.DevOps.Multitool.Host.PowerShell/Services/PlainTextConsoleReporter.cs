namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Contracts;

/// <summary>
/// An <see cref="IConsoleReporter"/> that writes plain, uncoloured text to a
/// <see cref="TextWriter"/> (defaults to <see cref="Console.Out"/>). Safe to use in
/// non-interactive environments where ANSI escape codes are not supported.
/// </summary>
public sealed class PlainTextConsoleReporter : IConsoleReporter
{
    private readonly TextWriter writer;

    /// <summary>
    /// Initializes the reporter with an optional <paramref name="writer"/>.
    /// Defaults to <see cref="Console.Out"/> when <see langword="null"/>.
    /// </summary>
    /// <param name="writer">Destination for all output lines.</param>
    public PlainTextConsoleReporter(TextWriter? writer = null)
    {
        this.writer = writer ?? Console.Out;
    }

    /// <summary>
    /// Writes a plain-text rule line: <c>── title ─────</c> to the output writer.
    /// </summary>
    /// <inheritdoc/>
    public void WriteHeader(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        string label = $"── {title} ";
        string fill = new('─', Math.Max(0, 80 - label.Length));
        writer.WriteLine($"{label}{fill}");
    }

    /// <summary>
    /// Writes <c>[INFO|WARN|ERROR] message</c> to the output writer.
    /// </summary>
    /// <inheritdoc/>
    public void WriteMessage(ConsoleMessageLevel level, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        var prefix = level switch
        {
            ConsoleMessageLevel.Info => "[INFO]",
            ConsoleMessageLevel.Warning => "[WARN]",
            ConsoleMessageLevel.Error => "[ERROR]",
            _ => "[INFO]",
        };

        writer.WriteLine($"{prefix} {message}");
    }

    /// <summary>
    /// Writes <c>key: value</c> to the output writer.
    /// </summary>
    /// <inheritdoc/>
    public void WriteKeyValue(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        writer.WriteLine($"{key}: {value}");
    }

    /// <summary>
    /// Writes an optional title followed by each key/value row as plain aligned text.
    /// </summary>
    /// <inheritdoc/>
    public void WriteKeyValueTable(string? title, IReadOnlyList<ConsoleKeyValueRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (!string.IsNullOrWhiteSpace(title))
        {
            writer.WriteLine($"{title}:");
        }

        if (rows.Count == 0)
        {
            return;
        }

        var maxKeyLength = rows.Max(static row => row.Key?.Length ?? 0);
        foreach (var row in rows)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(row.Key);
            writer.WriteLine($"  {row.Key.PadRight(maxKeyLength)} : {row.Value}");
        }
    }

    /// <summary>
    /// Writes diagnostics as plain text lines with severity, code, and optional source/location.
    /// </summary>
    /// <inheritdoc/>
    public void WriteDiagnostics(string? title, IReadOnlyList<ConsoleDiagnosticEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count == 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            writer.WriteLine($"{title}:");
        }

        foreach (var entry in entries)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entry.Code);
            ArgumentException.ThrowIfNullOrWhiteSpace(entry.Message);

            var prefix = entry.Level switch
            {
                ConsoleMessageLevel.Info => "[INFO]",
                ConsoleMessageLevel.Warning => "[WARN]",
                ConsoleMessageLevel.Error => "[ERROR]",
                _ => "[INFO]",
            };

            var context = string.Empty;
            if (!string.IsNullOrWhiteSpace(entry.Source) || !string.IsNullOrWhiteSpace(entry.Location))
            {
                context = $" ({entry.Source}{(string.IsNullOrWhiteSpace(entry.Source) || string.IsNullOrWhiteSpace(entry.Location) ? string.Empty : " @ ")}{entry.Location})";
            }

            writer.WriteLine($"  {prefix} [{entry.Code}] {entry.Message}{context}");
        }
    }

    /// <summary>
    /// Writes one structured activity/event as a readable plain-text line.
    /// </summary>
    /// <inheritdoc/>
    public void WriteActivity(ConsoleActivityEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.Category);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.Stage);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.Operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.Message);

        var prefix = entry.Level switch
        {
            ConsoleMessageLevel.Info => "[INFO]",
            ConsoleMessageLevel.Warning => "[WARN]",
            ConsoleMessageLevel.Error => "[ERROR]",
            _ => "[INFO]",
        };

        var correlationId = string.IsNullOrWhiteSpace(entry.CorrelationId) ? "-" : entry.CorrelationId;
        writer.WriteLine(
            $"{prefix} {entry.Category} {entry.Stage} op={entry.Operation} corr={correlationId} elapsedMs={entry.ElapsedMilliseconds} msg={entry.Message}");
    }
}
