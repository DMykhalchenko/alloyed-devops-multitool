namespace Alloyed.DevOps.Multitool.Core.Decoration.Decorators;

using System.Diagnostics;
using Alloyed.DevOps.Multitool.Core.Decoration.Contracts;
using Alloyed.DevOps.Multitool.Core.Decoration.Models;

public sealed class TransparencyDecorator : IDecorator
{
    public const string EnableTransparencyTag = "enableTransparency";
    public const string VerboseTransparencyTag = "transparencyVerbose";
    public const string ProfileTransparencyTag = "transparencyProfile";
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
        var profile = ResolveProfile(context.GetTag(ProfileTransparencyTag), verbose);
        var tagSummary = BuildSanitizedTagSummary(context.Tags, profile);

        _sink.Write(new DecorationEvent(
            Operation: context.Operation,
            Decorator: Name,
            Stage: DecorationStage.Enter,
            ElapsedMilliseconds: 0,
            CorrelationId: correlationId,
            Message: BuildMessage("enter", context.Operation, correlationId, tagSummary, null, profile)));

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
                Message: BuildMessage("exit", context.Operation, correlationId, tagSummary, null, profile)));

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
                Message: BuildMessage("error", context.Operation, correlationId, tagSummary, ex.GetType().Name, profile)));

            throw;
        }
    }

    private static string BuildSanitizedTagSummary(IDictionary<string, string> tags, string profile)
    {
        if (tags.Count == 0)
        {
            return "tags=<none>";
        }

        var pairs = tags
            .OrderBy(static t => t.Key, StringComparer.OrdinalIgnoreCase)
            .Where(kv => ShouldIncludeTag(kv.Key, profile))
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

    private static string BuildMessage(string phase, string operation, string? correlationId, string tagSummary, string? exception, string profile)
    {
        return profile switch
        {
            "minimal" => exception is null
                ? $"[{phase}] {operation}"
                : $"[{phase}] {operation} ex={exception}",
            "debug" => exception is null
                ? $"phase={phase} op={operation} corr={correlationId ?? "-"} profile={profile} {tagSummary}"
                : $"phase={phase} op={operation} corr={correlationId ?? "-"} profile={profile} exception={exception} {tagSummary}",
            _ => exception is null
                ? $"phase={phase} op={operation} corr={correlationId ?? "-"} profile={profile} {tagSummary}"
                : $"phase={phase} op={operation} corr={correlationId ?? "-"} profile={profile} exception={exception} {tagSummary}",
        };
    }

    private static bool ShouldIncludeTag(string key, string profile)
    {
        return profile switch
        {
            "minimal" => false,
            "standard" => IsHighSignalTag(key),
            "debug" => true,
            _ => IsHighSignalTag(key),
        };
    }

    private static string ResolveProfile(string? rawProfile, bool verbose)
    {
        if (string.IsNullOrWhiteSpace(rawProfile))
        {
            return verbose ? "debug" : "standard";
        }

        return rawProfile.Trim().ToLowerInvariant() switch
        {
            "minimal" => "minimal",
            "debug" => "debug",
            _ => "standard",
        };
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
