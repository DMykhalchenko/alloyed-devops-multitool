namespace Alloyed.DevOps.Multitool.Core.Decoration.Decorators;

using Alloyed.DevOps.Multitool.Core.Decoration.Contracts;
using Alloyed.DevOps.Multitool.Core.Decoration.Models;

/// <summary>
/// A decorator (priority 1000, outermost by default) that catches any unhandled exception from
/// the inner pipeline and re-throws it as a <see cref="DecorationExecutionException"/> enriched
/// with the operation name and current correlation ID. Already-wrapped
/// <see cref="DecorationExecutionException"/> instances are re-thrown as-is to prevent
/// double-wrapping.
/// </summary>
public sealed class ErrorHandlingDecorator : IDecorator
{
    /// <inheritdoc/>
    public int Priority => 1000;

    /// <inheritdoc/>
    public string Name => nameof(ErrorHandlingDecorator);

    /// <inheritdoc/>
    public bool Enabled(DecorationContext context)
    {
        _ = context;
        return true;
    }

    /// <summary>
    /// Delegates to <paramref name="next"/> and wraps any unexpected exception in a
    /// <see cref="DecorationExecutionException"/>.
    /// </summary>
    /// <inheritdoc/>
    public T Execute<T>(DecorationContext context, Func<T> next)
    {
        try
        {
            return next();
        }
        catch (DecorationExecutionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var correlationId = context.GetTag(CorrelationDecorator.CorrelationIdTag);
            throw new DecorationExecutionException(
                message: $"Decorated execution failed for operation '{context.Operation}'.",
                operation: context.Operation,
                correlationId: correlationId,
                innerException: ex);
        }
    }
}
