namespace Alloyed.DevOps.Multitool.Host.PowerShell.Models;

public sealed record RuntimeConfiguration(
    RuntimeOptions Runtime,
    SessionOptions Session,
    DecorationOptions Decoration,
    MockingOptions Mocking)
{
    public static RuntimeConfiguration Default { get; } = new(
        Runtime: new RuntimeOptions(FailOnSeverity: null),
        Session: new SessionOptions(Enabled: false),
        Decoration: new DecorationOptions(
            EnableErrorHandling: true,
            EnableObservability: true,
            EnableCorrelation: true,
            EnableTransparency: false),
        Mocking: new MockingOptions(
            Enabled: false,
            Mode: MockingMode.InMemory));
}

public sealed record RuntimeOptions(PipelineDiagnosticSeverity? FailOnSeverity);

public sealed record SessionOptions(bool Enabled);

public sealed record DecorationOptions(
    bool EnableErrorHandling,
    bool EnableObservability,
    bool EnableCorrelation,
    bool EnableTransparency);

public sealed record MockingOptions(bool Enabled, MockingMode Mode);

public enum MockingMode
{
    InMemory = 0,
    Moq = 1,
    Custom = 2,
}
