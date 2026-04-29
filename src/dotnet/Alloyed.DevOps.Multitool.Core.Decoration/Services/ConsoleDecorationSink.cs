namespace Alloyed.DevOps.Multitool.Core.Decoration.Services;

using Alloyed.DevOps.Multitool.Core.Decoration.Contracts;
using Alloyed.DevOps.Multitool.Core.Decoration.Models;

/// <summary>
/// An <see cref="IDecorationSink"/> that writes each <see cref="DecorationEvent"/> as a single
/// structured line to <see cref="Console.Out"/> in the format:
/// <c>[timestamp] decorator stage op=... corr=... elapsedMs=... msg=...</c>.
/// </summary>
public sealed class ConsoleDecorationSink : IDecorationSink
{
    /// <summary>
    /// Formats <paramref name="event"/> and writes it to <see cref="Console.Out"/>.
    /// </summary>
    /// <param name="event">The decoration event to record.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="event"/> is <see langword="null"/>.</exception>
    public void Write(DecorationEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        Console.WriteLine(
            "[{0}] {1} {2} op={3} corr={4} elapsedMs={5} msg={6}",
            DateTimeOffset.UtcNow.ToString("O"),
            @event.Decorator,
            @event.Stage,
            @event.Operation,
            @event.CorrelationId ?? "-",
            @event.ElapsedMilliseconds,
            @event.Message ?? "-");
    }
}
