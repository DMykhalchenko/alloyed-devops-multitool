namespace Alloyed.DevOps.Multitool.Core.Decoration.Decorators;

using System.Diagnostics;
using Alloyed.DevOps.Multitool.Core.Decoration.Contracts;
using Alloyed.DevOps.Multitool.Core.Decoration.Models;

public sealed class TransparencyDecorator : IDecorator
{
    public const string EnableTransparencyTag = "enableTransparency";
    public const string VerboseTransparencyTag = "transparencyVerbose";
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
        var verbose = bool.TryParse(context.GetTag(VerboseTransparencyTag), out var value) && value;
        var tagSummary = BuildSanitizedTagSummary(context.Tags, verbose);

        _sink.Write(new DecorationEvent(
            Operation: context.Operation,
            Decorator: Name,
            Stage: DecorationStage.Enter,
            ElapsedMilliseconds: 0,
            CorrelationId: correlationId,
            Message: $"phase=enter {tagSummary}"));

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
                Message: $"phase=exit {tagSummary}"));

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
                Message: $"phase=error exception={ex.GetType().Name} {tagSummary}"));

            throw;
        }
    }

    private static string BuildSanitizedTagSummary(IDictionary<string, string> tags, bool verbose)
    {
        if (tags.Count == 0)
        {
            return "tags=<none>";
        }

        var pairs = tags
            .OrderBy(static t => t.Key, StringComparer.OrdinalIgnoreCase)
            .Where(kv => verbose || IsHighSignalTag(kv.Key))
            .Select(static kv =>
            {
                var value = ShouldRedact(kv.Key) ? RedactedValue : kv.Value;
                return $"{kv.Key}={value}";
            });

        var preview = string.Join(", ", pairs);
        if (string.IsNullOrWhiteSpace(preview))
        {
            preview = "<none>";
        }

        return $"tags.count={tags.Count} tags.preview={preview}";
    }

    private static bool IsHighSignalTag(string key)
    {
        return key.Equals("operation", StringComparison.OrdinalIgnoreCase) ||
               key.Equals(EnableTransparencyTag, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(CorrelationDecorator.CorrelationIdTag, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldRedact(string key)
    {
        return SensitiveKeyMarkers.Any(marker =>
            key.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
