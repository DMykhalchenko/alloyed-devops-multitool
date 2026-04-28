namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Alloyed.DevOps.Multitool.Host.PowerShell.Contracts;

public static class ConsoleReporterFactory
{
    public static IConsoleReporter Create(ConsoleOutputMode mode, bool isInteractive, TextWriter? writer = null)
    {
        if (mode == ConsoleOutputMode.Rich && isInteractive)
        {
            return new SpectreConsoleReporter();
        }

        return new PlainTextConsoleReporter(writer);
    }
}

public enum ConsoleOutputMode
{
    Plain = 0,
    Rich = 1,
}
