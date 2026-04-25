namespace Alloyed.DevOps.Multitool.Core.Ast.Services;

using System.Text.RegularExpressions;
using Alloyed.DevOps.Multitool.Core.Ast.Contracts;
using Alloyed.DevOps.Multitool.Core.Ast.Models;

public sealed partial class HeuristicScriptAnalyzer : IScriptAnalyzer
{
    private static readonly HashSet<string> ReservedTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "if", "elseif", "else", "switch", "foreach", "for", "while", "do", "try", "catch", "finally", "return", "function"
    };

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

    [GeneratedRegex(@"(?<![\w-])(?:(?<module>[A-Za-z0-9_.]+)\\)?(?<cmd>[A-Za-z]+-[A-Za-z][A-Za-z0-9-]*)", RegexOptions.CultureInvariant)]
    private static partial Regex CommandPattern();

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
