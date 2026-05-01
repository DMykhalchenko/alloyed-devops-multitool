namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Contracts;

/// <summary>
/// Renders user-facing session lifecycle output through an <see cref="IConsoleReporter"/>.
/// </summary>
public static class SessionConsolePresenter
{
    /// <summary>
    /// Writes a banner describing the current Alloyed session state and suggested next steps.
    /// </summary>
    public static void WriteSessionReady(
        IConsoleReporter reporter,
        bool transparencyEnabled,
        bool sessionModeEnabled,
        string profile,
        string outputMode)
    {
        ArgumentNullException.ThrowIfNull(reporter);

        reporter.WriteHeader("Alloyed session is ready");
        reporter.WriteMessage(ConsoleMessageLevel.Info, "Transparency and session interception are configured for the current shell.");
        reporter.WriteKeyValueTable(
            "Session status",
            new[]
            {
                new ConsoleKeyValueRow("Transparency", transparencyEnabled.ToString()),
                new ConsoleKeyValueRow("SessionMode", sessionModeEnabled.ToString()),
                new ConsoleKeyValueRow("Profile", profile ?? string.Empty),
                new ConsoleKeyValueRow("OutputMode", outputMode ?? string.Empty),
            });

        reporter.WriteMessage(ConsoleMessageLevel.Info, "Next: run your script, check Get-AlloyedTransparencyModeStatus, or stop interception when needed.");
    }

    /// <summary>
    /// Writes a banner confirming that the current Alloyed session has been stopped.
    /// </summary>
    public static void WriteSessionStopped(IConsoleReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(reporter);

        reporter.WriteHeader("Alloyed session stopped");
        reporter.WriteMessage(ConsoleMessageLevel.Info, "Transparency mode and session interception are now disabled.");
    }
}
