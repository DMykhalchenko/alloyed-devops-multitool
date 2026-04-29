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
    /// Writes <c>== title ==</c> to the output writer.
    /// </summary>
    /// <inheritdoc/>
    public void WriteHeader(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        writer.WriteLine($"== {title} ==");
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
}
