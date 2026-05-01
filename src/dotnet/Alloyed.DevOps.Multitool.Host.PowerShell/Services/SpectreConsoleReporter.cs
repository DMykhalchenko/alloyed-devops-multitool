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
    /// Writes a left-justified cyan <see cref="Rule"/> as a section header.
    /// </summary>
    /// <inheritdoc/>
    public void WriteHeader(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        console.Write(new Rule($"[bold cyan]{Escape(title)}[/]")
            .RuleStyle(new Style(Color.Cyan1, decoration: Decoration.Dim))
            .LeftJustified());
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
    /// Renders key/value rows as a borderless two-column table inside a rounded <see cref="Panel"/>
    /// when a title is present, or as a plain table when no title is given.
    /// </summary>
    /// <inheritdoc/>
    public void WriteKeyValueTable(string? title, IReadOnlyList<ConsoleKeyValueRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (rows.Count == 0)
        {
            return;
        }

        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn(string.Empty).NoWrap())
            .AddColumn(new TableColumn(string.Empty));

        foreach (var row in rows)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(row.Key);
            table.AddRow(
                new Markup($"[grey]{Escape(row.Key)}[/]"),
                new Markup($"[white]{Escape(row.Value)}[/]"));
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            console.Write(new Panel(table)
                .Header(new PanelHeader($"[grey] {Escape(title)} [/]", Justify.Left))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey));
        }
        else
        {
            console.Write(table);
        }
    }

    /// <summary>
    /// Writes diagnostics as a Spectre.Console table with severity-aware styling, wrapped in a
    /// rounded <see cref="Panel"/> when a title is present.
    /// </summary>
    /// <inheritdoc/>
    public void WriteDiagnostics(string? title, IReadOnlyList<ConsoleDiagnosticEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count == 0)
        {
            return;
        }

        var table = new Table()
            .Border(TableBorder.Simple)
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

        if (!string.IsNullOrWhiteSpace(title))
        {
            console.Write(new Panel(table)
                .Header(new PanelHeader($"[grey] {Escape(title)} [/]", Justify.Left))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey));
        }
        else
        {
            console.Write(table);
        }
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
        console.Write(new ConsoleActivityRenderable(entry));
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
