namespace Alloyed.DevOps.Multitool.Core.Builders.Services;

using System.Text;
using System.Text.RegularExpressions;
using Alloyed.DevOps.Multitool.Core.Builders.Contracts;

public sealed partial class TextCommandTransformer : ICommandTransformer
{
    public string Transform(string sourceText, IReadOnlyDictionary<string, string> replacements)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        ArgumentNullException.ThrowIfNull(replacements);

        var ordered = replacements
            .Where(static kv => !string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
            .Where(static kv => !string.Equals(kv.Key, kv.Value, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(static kv => kv.Key.Length)
            .ThenBy(static kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (ordered.Count == 0)
        {
            return sourceText;
        }

        var normalized = sourceText.Replace("\r\n", "\n", StringComparison.Ordinal);
        var codeMask = BuildCodeMask(normalized);
        var output = new StringBuilder(normalized.Length + 64);

        var start = 0;
        while (start < normalized.Length)
        {
            var isCode = codeMask[start];
            var end = start + 1;

            while (end < normalized.Length && codeMask[end] == isCode)
            {
                end++;
            }

            var segment = normalized[start..end];
            output.Append(isCode ? ApplyReplacements(segment, ordered) : segment);
            start = end;
        }

        return output.ToString();
    }

    private static bool[] BuildCodeMask(string content)
    {
        var mask = Enumerable.Repeat(true, content.Length).ToArray();
        var state = ScanState.Normal;

        for (var i = 0; i < content.Length; i++)
        {
            var ch = content[i];

            switch (state)
            {
                case ScanState.Normal:
                    if (ch == '#')
                    {
                        mask[i] = false;
                        state = ScanState.LineComment;
                        continue;
                    }

                    if (ch == '\'')
                    {
                        mask[i] = false;
                        state = ScanState.SingleQuoted;
                        continue;
                    }

                    if (ch == '"')
                    {
                        mask[i] = false;
                        state = ScanState.DoubleQuoted;
                        continue;
                    }

                    if (ch == '@' && i + 1 < content.Length && (content[i + 1] == '\'' || content[i + 1] == '"') && IsHereStringStart(content, i))
                    {
                        var quote = content[i + 1];
                        mask[i] = false;
                        mask[i + 1] = false;
                        i++;
                        state = quote == '\'' ? ScanState.SingleHereString : ScanState.DoubleHereString;
                    }

                    break;

                case ScanState.LineComment:
                    if (ch != '\n')
                    {
                        mask[i] = false;
                    }
                    else
                    {
                        state = ScanState.Normal;
                    }

                    break;

                case ScanState.SingleQuoted:
                    if (ch != '\n')
                    {
                        mask[i] = false;
                    }

                    if (ch == '\'' && i + 1 < content.Length && content[i + 1] == '\'')
                    {
                        mask[i + 1] = false;
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
                        mask[i] = false;
                    }

                    if (ch == '`' && i + 1 < content.Length)
                    {
                        mask[i + 1] = false;
                        i++;
                        continue;
                    }

                    if (ch == '"')
                    {
                        state = ScanState.Normal;
                    }

                    break;

                case ScanState.SingleHereString:
                    if (IsHereStringTerminator(content, i, '\''))
                    {
                        mask[i] = false;
                        mask[i + 1] = false;
                        i++;
                        state = ScanState.Normal;
                        continue;
                    }

                    if (ch != '\n')
                    {
                        mask[i] = false;
                    }

                    break;

                case ScanState.DoubleHereString:
                    if (IsHereStringTerminator(content, i, '"'))
                    {
                        mask[i] = false;
                        mask[i + 1] = false;
                        i++;
                        state = ScanState.Normal;
                        continue;
                    }

                    if (ch != '\n')
                    {
                        mask[i] = false;
                    }

                    break;
            }
        }

        return mask;
    }

    private static bool IsHereStringStart(string text, int atIndex)
    {
        var i = atIndex + 2;

        while (i < text.Length && text[i] != '\n')
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                return false;
            }

            i++;
        }

        return true;
    }

    private static bool IsHereStringTerminator(string text, int index, char quote)
    {
        if (index + 1 >= text.Length || text[index] != quote || text[index + 1] != '@')
        {
            return false;
        }

        for (var left = index - 1; left >= 0 && text[left] != '\n'; left--)
        {
            if (!char.IsWhiteSpace(text[left]))
            {
                return false;
            }
        }

        for (var right = index + 2; right < text.Length && text[right] != '\n'; right++)
        {
            if (!char.IsWhiteSpace(text[right]))
            {
                return false;
            }
        }

        return true;
    }

    private static string ApplyReplacements(string source, IReadOnlyList<KeyValuePair<string, string>> ordered)
    {
        var current = source;

        foreach (var (original, replacement) in ordered)
        {
            var escaped = Regex.Escape(original);
            var pattern = $@"(?<![\w-]){escaped}(?![\w-])";
            current = Regex.Replace(current, pattern, replacement, RegexOptions.CultureInvariant);
        }

        return current;
    }

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
