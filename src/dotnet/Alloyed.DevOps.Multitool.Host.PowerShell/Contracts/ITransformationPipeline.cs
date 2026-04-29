namespace Alloyed.DevOps.Multitool.Host.PowerShell.Contracts;

using Alloyed.DevOps.Multitool.Host.PowerShell.Models;

/// <summary>
/// Orchestrates the end-to-end script transformation workflow: validation, AST analysis,
/// catalog resolution, command substitution, and module output.
/// </summary>
public interface ITransformationPipeline
{
    /// <summary>
    /// Executes the full transformation pipeline for the script described by
    /// <paramref name="request"/> and returns a <see cref="PipelineResult"/> that describes the
    /// outcome. Never throws; all errors are captured in the result.
    /// </summary>
    /// <param name="request">Input parameters for this pipeline run.</param>
    /// <returns>
    /// A <see cref="PipelineResult"/> with <see cref="PipelineResult.Success"/> set to
    /// <see langword="true"/> on success, or <see langword="false"/> with a populated
    /// <see cref="PipelineResult.ErrorMessage"/> on failure.
    /// </returns>
    PipelineResult Execute(PipelineRequest request);
}
