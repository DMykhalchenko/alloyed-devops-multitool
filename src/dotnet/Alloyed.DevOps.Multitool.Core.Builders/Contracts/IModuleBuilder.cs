namespace Alloyed.DevOps.Multitool.Core.Builders.Contracts;

using Models;

/// <summary>
/// Creates the on-disk artifacts that constitute a PowerShell module from a
/// <see cref="ModuleBuildRequest"/>.
/// </summary>
public interface IModuleBuilder
{
    /// <summary>
    /// Builds the PowerShell module described by <paramref name="request"/> and writes all output
    /// files to disk.
    /// </summary>
    /// <param name="request">Parameters describing the module to create.</param>
    /// <returns>
    /// A <see cref="ModuleBuildResult"/> that indicates success or failure and lists the paths of
    /// every file written. On failure, <see cref="ModuleBuildResult.ErrorMessage"/> contains the
    /// reason.
    /// </returns>
    ModuleBuildResult Build(ModuleBuildRequest request);
}
