namespace Alloyed.DevOps.Multitool.Core.Decoration.Models;

/// <summary>
/// Wraps an exception thrown during a decorated operation, enriching it with the operation name
/// and correlation ID from the active <see cref="DecorationContext"/>. Thrown by
/// <see cref="Decorators.ErrorHandlingDecorator"/> and re-thrown as-is by subsequent decorators
/// to prevent double-wrapping.
/// </summary>
public sealed class DecorationExecutionException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="DecorationExecutionException"/>.
    /// </summary>
    /// <param name="message">Human-readable description of the failure.</param>
    /// <param name="operation">
    /// Name of the operation that failed (from <see cref="DecorationContext.Operation"/>).
    /// </param>
    /// <param name="correlationId">
    /// Correlation identifier active at the time of failure, or <see langword="null"/> when
    /// <see cref="Decorators.CorrelationDecorator"/> was not in the pipeline.
    /// </param>
    /// <param name="innerException">The original exception that caused the failure.</param>
    public DecorationExecutionException(string message, string operation, string? correlationId, Exception innerException)
        : base(message, innerException)
    {
        Operation = operation;
        CorrelationId = correlationId;
    }

    /// <summary>Name of the operation that failed.</summary>
    public string Operation { get; }

    /// <summary>
    /// Correlation identifier active at the time of failure, or <see langword="null"/> when not available.
    /// </summary>
    public string? CorrelationId { get; }
}
