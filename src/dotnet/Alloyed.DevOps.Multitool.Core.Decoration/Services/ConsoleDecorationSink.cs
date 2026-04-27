namespace Alloyed.DevOps.Multitool.Core.Decoration.Services;

using Alloyed.DevOps.Multitool.Core.Decoration.Contracts;
using Alloyed.DevOps.Multitool.Core.Decoration.Models;

public sealed class ConsoleDecorationSink : IDecorationSink
{
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
