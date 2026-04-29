namespace Alloyed.DevOps.Multitool.Core.Ast.Services;

using System.Text.RegularExpressions;
using Contracts;
using Models;

/// <summary>
/// A lightweight, regex-based implementation of <see cref="IScriptAnalyzer"/> that operates without
/// loading the PowerShell runtime. It uses a character-level state machine to mask string literals,
/// here-strings, and line comments before applying a command-pattern regex, so that command-like
/// tokens inside quoted text are not reported as command usages.
/// </summary>
/// <remarks>
/// This analyzer is intentionally heuristic: it trades perfect accuracy for zero runtime dependencies.
/// For authoritative analysis, use <see cref="PowerShellScriptAnalyzer"/> instead.
/// </remarks>
public sealed partial class HeuristicScriptAnalyzer : IScriptAnalyzer
{
    private static readonly HashSet<string> ReservedTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "if", "elseif", "else", "switch", "foreach", "for", "while", "do", "try", "catch", "finally", "return", "function"
    };

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
    /// Analyzes the supplied script <paramref name="content"/> using the heuristic state-machine scanner.
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

        var normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal);
        var diagnostics = new List<ParseDiagnostic>();
        var masked = MaskNonCode(normalized, diagnostics);

        var commands = new List<CommandUsage>();
        var lines = masked.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            foreach (Match match in CommandPattern().Matches(line))
            {
                if (!match.Success)
                {
                    continue;
                }

                var commandName = match.Groups["cmd"].Value;
                var moduleName = match.Groups["module"].Success ? match.Groups["module"].Value : null;

                if (ReservedTokens.Contains(commandName))
                {
                    continue;
                }

                commands.Add(new CommandUsage(
                    CommandName: commandName,
                    ModuleName: moduleName,
                    Line: lineNumber,
                    Column: match.Index + 1,
                    IsQualified: moduleName is not null));
            }
        }

        return new ScriptAnalysisResult(logicalPath, commands, diagnostics, content);
    }

    /// <summary>
    /// Replaces all non-code characters (string literals, here-strings, line comments) in
    /// <paramref name="content"/> with spaces, preserving newlines so that line numbers remain
    /// accurate. A <see cref="ParseDiagnostic"/> is appended to <paramref name="diagnostics"/>
    /// when an unterminated string is detected at end-of-file.
    /// </summary>
    private static string MaskNonCode(string content, ICollection<ParseDiagnostic> diagnostics)
    {
        var chars = content.ToCharArray();
        var state = ScanState.Normal;

        for (var i = 0; i < chars.Length; i++)
        {
            var ch = chars[i];

            switch (state)
            {
                case ScanState.Normal:
                    if (ch == '#')
                    {
                        chars[i] = ' ';
                        state = ScanState.LineComment;
                        continue;
                    }

                    if (ch == '\'')
                    {
                        chars[i] = ' ';
                        state = ScanState.SingleQuoted;
                        continue;
                    }

                    if (ch == '"')
                    {
                        chars[i] = ' ';
                        state = ScanState.DoubleQuoted;
                        continue;
                    }

                    if (ch == '@' && i + 1 < chars.Length && (chars[i + 1] == '\'' || chars[i + 1] == '"') && IsHereStringStart(chars, i))
                    {
                        var quote = chars[i + 1];
                        chars[i] = ' ';
                        chars[i + 1] = ' ';
                        i++;
                        state = quote == '\'' ? ScanState.SingleHereString : ScanState.DoubleHereString;
                    }

                    break;

                case ScanState.LineComment:
                    if (ch != '\n')
                    {
                        chars[i] = ' ';
                    }
                    else
                    {
                        state = ScanState.Normal;
                    }

                    break;

                case ScanState.SingleQuoted:
                    if (ch != '\n')
                    {
                        chars[i] = ' ';
                    }

                    if (ch == '\'' && i + 1 < chars.Length && chars[i + 1] == '\'')
                    {
                        chars[i + 1] = ' ';
                        i++;
                        continue;
                    }

                    if (ch == '\'')
                    {
                        state = ScanState.Normal;
                    }

                    break;

                case ScanState.DoubleQuoted:
                    if (ch != '\n')
                    {
                        chars[i] = ' ';
                    }

                    if (ch == '`' && i + 1 < chars.Length)
                    {
                        chars[i + 1] = ' ';
                        i++;
                        continue;
                    }

                    if (ch == '"')
                    {
                        state = ScanState.Normal;
                    }

                    break;

                case ScanState.SingleHereString:
                    if (IsHereStringTerminator(chars, i, '\''))
                    {
                        chars[i] = ' ';
                        chars[i + 1] = ' ';
                        i++;
                        state = ScanState.Normal;
                        continue;
                    }

                    if (ch != '\n')
                    {
                        chars[i] = ' ';
                    }

                    break;

                case ScanState.DoubleHereString:
                    if (IsHereStringTerminator(chars, i, '"'))
                    {
                        chars[i] = ' ';
                        chars[i + 1] = ' ';
                        i++;
                        state = ScanState.Normal;
                        continue;
                    }

                    if (ch != '\n')
                    {
                        chars[i] = ' ';
                    }

                    break;
            }
        }

        if (state is ScanState.SingleQuoted or ScanState.DoubleQuoted or ScanState.SingleHereString or ScanState.DoubleHereString)
        {
            var line = content.Count(static c => c == '\n') + 1;
            diagnostics.Add(new ParseDiagnostic(
                Message: "Detected unterminated string in script.",
                Line: line,
                Column: 1,
                Severity: ParseDiagnosticSeverity.Warning));
        }

        return new string(chars);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the <c>@'</c> or <c>@"</c> sequence starting at
    /// <paramref name="atIndex"/> is followed only by optional whitespace before a newline,
    /// which is the PowerShell rule for a valid here-string opening delimiter.
    /// </summary>
    private static bool IsHereStringStart(IReadOnlyList<char> chars, int atIndex)
    {
        var i = atIndex + 2;

        while (i < chars.Count && chars[i] != '\n')
        {
            if (!char.IsWhiteSpace(chars[i]))
            {
                return false;
            }

            i++;
        }

        return true;
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="chars"/>[<paramref name="index"/>] is
    /// <paramref name="quote"/> followed by <c>@</c>, with only whitespace between the start of
    /// the current line and <paramref name="index"/>, and only whitespace between <c>@</c> and the
    /// next newline — the PowerShell rule for a valid here-string closing delimiter.
    /// </summary>
    private static bool IsHereStringTerminator(IReadOnlyList<char> chars, int index, char quote)
    {
        if (index + 1 >= chars.Count || chars[index] != quote || chars[index + 1] != '@')
        {
            return false;
        }

        for (var left = index - 1; left >= 0 && chars[left] != '\n'; left--)
        {
            if (!char.IsWhiteSpace(chars[left]))
            {
                return false;
            }
        }

        for (var right = index + 2; right < chars.Count && chars[right] != '\n'; right++)
        {
            if (!char.IsWhiteSpace(chars[right]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Source-generated regex that matches PowerShell command tokens of the form
    /// <c>[Module\]Verb-Noun</c> and a small set of approved aliases (<c>gci</c>, <c>gi</c>, <c>tp</c>).
    /// Named groups: <c>module</c> (optional qualifier) and <c>cmd</c> (command name).
    /// </summary>
    [GeneratedRegex(@"(?<![\w-])(?:(?<module>[A-Za-z0-9_.]+)\\)?(?<cmd>[A-Za-z]+-[A-Za-z][A-Za-z0-9-]*|gci|gi|tp)(?![\w-])", RegexOptions.CultureInvariant)]
    private static partial Regex CommandPattern();

    /// <summary>Internal state machine states used by <see cref="MaskNonCode"/>.</summary>
    private enum ScanState
    {
        Normal,
        LineComment,
        SingleQuoted,
        DoubleQuoted,
        SingleHereString,
        DoubleHereString,
    }
}
