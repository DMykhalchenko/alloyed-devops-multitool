namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Contracts;
using Spectre.Console;

/// <summary>
/// An <see cref="IConsoleReporter"/> backed by Spectre.Console that renders coloured, styled
/// output when the terminal supports ANSI escape sequences. Used by
/// <see cref="ConsoleReporterFactory"/> when the session is interactive and
/// <see cref="ConsoleOutputMode.Rich"/> is requested.
/// </summary>
public sealed class SpectreConsoleReporter : IConsoleReporter
{
    private readonly IAnsiConsole console;

    /// <summary>
    /// Initializes the reporter with an optional <paramref name="console"/>.
    /// Defaults to <see cref="AnsiConsole.Console"/> when <see langword="null"/>.
    /// </summary>
    /// <param name="console">Spectre.Console instance to write to.</param>
    public SpectreConsoleReporter(IAnsiConsole? console = null)
    {
        this.console = console ?? AnsiConsole.Console;
    }

    /// <summary>
    /// Writes a bold cyan <c>== title ==</c> header via Spectre markup.
    /// </summary>
    /// <inheritdoc/>
    public void WriteHeader(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        console.MarkupLine($"[bold cyan]== {Escape(title)} ==[/]");
    }

    /// <summary>
    /// Writes a colour-coded <c>[[INFO|WARN|ERROR]] message</c> via Spectre markup.
    /// Info is white, Warning is yellow, Error is red.
    /// </summary>
    /// <inheritdoc/>
    public void WriteMessage(ConsoleMessageLevel level, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        var style = level switch
        {
            ConsoleMessageLevel.Info => "white",
            ConsoleMessageLevel.Warning => "yellow",
            ConsoleMessageLevel.Error => "red",
            _ => "white",
        };

        var prefix = level switch
        {
            ConsoleMessageLevel.Info => "INFO",
            ConsoleMessageLevel.Warning => "WARN",
            ConsoleMessageLevel.Error => "ERROR",
            _ => "INFO",
        };

        console.MarkupLine($"[{style}][[{prefix}]][/] {Escape(message)}");
    }

    /// <summary>
    /// Writes a grey <c>key:</c> label followed by a white <c>value</c> via Spectre markup.
    /// </summary>
    /// <inheritdoc/>
    public void WriteKeyValue(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        console.MarkupLine($"[grey]{Escape(key)}:[/] [white]{Escape(value)}[/]");
    }

    /// <summary>
    /// Writes an optional title and renders key/value rows as a Spectre.Console table.
    /// </summary>
    /// <inheritdoc/>
    public void WriteKeyValueTable(string? title, IReadOnlyList<ConsoleKeyValueRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (!string.IsNullOrWhiteSpace(title))
        {
            console.MarkupLine($"[bold]{Escape(title)}[/]");
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[grey]Property[/]")
            .AddColumn("[grey]Value[/]");

        foreach (var row in rows)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(row.Key);
            table.AddRow(Escape(row.Key), Escape(row.Value ?? string.Empty));
        }

        console.Write(table);
    }

    /// <summary>
    /// Writes diagnostics as a Spectre.Console table with severity-aware styling.
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
            console.MarkupLine($"[bold]{Escape(title)}[/]");
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[grey]Level[/]")
            .AddColumn("[grey]Code[/]")
            .AddColumn("[grey]Message[/]")
            .AddColumn("[grey]Source[/]")
            .AddColumn("[grey]Location[/]");

        foreach (var entry in entries)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entry.Code);
            ArgumentException.ThrowIfNullOrWhiteSpace(entry.Message);

            var style = entry.Level switch
            {
                ConsoleMessageLevel.Info => "white",
                ConsoleMessageLevel.Warning => "yellow",
                ConsoleMessageLevel.Error => "red",
                _ => "white",
            };

            table.AddRow(
                $"[{style}]{Escape(entry.Level.ToString())}[/]",
                Escape(entry.Code),
                Escape(entry.Message),
                Escape(entry.Source ?? string.Empty),
                Escape(entry.Location ?? string.Empty));
        }

        console.Write(table);
    }

    /// <summary>
    /// Writes one structured activity/event line with stage-aware styling for rich terminals.
    /// </summary>
    /// <inheritdoc/>
    public void WriteActivity(ConsoleActivityEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.Category);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.Stage);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.Operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.Message);

        var levelStyle = entry.Level switch
        {
            ConsoleMessageLevel.Info => "cyan",
            ConsoleMessageLevel.Warning => "yellow",
            ConsoleMessageLevel.Error => "red",
            _ => "white",
        };

        var stageStyle = entry.Stage.Equals("Error", StringComparison.OrdinalIgnoreCase)
            ? "red"
            : entry.Stage.Equals("Exit", StringComparison.OrdinalIgnoreCase)
                ? "green"
                : "blue";

        var correlationId = string.IsNullOrWhiteSpace(entry.CorrelationId) ? "-" : entry.CorrelationId;
        console.MarkupLine(
            $"[{levelStyle}][[{Escape(entry.Category)}]][/] " +
            $"[{stageStyle}]{Escape(entry.Stage)}[/] " +
            $"[grey]op={Escape(entry.Operation)} corr={Escape(correlationId)} elapsedMs={entry.ElapsedMilliseconds}[/] " +
            $"[white]{Escape(entry.Message)}[/]");
    }

    /// <summary>
    /// Escapes Spectre.Console markup characters in <paramref name="value"/> so that they are
    /// rendered as literal text rather than interpreted as markup.
    /// </summary>
    private static string Escape(string value)
    {
        return Markup.Escape(value ?? string.Empty);
    }
}
