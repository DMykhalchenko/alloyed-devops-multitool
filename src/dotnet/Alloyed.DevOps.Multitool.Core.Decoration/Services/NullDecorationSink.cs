namespace Alloyed.DevOps.Multitool.Core.Decoration.Services;

using Contracts;
using Models;

/// <summary>
/// A no-op <see cref="IDecorationSink"/> that silently discards all <see cref="DecorationEvent"/>
/// entries. Used as the default sink when no explicit sink is configured, ensuring that decorators
/// can always write events without null checks.
/// </summary>
public sealed class NullDecorationSink : IDecorationSink
{
    /// <inheritdoc/>
    public void Write(DecorationEvent entry)
    {
        _ = entry;
    }
}
