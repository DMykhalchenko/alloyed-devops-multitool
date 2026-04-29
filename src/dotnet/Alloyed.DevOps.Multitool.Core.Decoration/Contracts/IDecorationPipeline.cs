namespace Alloyed.DevOps.Multitool.Core.Decoration.Contracts;

using Models;

/// <summary>
/// Composes a set of <see cref="IDecorator"/> instances into a single execution chain and invokes
/// them around a given action.
/// </summary>
public interface IDecorationPipeline
{
    /// <summary>
    /// Executes <paramref name="action"/> wrapped by all decorators that are enabled for
    /// <paramref name="context"/>, ordered from highest to lowest <see cref="IDecoratorPolicy.Priority"/>.
    /// </summary>
    /// <typeparam name="T">Return type of the decorated operation.</typeparam>
    /// <param name="context">Shared mutable context passed to every decorator.</param>
    /// <param name="action">The operation to execute inside the decorator chain.</param>
    /// <returns>The value returned by <paramref name="action"/>.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="context"/> or <paramref name="action"/> is <see langword="null"/>.
    /// </exception>
    T Execute<T>(DecorationContext context, Func<T> action);
}
