namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Alloyed.DevOps.Multitool.Core.Ast.Contracts;
using Alloyed.DevOps.Multitool.Core.Ast.Models;
using Alloyed.DevOps.Multitool.Core.Builders.Contracts;
using Alloyed.DevOps.Multitool.Core.Builders.Models;
using Alloyed.DevOps.Multitool.Core.Catalog.Contracts;
using Alloyed.DevOps.Multitool.Host.PowerShell.Contracts;
using Alloyed.DevOps.Multitool.Host.PowerShell.Models;

/// <summary>
/// The main <see cref="ITransformationPipeline"/> implementation. Orchestrates the following
/// steps for each <see cref="PipelineRequest"/>:
/// <list type="number">
///   <item>Input validation.</item>
///   <item>Output directory conflict check (respects <see cref="PipelineRequest.Force"/>).</item>
///   <item>Script analysis via <see cref="IScriptAnalyzer"/>.</item>
///   <item>Fail-on-severity evaluation.</item>
///   <item>Catalog resolution via <see cref="IWrapperCatalog"/>.</item>
///   <item>Source text transformation via <see cref="ICommandTransformer"/>.</item>
///   <item>Module output via <see cref="IModuleBuilder"/>.</item>
/// </list>
/// All exceptions are caught and returned as a failed <see cref="PipelineResult"/>; this method
/// never propagates.
/// </summary>
public sealed class TransformationPipeline : ITransformationPipeline
{
    private readonly IScriptAnalyzer _analyzer;
    private readonly IWrapperCatalog _catalog;
    private readonly ICommandTransformer _transformer;
    private readonly IModuleBuilder _moduleBuilder;
    private readonly RuntimeConfiguration _configuration;

    /// <summary>
    /// Initializes all pipeline stages.
    /// </summary>
    /// <param name="analyzer">AST analyzer used to extract command usages from the script.</param>
    /// <param name="catalog">Wrapper catalog used to resolve command substitutions.</param>
    /// <param name="transformer">Text transformer that applies the resolved substitutions.</param>
    /// <param name="moduleBuilder">Builder that writes the output module to disk.</param>
    /// <param name="configuration">
    /// Runtime configuration that supplies default options (e.g. fail-on-severity threshold).
    /// When <see langword="null"/>, <see cref="RuntimeConfiguration.Default"/> is used.
    /// </param>
    public TransformationPipeline(
        IScriptAnalyzer analyzer,
        IWrapperCatalog catalog,
        ICommandTransformer transformer,
        IModuleBuilder moduleBuilder,
        RuntimeConfiguration? configuration = null)
    {
        _analyzer = analyzer;
        _catalog = catalog;
        _transformer = transformer;
        _moduleBuilder = moduleBuilder;
        _configuration = configuration ?? RuntimeConfiguration.Default;
    }

    /// <inheritdoc/>
    public PipelineResult Execute(PipelineRequest request)
    {
        try
        {
            Validate(request);
            var ctx = Resolve(request);

            var modulePath = Path.Combine(ctx.OutputPath, ctx.ModuleName);
            if (Directory.Exists(modulePath) && !ctx.Force)
            {
                var diagnostics = new[]
                {
                    new PipelineDiagnostic(
                        Code: "PIPELINE-OUTPUT-EXISTS",
                        Source: "pipeline",
                        Message: $"Output module already exists: {modulePath}. Use Force=true to overwrite.",
                        Line: 0,
                        Column: 0,
                        Severity: PipelineDiagnosticSeverity.Error),
                };

                return new PipelineResult(
                    Success: false,
                    ModulePath: modulePath,
                    CommandsFound: 0,
                    CommandsReplaced: 0,
                    MissingCommands: Array.Empty<string>(),
                    Diagnostics: diagnostics,
                    ErrorMessage: diagnostics[0].Message);
            }

            var analysis = _analyzer.AnalyzeFile(ctx.ScriptPath);
            var pipelineDiagnostics = MapAstDiagnostics(analysis.Diagnostics);

            if (ctx.FailOnSeverity is { } threshold && pipelineDiagnostics.Any(d => d.Severity >= threshold))
            {
                var message = $"Pipeline stopped because analyzer diagnostics met fail policy ({threshold} or higher).";
                var diagnostics = pipelineDiagnostics.Concat(new[]
                {
                    new PipelineDiagnostic(
                        Code: "PIPELINE-FAIL-ON-SEVERITY",
                        Source: "pipeline",
                        Message: message,
                        Line: 0,
                        Column: 0,
                        Severity: PipelineDiagnosticSeverity.Error),
                }).ToArray();

                return new PipelineResult(
                    Success: false,
                    ModulePath: string.Empty,
                    CommandsFound: analysis.Commands.Count,
                    CommandsReplaced: 0,
                    MissingCommands: Array.Empty<string>(),
                    Diagnostics: diagnostics,
                    ErrorMessage: message);
            }

            var commandNames = analysis.Commands.Select(static c => c.CommandName).ToList();
            var resolution = _catalog.Resolve(commandNames);
            var transformed = _transformer.Transform(analysis.SourceText, resolution.Replacements);

            var buildRequest = new ModuleBuildRequest(
                ModuleName: ctx.ModuleName,
                OutputPath: ctx.OutputPath,
                TransformedScript: transformed,
                RequiredModules: resolution.RequiredModules,
                Author: Environment.UserName,
                Description: "Generated by Alloyed transformation pipeline.");

            var buildResult = _moduleBuilder.Build(buildRequest);
            if (!buildResult.Success)
            {
                var diagnostics = pipelineDiagnostics.Concat(new[]
                {
                    new PipelineDiagnostic(
                        Code: "MODULE-BUILD-FAILED",
                        Source: "module-builder",
                        Message: buildResult.ErrorMessage ?? "Module build failed.",
                        Line: 0,
                        Column: 0,
                        Severity: PipelineDiagnosticSeverity.Error),
                }).ToArray();

                return new PipelineResult(
                    Success: false,
                    ModulePath: string.Empty,
                    CommandsFound: analysis.Commands.Count,
                    CommandsReplaced: CountReplacements(resolution.Replacements),
                    MissingCommands: resolution.MissingCommands,
                    Diagnostics: diagnostics,
                    ErrorMessage: buildResult.ErrorMessage);
            }

            return new PipelineResult(
                Success: true,
                ModulePath: buildResult.ModulePath,
                CommandsFound: analysis.Commands.Count,
                CommandsReplaced: CountReplacements(resolution.Replacements),
                MissingCommands: resolution.MissingCommands,
                Diagnostics: pipelineDiagnostics,
                ErrorMessage: null);
        }
        catch (Exception ex)
        {
            var diagnostics = new[]
            {
                new PipelineDiagnostic(
                    Code: "PIPELINE-EXCEPTION",
                    Source: "pipeline",
                    Message: ex.Message,
                    Line: 0,
                    Column: 0,
                    Severity: PipelineDiagnosticSeverity.Error),
            };

            return new PipelineResult(
                Success: false,
                ModulePath: string.Empty,
                CommandsFound: 0,
                CommandsReplaced: 0,
                MissingCommands: Array.Empty<string>(),
                Diagnostics: diagnostics,
                ErrorMessage: ex.Message);
        }
    }

