namespace Alloyed.DevOps.Multitool.Core.Decoration.Models;

public sealed class DecorationExecutionException : Exception
{
    public DecorationExecutionException(string message, string operation, string? correlationId, Exception innerException)
        : base(message, innerException)
    {
        Operation = operation;
        CorrelationId = correlationId;
    }

    public string Operation { get; }

    public string? CorrelationId { get; }
}
