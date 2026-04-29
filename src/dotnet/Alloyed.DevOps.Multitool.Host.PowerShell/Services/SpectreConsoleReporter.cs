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
    /// Escapes Spectre.Console markup characters in <paramref name="value"/> so that they are
    /// rendered as literal text rather than interpreted as markup.
    /// </summary>
    private static string Escape(string value)
    {
        return Markup.Escape(value ?? string.Empty);
    }
}
