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

        var diagnostics = new List<ConsoleDiagnosticEntry>();
        foreach (var diagnostic in result.Diagnostics)
        {
            var level = diagnostic.Severity.ToString() switch
            {
                "Error" => ConsoleMessageLevel.Error,
                "Warning" => ConsoleMessageLevel.Warning,
                _ => ConsoleMessageLevel.Info,
            };

            var location = diagnostic.Line > 0
                ? diagnostic.Column > 0
                    ? $"Line {diagnostic.Line}, Col {diagnostic.Column}"
                    : $"Line {diagnostic.Line}"
                : string.Empty;

            diagnostics.Add(
                new ConsoleDiagnosticEntry(
                    level,
                    diagnostic.Code,
                    diagnostic.Message,
                    diagnostic.Source,
                    location));
        }

        reporter.WriteDiagnostics("Diagnostics", diagnostics);
        if (diagnostics.Count == 0 && result.MissingCommands.Count > 0)
        {
            var missingCommandEntries = result.MissingCommands
                .Select(command => new ConsoleDiagnosticEntry(
                    ConsoleMessageLevel.Warning,
                    "CATALOG-MISS",
                    $"No wrapper mapping found for '{command}'.",
                    "catalog"))
                .ToList();

            reporter.WriteDiagnostics("Missing command mappings", missingCommandEntries);
        }
    }
}