    /// <summary>
    /// Validates that all required fields in <paramref name="request"/> are non-null and
    /// non-whitespace.
    /// </summary>
    private static void Validate(PipelineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ScriptPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ModuleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputPath);
    }

    /// <summary>
    /// Counts entries in <paramref name="replacements"/> where key and value differ
    /// (i.e. actual substitutions, not identity mappings for unmatched commands).
    /// </summary>
    private static int CountReplacements(IReadOnlyDictionary<string, string> replacements)
    {
        return replacements.Count(static kv => !string.Equals(kv.Key, kv.Value, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Converts AST-layer <see cref="ParseDiagnostic"/> entries to pipeline-layer
    /// <see cref="PipelineDiagnostic"/> entries, inferring a short diagnostic code from the message.
    /// </summary>
    private static PipelineDiagnostic[] MapAstDiagnostics(IReadOnlyList<ParseDiagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
        {
            return Array.Empty<PipelineDiagnostic>();
        }

        return diagnostics
            .Select(static d => new PipelineDiagnostic(
                Code: InferAstCode(d.Message),
                Source: "ast-analyzer",
                Message: d.Message,
                Line: d.Line,
                Column: d.Column,
                Severity: MapSeverity(d.Severity)))
            .ToArray();
    }

    /// <summary>
    /// Infers a short diagnostic code from the AST error <paramref name="message"/> text.
    /// Defaults to <c>AST-DIAGNOSTIC</c> for unrecognised patterns.
    /// </summary>
    private static string InferAstCode(string message)
    {
        if (message.Contains("unterminated string", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("missing the terminator", StringComparison.OrdinalIgnoreCase))
        {
            return "AST-UNTERMINATED-STRING";
        }

        if (message.Contains("unbalanced quote", StringComparison.OrdinalIgnoreCase))
        {
            return "AST-UNBALANCED-QUOTE";
        }

        return "AST-DIAGNOSTIC";
    }

    /// <summary>
    /// Maps a <see cref="ParseDiagnosticSeverity"/> value to the corresponding
    /// <see cref="PipelineDiagnosticSeverity"/>. Unknown values map to Warning.
    /// </summary>
    private static PipelineDiagnosticSeverity MapSeverity(ParseDiagnosticSeverity severity)
    {
        return severity switch
        {
            ParseDiagnosticSeverity.Info => PipelineDiagnosticSeverity.Info,
            ParseDiagnosticSeverity.Warning => PipelineDiagnosticSeverity.Warning,
            ParseDiagnosticSeverity.Error => PipelineDiagnosticSeverity.Error,
            _ => PipelineDiagnosticSeverity.Warning,
        };
    }

    /// <summary>
    /// Merges per-request options with configuration defaults to produce the effective context
    /// used during this pipeline run.
    /// </summary>
    private EffectivePipelineContext Resolve(PipelineRequest request) =>
        new(
            ScriptPath: request.ScriptPath,
            ModuleName: request.ModuleName,
            OutputPath: request.OutputPath,
            Force: request.Force,
            FailOnSeverity: request.FailOnSeverity ?? _configuration.Runtime.FailOnSeverity);

    /// <summary>
    /// Immutable struct that holds the resolved, effective parameters for a single pipeline run.
    /// </summary>
    private readonly record struct EffectivePipelineContext(
        string ScriptPath,
        string ModuleName,
        string OutputPath,
        bool Force,
        PipelineDiagnosticSeverity? FailOnSeverity);
}
