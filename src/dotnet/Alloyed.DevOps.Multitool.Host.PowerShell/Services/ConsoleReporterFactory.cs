namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Alloyed.DevOps.Multitool.Host.PowerShell.Contracts;

public static class ConsoleReporterFactory
{
    public static IConsoleReporter Create(ConsoleOutputMode mode, bool isInteractive, TextWriter? writer = null)
    {
        // Rich mode will be introduced with Spectre.Console in a follow-up wave.
        if (mode == ConsoleOutputMode.Rich && isInteractive)
        {
            return new PlainTextConsoleReporter(writer);
        }

        return new PlainTextConsoleReporter(writer);
    }
}

public enum ConsoleOutputMode
{
    Plain = 0,
    Rich = 1,
}

