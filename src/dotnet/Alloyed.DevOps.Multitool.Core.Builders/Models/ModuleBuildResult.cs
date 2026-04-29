namespace Alloyed.DevOps.Multitool.Core.Builders.Models;

/// <summary>
/// The immutable result returned by <see cref="Contracts.IModuleBuilder.Build"/> after attempting
/// to write a PowerShell module to disk.
/// </summary>
/// <param name="Success"><see langword="true"/> when all module files were written successfully.</param>
/// <param name="ModulePath">
/// Absolute path to the module directory. Empty string when <paramref name="Success"/> is
/// <see langword="false"/>.
/// </param>
/// <param name="Files">
/// Paths of every file created during the build (e.g. <c>.psm1</c>, <c>.psd1</c>, <c>README.md</c>).
/// Empty when <paramref name="Success"/> is <see langword="false"/>.
/// </param>
/// <param name="ErrorMessage">
/// Human-readable failure reason, or <see langword="null"/> when <paramref name="Success"/> is
/// <see langword="true"/>.
/// </param>
public sealed record ModuleBuildResult(
    bool Success,
    string ModulePath,
    IReadOnlyList<string> Files,
    string? ErrorMessage
);
