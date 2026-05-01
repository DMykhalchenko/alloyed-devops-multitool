namespace Alloyed.DevOps.Multitool.Host.PowerShell.Contracts;

/// <summary>
/// Writes human-readable pipeline progress and results to the console. Implementations may render
/// plain text or rich markup (e.g. via Spectre.Console).
/// </summary>
public interface IConsoleReporter
{
    /// <summary>
    /// Writes a section header line (e.g. a banner or separator) using <paramref name="title"/>.
    /// </summary>
    /// <param name="title">Non-empty header text.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="title"/> is null or whitespace.</exception>
    void WriteHeader(string title);

    /// <summary>
    /// Writes a message at the specified <paramref name="level"/>. Implementations typically
    /// colour-code or prefix the output based on severity.
    /// </summary>
    /// <param name="level">Severity level that controls styling or prefix.</param>
    /// <param name="message">Non-empty message text.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="message"/> is null or whitespace.</exception>
    void WriteMessage(ConsoleMessageLevel level, string message);

    /// <summary>
    /// Writes a single key/value pair (e.g. <c>Module: MyModule</c>).
    /// </summary>
    /// <param name="key">Non-empty label.</param>
    /// <param name="value">Associated value, may be empty.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="key"/> is null or whitespace.</exception>
    void WriteKeyValue(string key, string value);

    /// <summary>
    /// Writes a structured key/value block. Rich renderers may display this as a table, while plain
    /// renderers can fall back to aligned text lines.
    /// </summary>
    /// <param name="title">Optional section title shown above the block.</param>
    /// <param name="rows">Ordered key/value rows to render.</param>
    void WriteKeyValueTable(string? title, IReadOnlyList<ConsoleKeyValueRow> rows);

    /// <summary>
    /// Writes a structured diagnostics block. Rich renderers may display this as a table, while
    /// plain renderers can fall back to readable text lines.
    /// </summary>
    /// <param name="title">Optional section title shown above the diagnostics block.</param>
    /// <param name="entries">Ordered diagnostics to render.</param>
    void WriteDiagnostics(string? title, IReadOnlyList<ConsoleDiagnosticEntry> entries);

    /// <summary>
    /// Writes one structured activity/event line. Rich renderers may use stage-aware styling while
    /// plain renderers can fall back to a readable single-line message.
    /// </summary>
    /// <param name="entry">Structured activity/event details.</param>
    void WriteActivity(ConsoleActivityEntry entry);
}

/// <summary>
/// Represents one row in a structured console key/value block.
/// </summary>
/// <param name="Key">Human-readable row label.</param>
/// <param name="Value">Associated row value.</param>
public sealed record ConsoleKeyValueRow(string Key, string Value);

/// <summary>
/// Represents one structured diagnostic entry for console reporting.
/// </summary>
/// <param name="Level">Severity level that controls styling.</param>
/// <param name="Code">Short machine-readable diagnostic code.</param>
/// <param name="Message">Human-readable diagnostic message.</param>
/// <param name="Source">Optional pipeline/source label.</param>
/// <param name="Location">Optional source location text.</param>
public sealed record ConsoleDiagnosticEntry(
    ConsoleMessageLevel Level,
    string Code,
    string Message,
    string? Source = null,
    string? Location = null);

/// <summary>
/// Represents one structured activity/event entry for host-side console rendering.
/// </summary>
/// <param name="Level">Severity level that controls styling.</param>
/// <param name="Category">High-level source/category label.</param>
/// <param name="Stage">Lifecycle stage label.</param>
/// <param name="Operation">Operation name associated with the event.</param>
/// <param name="CorrelationId">Optional correlation identifier.</param>
/// <param name="ElapsedMilliseconds">Elapsed wall-clock time in milliseconds.</param>
/// <param name="Message">Human-readable detail message.</param>
public sealed record ConsoleActivityEntry(
    ConsoleMessageLevel Level,
    string Category,
    string Stage,
    string Operation,
    string? CorrelationId,
    long ElapsedMilliseconds,
    string Message);

/// <summary>
/// Severity level passed to <see cref="IConsoleReporter.WriteMessage"/> to control output styling.
/// </summary>
public enum ConsoleMessageLevel
{
    /// <summary>Informational output; no user action required.</summary>
    Info = 0,

    /// <summary>A non-fatal condition that deserves attention.</summary>
    Warning = 1,

    /// <summary>A failure or critical issue.</summary>
    Error = 2,
}
