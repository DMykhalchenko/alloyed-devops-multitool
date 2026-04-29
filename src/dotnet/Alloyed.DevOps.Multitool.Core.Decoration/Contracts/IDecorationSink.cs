namespace Alloyed.DevOps.Multitool.Core.Decoration.Contracts;

using Models;

/// <summary>
/// Receives <see cref="DecorationEvent"/> entries emitted by decorators during execution.
/// Implementations may write to the console, a structured log, a telemetry backend, or discard
/// the events entirely (see <see cref="Services.NullDecorationSink"/>).
/// </summary>
public interface IDecorationSink
{
    /// <summary>
    /// Writes a single decoration event. Implementations must not throw; exceptions from sinks
    /// would interfere with the decorated operation itself.
    /// </summary>
    /// <param name="entry">The event to record.</param>
    void Write(DecorationEvent entry);
}
