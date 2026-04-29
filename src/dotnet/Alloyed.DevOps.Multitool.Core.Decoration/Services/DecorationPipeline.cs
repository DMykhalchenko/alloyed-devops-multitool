namespace Alloyed.DevOps.Multitool.Core.Decoration.Services;

using Contracts;
using Models;

/// <summary>
/// The default <see cref="IDecorationPipeline"/> implementation. At construction time it sorts the
/// supplied decorators by descending <see cref="IDecoratorPolicy.Priority"/> (ties broken
/// alphabetically by name). At execution time it filters to the enabled subset and composes them
/// into a nested delegate chain so that the highest-priority decorator is always outermost.
/// </summary>
public sealed class DecorationPipeline : IDecorationPipeline
{
    private readonly IReadOnlyList<IDecorator> _decorators;

    /// <summary>
    /// Initializes a new pipeline from the supplied <paramref name="decorators"/>.
    /// </summary>
    /// <param name="decorators">
    /// The full set of available decorators. Sorted once at construction; <see langword="null"/>
    /// elements are not permitted.
    /// </param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="decorators"/> is <see langword="null"/>.</exception>
    public DecorationPipeline(IEnumerable<IDecorator> decorators)
    {
        ArgumentNullException.ThrowIfNull(decorators);

        _decorators = decorators
            .OrderByDescending(static d => d.Priority)
            .ThenBy(static d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Executes <paramref name="action"/> wrapped by all decorators that return
    /// <see langword="true"/> from <see cref="IDecoratorPolicy.Enabled"/> for the given
    /// <paramref name="context"/>. Decorators are composed in reverse order so the highest-priority
    /// decorator remains outermost.
    /// </summary>
    /// <typeparam name="T">Return type of the decorated operation.</typeparam>
    /// <param name="context">Shared mutable context for the operation.</param>
    /// <param name="action">The operation to execute inside the chain.</param>
    /// <returns>The value returned by <paramref name="action"/>.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="context"/> or <paramref name="action"/> is <see langword="null"/>.
    /// </exception>
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
