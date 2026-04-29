namespace Alloyed.DevOps.Multitool.Core.Decoration.Models;

/// <summary>
/// An immutable record describing a single observation emitted by a decorator to an
/// <see cref="Contracts.IDecorationSink"/>.
/// </summary>
/// <param name="Operation">Name of the operation being decorated (from <see cref="DecorationContext.Operation"/>).</param>
/// <param name="Decorator">Name of the decorator that emitted the event (from <see cref="Contracts.IDecoratorPolicy.Name"/>).</param>
/// <param name="Stage">Lifecycle phase at which the event was emitted.</param>
/// <param name="ElapsedMilliseconds">
/// Wall-clock milliseconds elapsed since the decorator began executing. Zero at the
/// <see cref="DecorationStage.Enter"/> stage.
/// </param>
/// <param name="CorrelationId">
/// Correlation identifier propagated by <see cref="Decorators.CorrelationDecorator"/>, or
/// <see langword="null"/> when correlation is not active.
/// </param>
/// <param name="Message">Optional human-readable detail string, or <see langword="null"/> when not applicable.</param>
public sealed record DecorationEvent(
    string Operation,
    string Decorator,
    DecorationStage Stage,
    long ElapsedMilliseconds,
    string? CorrelationId,
    string? Message
);
