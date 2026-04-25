namespace Alloyed.DevOps.Multitool.Core.Decoration.Decorators;

using Alloyed.DevOps.Multitool.Core.Decoration.Contracts;
using Alloyed.DevOps.Multitool.Core.Decoration.Models;

public sealed class CorrelationDecorator : IDecorator
{
    public const string CorrelationIdTag = "correlationId";

    public int Priority => 800;

    public string Name => nameof(CorrelationDecorator);

    public bool Enabled(DecorationContext context)
    {
        _ = context;
        return true;
    }

    public T Execute<T>(DecorationContext context, Func<T> next)
    {
        if (string.IsNullOrWhiteSpace(context.GetTag(CorrelationIdTag)))
        {
            context.SetTag(CorrelationIdTag, Guid.NewGuid().ToString("N"));
        }

        return next();
    }
}
