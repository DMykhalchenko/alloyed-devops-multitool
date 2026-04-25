namespace Alloyed.DevOps.Multitool.Core.Decoration.Decorators;

using System.Diagnostics;
using Alloyed.DevOps.Multitool.Core.Decoration.Contracts;
using Alloyed.DevOps.Multitool.Core.Decoration.Models;

public sealed class ObservabilityDecorator : IDecorator
{
    private readonly IDecorationSink _sink;

    public ObservabilityDecorator(IDecorationSink? sink = null)
    {
        _sink = sink ?? new Services.NullDecorationSink();
    }

    public int Priority => 700;

    public string Name => nameof(ObservabilityDecorator);

    public bool Enabled(DecorationContext context)
    {
        _ = context;
        return true;
    }

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
