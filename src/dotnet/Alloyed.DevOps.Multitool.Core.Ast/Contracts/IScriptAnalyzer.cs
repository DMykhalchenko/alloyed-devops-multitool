namespace Alloyed.DevOps.Multitool.Core.Ast.Contracts;

using Alloyed.DevOps.Multitool.Core.Ast.Models;

/// <summary>
/// Analyzes PowerShell scripts and extracts command usages and parse diagnostics.
/// </summary>
public interface IScriptAnalyzer
{
    /// <summary>
    /// Reads a script from <paramref name="path"/> and returns the analysis result.
    /// </summary>
    /// <param name="path">Absolute or relative path to the PowerShell script file.</param>
    /// <returns>A <see cref="ScriptAnalysisResult"/> describing commands and diagnostics found in the script.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="path"/> is null or whitespace.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when the file does not exist.</exception>
    ScriptAnalysisResult AnalyzeFile(string path);

    /// <summary>
    /// Analyzes the supplied script <paramref name="content"/> without reading from disk.
    /// </summary>
    /// <param name="logicalPath">A logical identifier used as the script path in the returned result (e.g. the original file path).</param>
    /// <param name="content">Raw text of the PowerShell script.</param>
    /// <returns>A <see cref="ScriptAnalysisResult"/> describing commands and diagnostics found in the script.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="logicalPath"/> or <paramref name="content"/> is null.</exception>
    ScriptAnalysisResult AnalyzeContent(string logicalPath, string content);
}
