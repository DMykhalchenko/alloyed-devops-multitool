namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Contracts;
using Models;

/// <summary>
/// Renders <see cref="PipelineResult"/> objects through an <see cref="IConsoleReporter"/> so
/// PowerShell entry points can stay thin while output formatting evolves in the host layer.
/// </summary>
public static class PipelineResultConsolePresenter
{
    /// <summary>
    /// Writes a user-facing summary for a completed pipeline operation.
    /// </summary>
    /// <param name="reporter">Console reporter selected for the current output mode.</param>
    /// <param name="result">Pipeline result to render.</param>
    /// <param name="operation">Operation name displayed in the header.</param>
    public static void WriteSummary(IConsoleReporter reporter, PipelineResult result, string operation)
    {
        ArgumentNullException.ThrowIfNull(reporter);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        reporter.WriteHeader(operation);

        if (result.Success)
        {
            reporter.WriteMessage(ConsoleMessageLevel.Info, "Pipeline completed successfully.");
        }
        else
        {
            reporter.WriteMessage(ConsoleMessageLevel.Error, "Pipeline failed.");
        }

        var rows = new List<ConsoleKeyValueRow>
        {
            new("CommandsFound", result.CommandsFound.ToString()),
            new("CommandsReplaced", result.CommandsReplaced.ToString()),
            new("MissingCommands", result.MissingCommands.Count.ToString()),
        };

        if (!string.IsNullOrWhiteSpace(result.ModulePath))
        {
            rows.Add(new ConsoleKeyValueRow("ModulePath", result.ModulePath));
        }

        reporter.WriteKeyValueTable("Summary", rows);

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            reporter.WriteMessage(ConsoleMessageLevel.Error, result.ErrorMessage);
        }

        foreach (var diagnostic in result.Diagnostics)
        {
            var level = diagnostic.Severity.ToString() switch
            {
                "Error" => ConsoleMessageLevel.Error,
                "Warning" => ConsoleMessageLevel.Warning,
                _ => ConsoleMessageLevel.Info,
            };

            reporter.WriteMessage(level, $"[{diagnostic.Code}] {diagnostic.Message}");
        }
    }
}
