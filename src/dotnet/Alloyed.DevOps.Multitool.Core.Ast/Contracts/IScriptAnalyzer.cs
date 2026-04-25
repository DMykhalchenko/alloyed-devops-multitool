namespace Alloyed.DevOps.Multitool.Core.Ast.Contracts;

using Alloyed.DevOps.Multitool.Core.Ast.Models;

public interface IScriptAnalyzer
{
    ScriptAnalysisResult AnalyzeFile(string path);

    ScriptAnalysisResult AnalyzeContent(string logicalPath, string content);
}
