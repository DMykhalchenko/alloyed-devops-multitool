namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Alloyed.DevOps.Multitool.Host.PowerShell.Contracts;
using Spectre.Console;

public sealed class SpectreConsoleReporter : IConsoleReporter
{
    private readonly IAnsiConsole console;

    public SpectreConsoleReporter(IAnsiConsole? console = null)
    {
        this.console = console ?? AnsiConsole.Console;
    }

    public void WriteHeader(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        console.MarkupLine($"[bold cyan]== {Escape(title)} ==[/]");
    }

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

    public void WriteKeyValue(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        console.MarkupLine($"[grey]{Escape(key)}:[/] [white]{Escape(value)}[/]");
    }

    private static string Escape(string value)
    {
        return Markup.Escape(value ?? string.Empty);
    }
}

