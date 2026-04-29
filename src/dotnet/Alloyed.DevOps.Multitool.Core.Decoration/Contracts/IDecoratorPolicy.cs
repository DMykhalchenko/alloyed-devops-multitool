namespace Alloyed.DevOps.Multitool.Core.Decoration.Contracts;

using Models;

/// <summary>
/// Defines the scheduling and activation contract for a decorator.
/// Implementations that also perform work should implement <see cref="IDecorator"/>.
/// </summary>
public interface IDecoratorPolicy
{
    /// <summary>
    /// Execution order relative to other decorators. Higher values run outermost (first on enter,
    /// last on exit). The built-in decorators use: ErrorHandling=1000, Correlation=800,
    /// Observability=700, Transparency=650.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Human-readable name used in log output and diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Returns <see langword="true"/> when this decorator should participate in the execution of
    /// the supplied <paramref name="context"/>. Called by <see cref="IDecorationPipeline"/> once
    /// per execution to build the active decorator chain.
    /// </summary>
    /// <param name="context">The current operation context, including any tags set by prior decorators.</param>
    bool Enabled(DecorationContext context);
}
