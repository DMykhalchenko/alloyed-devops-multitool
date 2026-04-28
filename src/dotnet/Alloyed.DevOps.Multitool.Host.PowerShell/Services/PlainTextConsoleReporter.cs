namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Alloyed.DevOps.Multitool.Host.PowerShell.Contracts;

public sealed class PlainTextConsoleReporter : IConsoleReporter
{
    private readonly TextWriter writer;

    public PlainTextConsoleReporter(TextWriter? writer = null)
    {
        this.writer = writer ?? Console.Out;
    }

    public void WriteHeader(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        writer.WriteLine($"== {title} ==");
    }

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

    public void WriteKeyValue(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        writer.WriteLine($"{key}: {value}");
    }
}

