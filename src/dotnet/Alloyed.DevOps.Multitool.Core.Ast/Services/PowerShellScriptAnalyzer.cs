namespace Alloyed.DevOps.Multitool.Core.Ast.Services;

using System.Management.Automation.Language;
using Alloyed.DevOps.Multitool.Core.Ast.Contracts;
using Alloyed.DevOps.Multitool.Core.Ast.Models;

/// <summary>
/// An <see cref="IScriptAnalyzer"/> implementation backed by the PowerShell
/// <see cref="Parser"/> from <c>System.Management.Automation</c>. Produces a fully accurate AST,
/// including local function definitions that are excluded from the command list so they are not
/// treated as external invocations.
/// </summary>
/// <remarks>
/// Requires the PowerShell SDK to be present at runtime. For dependency-free analysis,
/// use <see cref="HeuristicScriptAnalyzer"/> instead.
/// </remarks>
public sealed class PowerShellScriptAnalyzer : IScriptAnalyzer
{
    /// <summary>
    /// Reads the script at <paramref name="path"/> from disk and delegates to
    /// <see cref="AnalyzeContent"/>.
    /// </summary>
    /// <param name="path">Absolute or relative path to the PowerShell script file.</param>
    /// <returns>Analysis result containing all detected command usages and any parse diagnostics.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="path"/> is null or whitespace.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when the file does not exist at <paramref name="path"/>.</exception>
    public ScriptAnalysisResult AnalyzeFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Script file not found: {path}", path);
        }

        var content = File.ReadAllText(path);
        return AnalyzeContent(path, content);
    }

    /// <summary>
    /// Parses <paramref name="content"/> with the PowerShell <see cref="Parser"/> and walks the
    /// resulting AST to collect <see cref="CommandAst"/> nodes, skipping locally defined functions.
    /// Parse errors are reported as <see cref="ParseDiagnosticSeverity.Warning"/> because the AST
    /// is still usable and transformation can proceed.
    /// </summary>
    /// <param name="logicalPath">
    /// A logical identifier stored in the returned <see cref="ScriptAnalysisResult.ScriptPath"/>;
    /// does not need to be a real file path.
    /// </param>
    /// <param name="content">Raw PowerShell script text to analyze.</param>
    /// <returns>Analysis result containing all detected command usages and any parse diagnostics.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="logicalPath"/> or <paramref name="content"/> is <see langword="null"/>.
    /// </exception>
    public ScriptAnalysisResult AnalyzeContent(string logicalPath, string content)
    {
        ArgumentNullException.ThrowIfNull(logicalPath);
        ArgumentNullException.ThrowIfNull(content);

        var ast = Parser.ParseInput(content, out _, out ParseError[] parseErrors);

        // ParseErrors are non-fatal: the AST is still produced and transformation can proceed.
        // Report them as Warning so callers can escalate if needed via FailOnSeverity.
        var diagnostics = parseErrors
            .Select(e => new ParseDiagnostic(
                Message: e.Message,
                Line: e.Extent.StartLineNumber,
                Column: e.Extent.StartColumnNumber,
                Severity: ParseDiagnosticSeverity.Warning))
            .ToList();

        var localFunctionNames = ast
            .FindAll(node => node is FunctionDefinitionAst, searchNestedScriptBlocks: true)
            .Cast<FunctionDefinitionAst>()
            .Select(f => f.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var commandNodes = ast.FindAll(node => node is CommandAst, searchNestedScriptBlocks: true);

        var commands = commandNodes
            .Cast<CommandAst>()
            .Select(cmd =>
            {
                var nameExpr = cmd.CommandElements.FirstOrDefault();
                if (nameExpr is null)
                {
                    return null;
                }

                var rawName = nameExpr.Extent.Text;
                string? moduleName = null;
                string commandName;

                var backslash = rawName.IndexOf('\\', StringComparison.Ordinal);
                if (backslash >= 0)
                {
                    moduleName = rawName[..backslash];
                    commandName = rawName[(backslash + 1)..];
                }
                else
                {
                    commandName = rawName;
                }

                return new CommandUsage(
                    CommandName: commandName,
                    ModuleName: moduleName,
                    Line: nameExpr.Extent.StartLineNumber,
                    Column: nameExpr.Extent.StartColumnNumber,
                    IsQualified: moduleName is not null);
            })
            .OfType<CommandUsage>()
            .Where(c => !localFunctionNames.Contains(c.CommandName))
            .ToList();

        return new ScriptAnalysisResult(logicalPath, commands, diagnostics, content);
    }
}
