namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Contracts;

/// <summary>
/// Creates the appropriate <see cref="IConsoleReporter"/> based on the requested output mode and
/// whether the current session is interactive. Rich output requires both <see cref="ConsoleOutputMode.Rich"/>
/// and <paramref name="isInteractive"/> to be <see langword="true"/>; otherwise plain text is used.
/// </summary>
public static class ConsoleReporterFactory
{
    /// <summary>
    /// Returns a <see cref="SpectreConsoleReporter"/> when <paramref name="mode"/> is
    /// <see cref="ConsoleOutputMode.Rich"/> and <paramref name="isInteractive"/> is
    /// <see langword="true"/>; otherwise returns a <see cref="PlainTextConsoleReporter"/>.
    /// </summary>
    /// <param name="mode">Requested output rendering mode.</param>
    /// <param name="isInteractive">
    /// <see langword="true"/> when the process is attached to an interactive terminal.
    /// </param>
    /// <param name="writer">
    /// Optional <see cref="TextWriter"/> forwarded to <see cref="PlainTextConsoleReporter"/>.
    /// Ignored when a <see cref="SpectreConsoleReporter"/> is created.
    /// </param>
    public static IConsoleReporter Create(ConsoleOutputMode mode, bool isInteractive, TextWriter? writer = null)
    {
        if (mode == ConsoleOutputMode.Rich && isInteractive)
        {
            return new SpectreConsoleReporter();
        }

        return new PlainTextConsoleReporter(writer);
    }
}

/// <summary>
/// Selects the rendering back-end used by <see cref="ConsoleReporterFactory.Create"/>.
/// </summary>
public enum ConsoleOutputMode
{
    /// <summary>Uncoloured plain-text output, safe in any environment.</summary>
    Plain = 0,

    /// <summary>
    /// Rich ANSI-coloured output via Spectre.Console; falls back to plain text when the
    /// session is non-interactive.
    /// </summary>
    Rich = 1,
}
