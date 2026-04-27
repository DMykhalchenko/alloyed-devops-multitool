namespace Alloyed.DevOps.Multitool.Core.Ast.Services;

using System.Management.Automation.Language;
using Alloyed.DevOps.Multitool.Core.Ast.Contracts;
using Alloyed.DevOps.Multitool.Core.Ast.Models;

public sealed class PowerShellScriptAnalyzer : IScriptAnalyzer
{
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
            .ToList();

        return new ScriptAnalysisResult(logicalPath, commands, diagnostics, content);
    }
}
