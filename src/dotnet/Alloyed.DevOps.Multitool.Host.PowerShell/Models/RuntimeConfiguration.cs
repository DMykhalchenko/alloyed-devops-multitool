namespace Alloyed.DevOps.Multitool.Host.PowerShell.Models;

/// <summary>
/// Root configuration record for the Alloyed transformation host. Loaded by
/// <see cref="Services.RuntimeConfigurationLoader"/> from a layered hierarchy of defaults, JSON,
/// YAML, and environment variables. Use <see cref="Default"/> for sensible out-of-the-box values.
/// </summary>
public sealed record RuntimeConfiguration(
    RuntimeOptions Runtime,
    SessionOptions Session,
    DecorationOptions Decoration,
    MockingOptions Mocking,
    CatalogOptions Catalog)
{
    /// <summary>
    /// A pre-built instance with safe defaults: no fail-on-severity threshold, output path
    /// <c>out</c>, session disabled, error handling and observability enabled, transparency and
    /// mocking disabled, and the embedded catalog in use.
    /// </summary>
    public static RuntimeConfiguration Default { get; } = new(
        Runtime: new RuntimeOptions(FailOnSeverity: null, DefaultOutputPath: "out"),
        Session: new SessionOptions(Enabled: false),
        Decoration: new DecorationOptions(
            EnableErrorHandling: true,
            EnableObservability: true,
            EnableCorrelation: true,
            EnableTransparency: false,
            TransparencyProfile: TransparencyProfile.Standard),
        Mocking: new MockingOptions(
            Enabled: false,
            Mode: MockingMode.InMemory),
        Catalog: new CatalogOptions(SourcePath: null));
}

/// <summary>
/// General runtime settings that affect pipeline behaviour.
/// </summary>
/// <param name="FailOnSeverity">
/// When set, the pipeline stops early if any AST diagnostic at or above this level is detected.
/// <see langword="null"/> means no early-exit threshold is applied.
/// </param>
/// <param name="DefaultOutputPath">
/// Fallback output directory used when the caller does not supply one explicitly. Defaults to
/// <c>out</c>.
/// </param>
public sealed record RuntimeOptions(PipelineDiagnosticSeverity? FailOnSeverity, string DefaultOutputPath);

/// <summary>
/// Controls whether an interactive host session is active.
/// </summary>
/// <param name="Enabled">
/// <see langword="true"/> to enable the session; <see langword="false"/> (default) for
/// non-interactive/batch execution.
/// </param>
public sealed record SessionOptions(bool Enabled);

/// <summary>
/// Toggles the individual decorators that wrap pipeline operations.
/// </summary>
/// <param name="EnableErrorHandling">Activates <see cref="Decorators.ErrorHandlingDecorator"/> (priority 1000).</param>
/// <param name="EnableObservability">Activates <see cref="Decorators.ObservabilityDecorator"/> (priority 700).</param>
/// <param name="EnableCorrelation">Activates <see cref="Decorators.CorrelationDecorator"/> (priority 800).</param>
/// <param name="EnableTransparency">Activates <see cref="Decorators.TransparencyDecorator"/> (priority 650); off by default.</param>
/// <param name="TransparencyProfile">Verbosity profile applied when transparency is enabled.</param>
public sealed record DecorationOptions(
    bool EnableErrorHandling,
    bool EnableObservability,
    bool EnableCorrelation,
    bool EnableTransparency,
    TransparencyProfile TransparencyProfile);

/// <summary>
/// Controls the test-mocking layer used during integration testing.
/// </summary>
/// <param name="Enabled"><see langword="true"/> to replace real services with mocks.</param>
/// <param name="Mode">The mocking strategy to apply when <paramref name="Enabled"/> is <see langword="true"/>.</param>
public sealed record MockingOptions(bool Enabled, MockingMode Mode);

/// <summary>
/// Points the catalog loader to an external JSON file, overriding the embedded catalog.
/// </summary>
/// <param name="SourcePath">
/// Absolute or relative path to a <c>ports.catalog.json</c> file, or <see langword="null"/> to
/// use the embedded resource.
/// </param>
public sealed record CatalogOptions(string? SourcePath);

/// <summary>
/// Determines which mocking strategy is used when <see cref="MockingOptions.Enabled"/> is
/// <see langword="true"/>.
/// </summary>
public enum MockingMode
{
    /// <summary>Use a simple in-memory stub implementation.</summary>
    InMemory = 0,

    /// <summary>Use Moq-generated mocks.</summary>
    Moq = 1,

    /// <summary>Use a caller-supplied custom mock implementation.</summary>
    Custom = 2,
}

/// <summary>
/// Controls the verbosity of <see cref="Decorators.TransparencyDecorator"/> log output.
/// </summary>
public enum TransparencyProfile
{
    /// <summary>Operation name only; no tag values are emitted.</summary>
    Minimal = 0,

    /// <summary>High-signal tags only (operation, correlationId, enableTransparency).</summary>
    Standard = 1,

    /// <summary>All tags, including low-signal ones; sensitive values are still redacted.</summary>
    Debug = 2,
}
