namespace Alloyed.DevOps.Multitool.Host.PowerShell.Models;

/// <summary>
/// Captures user-selected defaults for initializing an Alloyed runtime configuration.
/// </summary>
/// <param name="OutputMode">Selected console output mode (Plain or Rich).</param>
/// <param name="EnableTransparency">Whether transparency should be enabled by default.</param>
/// <param name="EnableSession">Whether session mode should be enabled by default.</param>
/// <param name="MaxRetries">Selected retry count for runtime execution.</param>
/// <param name="EnableBackoff">Whether exponential backoff should be enabled.</param>
/// <param name="EnablePreview">Whether runtime preview logs should be enabled.</param>
/// <param name="TransparencyProfile">Selected transparency profile.</param>
/// <param name="ApplyToCurrentSession">Whether selected settings should be applied to the current session immediately.</param>
public sealed record RuntimeConfigurationInitializationSelection(
    string OutputMode,
    bool EnableTransparency,
    bool EnableSession,
    int MaxRetries,
    bool EnableBackoff,
    bool EnablePreview,
    string TransparencyProfile,
    bool ApplyToCurrentSession);
