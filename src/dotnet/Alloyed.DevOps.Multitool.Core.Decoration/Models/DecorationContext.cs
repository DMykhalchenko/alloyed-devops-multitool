namespace Alloyed.DevOps.Multitool.Core.Decoration.Models;

/// <summary>
/// Mutable context object shared across all decorators in a single pipeline execution. It carries
/// the logical operation name and an open-ended tag dictionary that decorators use to pass data
/// to each other (e.g., a correlation ID set by <see cref="Decorators.CorrelationDecorator"/>).
/// </summary>
public sealed class DecorationContext
{
    /// <summary>
    /// Initializes a new context for the named <paramref name="operation"/>.
    /// </summary>
    /// <param name="operation">
    /// Human-readable name of the operation being executed. Defaults to <c>"unknown"</c> when
    /// null or whitespace.
    /// </param>
    /// <param name="tags">
    /// Optional seed tags. Copied into an internal case-insensitive dictionary; the original
    /// dictionary is not mutated.
    /// </param>
    public DecorationContext(string operation, IDictionary<string, string>? tags = null)
    {
        Operation = string.IsNullOrWhiteSpace(operation) ? "unknown" : operation;
        Tags = tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(tags, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Human-readable name of the operation being executed (e.g. <c>"TransformPipeline"</c>).
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Open-ended, case-insensitive key/value store. Decorators read and write tags here to
    /// coordinate (e.g., correlation ID, transparency flags).
    /// </summary>
    public IDictionary<string, string> Tags { get; }

    /// <summary>
    /// Returns the value for <paramref name="key"/>, or <see langword="null"/> when the key is
    /// not present.
    /// </summary>
    /// <param name="key">Tag key (case-insensitive).</param>
    public string? GetTag(string key)
    {
        return Tags.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Sets <paramref name="key"/> to <paramref name="value"/> in the tag dictionary.
    /// Silently ignores calls where <paramref name="key"/> is null or whitespace.
    /// </summary>
    /// <param name="key">Tag key (case-insensitive).</param>
    /// <param name="value">Value to store.</param>
    public void SetTag(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        Tags[key] = value;
    }
}
