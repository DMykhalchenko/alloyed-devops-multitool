namespace Alloyed.DevOps.Multitool.Core.Decoration.Decorators;

using System.Diagnostics;
using Alloyed.DevOps.Multitool.Core.Decoration.Contracts;
using Alloyed.DevOps.Multitool.Core.Decoration.Models;

public sealed class TransparencyDecorator : IDecorator
{
    public const string EnableTransparencyTag = "enableTransparency";
    private const string RedactedValue = "***REDACTED***";

    private static readonly string[] SensitiveKeyMarkers =
    {
        "password",
        "secret",
        "token",
        "apikey",
        "api_key",
        "credential",
        "authorization",
        "accesskey",
        "privatekey",
    };

    private readonly IDecorationSink _sink;

    public TransparencyDecorator(IDecorationSink? sink = null)
    {
        _sink = sink ?? new Services.NullDecorationSink();
    }

    // Keep it inside Observability to preserve standard enter/exit envelopes.
    public int Priority => 650;

    public string Name => nameof(TransparencyDecorator);

    public bool Enabled(DecorationContext context)
    {
        var raw = context.GetTag(EnableTransparencyTag);
        return bool.TryParse(raw, out var enabled) && enabled;
    }

    public T Execute<T>(DecorationContext context, Func<T> next)
    {
        var correlationId = context.GetTag(CorrelationDecorator.CorrelationIdTag);
        var tagSummary = BuildSanitizedTagSummary(context.Tags);

        _sink.Write(new DecorationEvent(
            Operation: context.Operation,
            Decorator: Name,
            Stage: DecorationStage.Enter,
            ElapsedMilliseconds: 0,
            CorrelationId: correlationId,
            Message: $"watch enter :: {tagSummary}"));

        var sw = Stopwatch.StartNew();
        try
        {
            var result = next();
            sw.Stop();

            _sink.Write(new DecorationEvent(
                Operation: context.Operation,
                Decorator: Name,
                Stage: DecorationStage.Exit,
                ElapsedMilliseconds: sw.ElapsedMilliseconds,
                CorrelationId: correlationId,
                Message: $"watch exit :: {tagSummary}"));

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();

            _sink.Write(new DecorationEvent(
                Operation: context.Operation,
                Decorator: Name,
                Stage: DecorationStage.Error,
                ElapsedMilliseconds: sw.ElapsedMilliseconds,
                CorrelationId: correlationId,
                Message: $"watch error :: {ex.GetType().Name} :: {tagSummary}"));

            throw;
        }
    }

    private static string BuildSanitizedTagSummary(IDictionary<string, string> tags)
    {
        if (tags.Count == 0)
        {
            return "tags=<none>";
        }

        var pairs = tags
            .OrderBy(static t => t.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static kv =>
            {
                var value = ShouldRedact(kv.Key) ? RedactedValue : kv.Value;
                return $"{kv.Key}={value}";
            });

        return $"tags={string.Join(", ", pairs)}";
    }

    private static bool ShouldRedact(string key)
    {
        return SensitiveKeyMarkers.Any(marker =>
            key.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
