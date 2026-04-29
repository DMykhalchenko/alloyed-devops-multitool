namespace Alloyed.DevOps.Multitool.Core.Builders.Models;

/// <summary>
/// Describes all inputs required to build a PowerShell module on disk.
/// </summary>
/// <param name="ModuleName">Name of the module, used as the directory name and as the base name for <c>.psm1</c> and <c>.psd1</c> files.</param>
/// <param name="OutputPath">Parent directory under which the module subdirectory will be created.</param>
/// <param name="TransformedScript">Full text of the (already transformed) PowerShell script to write as the module root (<c>.psm1</c>).</param>
/// <param name="RequiredModules">
/// List of module names that will appear in the <c>RequiredModules</c> field of the generated manifest (<c>.psd1</c>).
/// </param>
/// <param name="Author">Author string written to the module manifest.</param>
/// <param name="Description">Description string written to the module manifest and README.</param>
public sealed record ModuleBuildRequest(
    string ModuleName,
    string OutputPath,
    string TransformedScript,
    IReadOnlyList<string> RequiredModules,
    string Author,
    string Description
);
