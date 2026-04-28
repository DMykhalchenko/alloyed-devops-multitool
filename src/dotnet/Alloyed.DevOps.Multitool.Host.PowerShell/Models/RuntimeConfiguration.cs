namespace Alloyed.DevOps.Multitool.Host.PowerShell.Models;

public sealed record RuntimeConfiguration(
    RuntimeOptions Runtime,
    SessionOptions Session,
    DecorationOptions Decoration,
    MockingOptions Mocking,
    CatalogOptions Catalog)
{
    public static RuntimeConfiguration Default { get; } = new(
        Runtime: new RuntimeOptions(FailOnSeverity: null, DefaultOutputPath: "out"),
        Session: new SessionOptions(Enabled: false),
        Decoration: new DecorationOptions(
            EnableErrorHandling: true,
            EnableObservability: true,
            EnableCorrelation: true,
            EnableTransparency: false),
        Mocking: new MockingOptions(
            Enabled: false,
            Mode: MockingMode.InMemory),
        Catalog: new CatalogOptions(SourcePath: null));
}

public sealed record RuntimeOptions(PipelineDiagnosticSeverity? FailOnSeverity, string DefaultOutputPath);

public sealed record SessionOptions(bool Enabled);

public sealed record DecorationOptions(
    bool EnableErrorHandling,
    bool EnableObservability,
    bool EnableCorrelation,
    bool EnableTransparency);

public sealed record MockingOptions(bool Enabled, MockingMode Mode);

public sealed record CatalogOptions(string? SourcePath);

public enum MockingMode
{
    InMemory = 0,
    Moq = 1,
    Custom = 2,
}
