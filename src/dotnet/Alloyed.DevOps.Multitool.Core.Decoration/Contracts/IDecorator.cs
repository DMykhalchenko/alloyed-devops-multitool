namespace Alloyed.DevOps.Multitool.Core.Decoration.Contracts;

using Alloyed.DevOps.Multitool.Core.Decoration.Models;

/// <summary>
/// A single cross-cutting concern that wraps a unit of work. Decorators are composed into a
/// priority-ordered chain by <see cref="IDecorationPipeline"/>. Each decorator receives the
/// context and a delegate to the next step in the chain.
/// </summary>
public interface IDecorator : IDecoratorPolicy
{
    /// <summary>
    /// Wraps the execution of <paramref name="next"/> with the decorator's cross-cutting behavior
    /// (e.g., timing, error handling, correlation ID injection).
    /// </summary>
    /// <typeparam name="T">Return type of the decorated operation.</typeparam>
    /// <param name="context">Shared mutable context for the current operation.</param>
    /// <param name="next">The next step in the decorator chain, ultimately invoking the original action.</param>
    /// <returns>The value returned by <paramref name="next"/>.</returns>
    T Execute<T>(DecorationContext context, Func<T> next);
}
