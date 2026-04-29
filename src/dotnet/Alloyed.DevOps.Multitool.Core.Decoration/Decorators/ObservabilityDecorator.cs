namespace Alloyed.DevOps.Multitool.Core.Decoration.Decorators;

using System.Diagnostics;
using Contracts;
using Models;

/// <summary>
/// A decorator (priority 700) that emits <see cref="DecorationEvent"/> entries at the
/// <see cref="DecorationStage.Enter"/>, <see cref="DecorationStage.Exit"/>, and
/// <see cref="DecorationStage.Error"/> stages of every operation, including wall-clock elapsed
/// milliseconds. Exceptions are re-thrown unmodified after the error event is written; wrapping is
/// the responsibility of <see cref="ErrorHandlingDecorator"/>.
/// </summary>
public sealed class ObservabilityDecorator : IDecorator
{
    private readonly IDecorationSink _sink;

    /// <summary>
    /// Initializes the decorator with an optional <paramref name="sink"/>. Defaults to
    /// <see cref="Services.NullDecorationSink"/> when <see langword="null"/>.
    /// </summary>
    /// <param name="sink">The sink that receives the emitted events.</param>
    public ObservabilityDecorator(IDecorationSink? sink = null)
    {
        _sink = sink ?? new Services.NullDecorationSink();
    }

    /// <inheritdoc/>
    public int Priority => 700;

    /// <inheritdoc/>
    public string Name => nameof(ObservabilityDecorator);

    /// <inheritdoc/>
    public bool Enabled(DecorationContext context)
    {
        _ = context;
        return true;
    }

    /// <summary>
    /// Emits enter/exit/error events around <paramref name="next"/> and measures elapsed time
    /// with a <see cref="Stopwatch"/>.
    /// </summary>
    /// <inheritdoc/>
    public T Execute<T>(DecorationContext context, Func<T> next)
    {
        var correlationId = context.GetTag(CorrelationDecorator.CorrelationIdTag);
        var stopwatch = Stopwatch.StartNew();

        _sink.Write(new DecorationEvent(
            Operation: context.Operation,
            Decorator: Name,
            Stage: DecorationStage.Enter,
            ElapsedMilliseconds: 0,
            CorrelationId: correlationId,
            Message: null));

        try
        {
            var result = next();
            stopwatch.Stop();

            _sink.Write(new DecorationEvent(
                Operation: context.Operation,
                Decorator: Name,
                Stage: DecorationStage.Exit,
                ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
                CorrelationId: correlationId,
                Message: null));

            return result;
        }
        catch
        {
            stopwatch.Stop();

            _sink.Write(new DecorationEvent(
                Operation: context.Operation,
                Decorator: Name,
                Stage: DecorationStage.Error,
                ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
                CorrelationId: correlationId,
                Message: "Execution failed. Delegated to ErrorHandlingDecorator."));

            throw;
        }
    }
}
