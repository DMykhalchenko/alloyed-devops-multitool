namespace Alloyed.DevOps.Multitool.Core.Decoration.Services;

using Alloyed.DevOps.Multitool.Core.Decoration.Contracts;
using Alloyed.DevOps.Multitool.Core.Decoration.Models;

public sealed class DecorationPipeline : IDecorationPipeline
{
    private readonly IReadOnlyList<IDecorator> _decorators;

    public DecorationPipeline(IEnumerable<IDecorator> decorators)
    {
        ArgumentNullException.ThrowIfNull(decorators);

        _decorators = decorators
            .OrderByDescending(static d => d.Priority)
            .ThenBy(static d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public T Execute<T>(DecorationContext context, Func<T> action)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(action);

        var activeDecorators = _decorators
            .Where(d => d.Enabled(context))
            .ToList();

        Func<T> pipeline = action;

        // Compose in reverse order so highest priority stays outermost.
        for (var i = activeDecorators.Count - 1; i >= 0; i--)
        {
            var decorator = activeDecorators[i];
            var next = pipeline;
            pipeline = () => decorator.Execute(context, next);
        }

        return pipeline();
    }
}
