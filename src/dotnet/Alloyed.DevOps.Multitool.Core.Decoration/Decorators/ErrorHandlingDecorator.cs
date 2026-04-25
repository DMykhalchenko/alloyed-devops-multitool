namespace Alloyed.DevOps.Multitool.Core.Decoration.Decorators;

using Alloyed.DevOps.Multitool.Core.Decoration.Contracts;
using Alloyed.DevOps.Multitool.Core.Decoration.Models;

public sealed class ErrorHandlingDecorator : IDecorator
{
    public int Priority => 1000;

    public string Name => nameof(ErrorHandlingDecorator);

    public bool Enabled(DecorationContext context)
    {
        _ = context;
        return true;
    }

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
