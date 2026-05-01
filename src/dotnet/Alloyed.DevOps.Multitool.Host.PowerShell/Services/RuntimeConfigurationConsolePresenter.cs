namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Contracts;

/// <summary>
/// Renders runtime configuration initialization and validation output through an
/// <see cref="IConsoleReporter"/> so PowerShell commands do not own presentation details.
/// </summary>
public static class RuntimeConfigurationConsolePresenter
{
    /// <summary>
    /// Writes a summary for a freshly initialized runtime configuration.
    /// </summary>
    public static void WriteInitializationSummary(
        IConsoleReporter reporter,
        string configPath,
        string outputMode,
        bool enableTransparency,
        string transparencyProfile,
        bool sessionEnabled,
        int runtimeMaxRetries,
        bool runtimeExponentialBackoff,
        bool runtimePreview,
        bool applyToCurrentSession)
    {
        ArgumentNullException.ThrowIfNull(reporter);
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);

        reporter.WriteHeader("Alloyed runtime config initialized");
        reporter.WriteMessage(ConsoleMessageLevel.Info, "Runtime configuration defaults were written successfully.");
        reporter.WriteKeyValueTable(
            "Config summary",
            new[]
            {
                new ConsoleKeyValueRow("ConfigPath", configPath),
                new ConsoleKeyValueRow("OutputMode", outputMode ?? string.Empty),
                new ConsoleKeyValueRow("EnableTransparency", enableTransparency.ToString()),
                new ConsoleKeyValueRow("TransparencyProfile", transparencyProfile ?? string.Empty),
                new ConsoleKeyValueRow("SessionEnabled", sessionEnabled.ToString()),
                new ConsoleKeyValueRow("RuntimeMaxRetries", runtimeMaxRetries.ToString()),
                new ConsoleKeyValueRow("RuntimeExponentialBackoff", runtimeExponentialBackoff.ToString()),
                new ConsoleKeyValueRow("RuntimePreview", runtimePreview.ToString()),
                new ConsoleKeyValueRow("ApplyToCurrentSession", applyToCurrentSession.ToString()),
            });
    }

    /// <summary>
    /// Writes a summary of the merged runtime configuration and effective execution policy.
    /// </summary>
    public static void WriteValidationSummary(
        IConsoleReporter reporter,
        string basePath,
        string configPath,
        string runtimeDefaultOutputPath,
        bool sessionEnabled,
        bool transparencyEnabled,
        string consoleOutputMode,
        int runtimeMaxRetries,
        int runtimeRetryDelaySec,
        bool runtimeExponentialBackoff,
        bool runtimePreview,
        int runtimeTimeoutSec)
    {
        ArgumentNullException.ThrowIfNull(reporter);

        reporter.WriteHeader("Alloyed runtime config validation");
        reporter.WriteMessage(ConsoleMessageLevel.Info, "Merged runtime configuration and execution policy were resolved successfully.");
        reporter.WriteKeyValueTable(
            "Effective runtime",
            new[]
            {
                new ConsoleKeyValueRow("BasePath", basePath ?? string.Empty),
                new ConsoleKeyValueRow("ConfigPath", configPath ?? string.Empty),
                new ConsoleKeyValueRow("RuntimeDefaultOutputPath", runtimeDefaultOutputPath ?? string.Empty),
                new ConsoleKeyValueRow("SessionEnabled", sessionEnabled.ToString()),
                new ConsoleKeyValueRow("TransparencyEnabled", transparencyEnabled.ToString()),
                new ConsoleKeyValueRow("ConsoleOutputMode", consoleOutputMode ?? string.Empty),
                new ConsoleKeyValueRow("RuntimeMaxRetries", runtimeMaxRetries.ToString()),
                new ConsoleKeyValueRow("RuntimeRetryDelaySec", runtimeRetryDelaySec.ToString()),
                new ConsoleKeyValueRow("RuntimeExponentialBackoff", runtimeExponentialBackoff.ToString()),
                new ConsoleKeyValueRow("RuntimePreview", runtimePreview.ToString()),
                new ConsoleKeyValueRow("RuntimeTimeoutSec", runtimeTimeoutSec.ToString()),
            });
    }
}
