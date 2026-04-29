namespace Alloyed.DevOps.Multitool.Host.PowerShell.Models;

/// <summary>
/// Input parameters for a single <see cref="Contracts.ITransformationPipeline.Execute"/> call.
/// </summary>
/// <param name="ScriptPath">Absolute or relative path to the PowerShell script to transform.</param>
/// <param name="ModuleName">Name of the output PowerShell module (used as the output directory name and module base name).</param>
/// <param name="OutputPath">Parent directory where the module subdirectory will be created.</param>
/// <param name="Force">
/// When <see langword="true"/>, overwrites an existing module directory. When
/// <see langword="false"/>, the pipeline returns an error if the target already exists.
/// </param>
/// <param name="FailOnSeverity">
/// Optional severity threshold. When any diagnostic at or above this level is detected during AST
/// analysis, the pipeline stops early. When <see langword="null"/>, the threshold from
/// <see cref="RuntimeConfiguration"/> is used instead.
/// </param>
public sealed record PipelineRequest(
    string ScriptPath,
    string ModuleName,
    string OutputPath,
    bool Force,
    PipelineDiagnosticSeverity? FailOnSeverity = null
);
