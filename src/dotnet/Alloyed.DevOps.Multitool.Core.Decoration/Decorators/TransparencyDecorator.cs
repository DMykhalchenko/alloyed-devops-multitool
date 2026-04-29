namespace Alloyed.DevOps.Multitool.Core.Decoration.Decorators;

using System.Diagnostics;
using Contracts;
using Models;

/// <summary>
/// A decorator (priority 650, inside <see cref="ObservabilityDecorator"/>) that emits detailed
/// transparency events including sanitized tag snapshots. It is opt-in: it only activates when
/// the <see cref="EnableTransparencyTag"/> tag is set to <c>"true"</c> in the context.
/// </summary>
/// <remarks>
/// <para>
/// Three transparency profiles control verbosity:
/// <list type="bullet">
///   <item><term>minimal</term><description>Operation name only; no tags.</description></item>
///   <item><term>standard</term><description>High-signal tags only (operation, correlationId, enableTransparency).</description></item>
///   <item><term>debug</term><description>All tags, including low-signal ones.</description></item>
/// </list>
/// </para>
/// <para>
/// Tag values whose keys match <c>password</c>, <c>secret</c>, <c>token</c>, <c>apikey</c>,
/// <c>api_key</c>, <c>credential</c>, <c>authorization</c>, <c>accesskey</c>, or
/// <c>privatekey</c> (case-insensitive substring match) are replaced with <c>***REDACTED***</c>.
/// </para>
/// </remarks>
public sealed class TransparencyDecorator : IDecorator
{
    /// <summary>
    /// Tag key that enables this decorator. Set to <c>"true"</c> in the context to activate.
    /// </summary>
    public const string EnableTransparencyTag = "enableTransparency";

    /// <summary>
    /// Tag key that switches the profile to <c>debug</c> when its value parses to
    /// <see langword="true"/>. Ignored when <see cref="ProfileTransparencyTag"/> is set explicitly.
    /// </summary>
    public const string VerboseTransparencyTag = "transparencyVerbose";

    /// <summary>
    /// Tag key for the explicit transparency profile name (<c>minimal</c>, <c>standard</c>, or
    /// <c>debug</c>). Takes precedence over <see cref="VerboseTransparencyTag"/>.
    /// </summary>
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

    /// <summary>
    /// Initializes the decorator with an optional <paramref name="sink"/>. Defaults to
    /// <see cref="Services.NullDecorationSink"/> when <see langword="null"/>.
    /// </summary>
    /// <param name="sink">The sink that receives the emitted events.</param>
    public TransparencyDecorator(IDecorationSink? sink = null)
    {
        _sink = sink ?? new Services.NullDecorationSink();
    }

    /// <inheritdoc/>
    // Keep it inside Observability to preserve standard enter/exit envelopes.
    public int Priority => 650;

    /// <inheritdoc/>
    public string Name => nameof(TransparencyDecorator);

    /// <summary>
    /// Returns <see langword="true"/> only when the <see cref="EnableTransparencyTag"/> tag in
    /// <paramref name="context"/> parses to <see langword="true"/>.
    /// </summary>
    /// <inheritdoc/>
    public bool Enabled(DecorationContext context)
    {
        var raw = context.GetTag(EnableTransparencyTag);
        return bool.TryParse(raw, out var enabled) && enabled;
    }

    /// <summary>
    /// Emits enter/exit/error events with a sanitized tag summary built according to the active
    /// transparency profile.
    /// </summary>
    /// <inheritdoc/>
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

    /// <summary>
    /// Builds a space-prefixed tag summary string with sensitive values redacted, filtered by the
    /// active <paramref name="profile"/>.
    /// </summary>
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

    /// <summary>
    /// Formats the event message according to the active <paramref name="profile"/>.
    /// </summary>
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

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="key"/> should appear in the tag summary
    /// for the given <paramref name="profile"/>.
    /// </summary>
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

    /// <summary>
    /// Resolves the transparency profile name from the raw tag value and the verbose flag.
    /// Returns <c>standard</c> when the raw value is absent or unrecognized.
    /// </summary>
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

    /// <summary>
    /// Returns <see langword="true"/> for tags that are always useful regardless of profile
    /// (operation, correlationId, enableTransparency).
    /// </summary>
    private static bool IsHighSignalTag(string key)
    {
        return key.Equals("operation", StringComparison.OrdinalIgnoreCase) ||
               key.Equals(EnableTransparencyTag, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(CorrelationDecorator.CorrelationIdTag, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="key"/> contains a sensitive keyword
    /// (case-insensitive substring match against <see cref="SensitiveKeyMarkers"/>).
    /// </summary>
    private static bool ShouldRedact(string key)
    {
        return SensitiveKeyMarkers.Any(marker =>
            key.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
