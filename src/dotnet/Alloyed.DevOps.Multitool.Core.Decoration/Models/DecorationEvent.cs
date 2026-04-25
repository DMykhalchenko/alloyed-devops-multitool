namespace Alloyed.DevOps.Multitool.Core.Decoration.Models;

public sealed record DecorationEvent(
    string Operation,
    string Decorator,
    DecorationStage Stage,
    long ElapsedMilliseconds,
    string? CorrelationId,
    string? Message
);
