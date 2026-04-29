namespace Alloyed.DevOps.Multitool.Core.Decoration.Decorators;

using Alloyed.DevOps.Multitool.Core.Decoration.Contracts;
using Alloyed.DevOps.Multitool.Core.Decoration.Models;

/// <summary>
/// A decorator (priority 800) that ensures every operation has a unique correlation ID stored in
/// <see cref="DecorationContext.Tags"/> under the key <see cref="CorrelationIdTag"/>. If the tag
/// is already set by the caller, the existing value is preserved so that cross-service correlation
/// works correctly.
/// </summary>
public sealed class CorrelationDecorator : IDecorator
{
    /// <summary>
    /// The tag key under which the correlation ID is stored in <see cref="DecorationContext.Tags"/>.
    /// Other decorators (e.g. <see cref="ObservabilityDecorator"/>) read this key to include the
    /// ID in their log output.
    /// </summary>
    public const string CorrelationIdTag = "correlationId";

    /// <inheritdoc/>
    public int Priority => 800;

    /// <inheritdoc/>
    public string Name => nameof(CorrelationDecorator);

    /// <inheritdoc/>
    public bool Enabled(DecorationContext context)
    {
        _ = context;
        return true;
    }

    /// <summary>
    /// Injects a new <see cref="Guid"/>-based correlation ID into <paramref name="context"/> when
    /// one is not already present, then delegates to <paramref name="next"/>.
    /// </summary>
    /// <inheritdoc/>
    public T Execute<T>(DecorationContext context, Func<T> next)
    {
        if (string.IsNullOrWhiteSpace(context.GetTag(CorrelationIdTag)))
        {
            context.SetTag(CorrelationIdTag, Guid.NewGuid().ToString("N"));
        }

        return next();
    }
}
