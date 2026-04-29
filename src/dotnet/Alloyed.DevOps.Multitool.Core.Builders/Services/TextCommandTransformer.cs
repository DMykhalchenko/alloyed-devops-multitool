namespace Alloyed.DevOps.Multitool.Core.Builders.Services;

using System.Text;
using System.Text.RegularExpressions;
using Contracts;

/// <summary>
/// An <see cref="ICommandTransformer"/> that rewrites PowerShell command names in source text
/// using pre-compiled regular expressions. A boolean code-mask is built first by a state machine
/// identical to the one in the analyzer, so that replacements are applied only to actual code
/// segments and never to string literals, here-strings, or line comments.
/// </summary>
public sealed partial class TextCommandTransformer : ICommandTransformer
{
    /// <summary>
    /// Applies <paramref name="replacements"/> to <paramref name="sourceText"/>, skipping segments
    /// that the code-mask identifies as non-code (strings, comments).
    /// Replacements are applied longest-key-first to avoid partial matches.
    /// </summary>
    /// <param name="sourceText">Original PowerShell script text.</param>
    /// <param name="replacements">
    /// Map of <c>originalCommand → replacementCommand</c>. Entries where key and value are equal
    /// (case-insensitive) or where either part is whitespace-only are silently skipped.
    /// </param>
    /// <returns>Rewritten script text, or <paramref name="sourceText"/> unchanged when there are no applicable replacements.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="sourceText"/> or <paramref name="replacements"/> is <see langword="null"/>.
    /// </exception>
    public string Transform(string sourceText, IReadOnlyDictionary<string, string> replacements)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        ArgumentNullException.ThrowIfNull(replacements);

        var ordered = replacements
            .Where(static kv => !string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
            .Where(static kv => !string.Equals(kv.Key, kv.Value, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(static kv => kv.Key.Length)
            .ThenBy(static kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static kv => (
                Pattern: new Regex(
                    $@"(?<![\w-]){Regex.Escape(kv.Key)}(?![\w-])",
                    RegexOptions.CultureInvariant | RegexOptions.Compiled,
                    TimeSpan.FromSeconds(5)),
                Replacement: kv.Value))
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

    /// <summary>
    /// Builds a boolean mask over <paramref name="content"/> where <see langword="true"/> means the
    /// character belongs to executable code and <see langword="false"/> means it is inside a string
    /// literal, here-string, or line comment. Newlines are always classified as code so that line
    /// boundaries are preserved.
    /// </summary>
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

    /// <summary>
    /// Returns <see langword="true"/> when the <c>@'</c> or <c>@"</c> sequence at
    /// <paramref name="atIndex"/> is followed only by optional whitespace before a newline,
    /// satisfying the PowerShell requirement for a valid here-string opening delimiter.
    /// </summary>
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

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="text"/>[<paramref name="index"/>] is
    /// <paramref name="quote"/> followed by <c>@</c>, with only whitespace between the start of
    /// the line and <paramref name="index"/>, and only whitespace after <c>@</c> until the next
    /// newline — the PowerShell rule for a valid here-string closing delimiter.
    /// </summary>
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

    /// <summary>
    /// Applies each regex replacement in <paramref name="ordered"/> to <paramref name="source"/>
    /// sequentially, in the pre-sorted longest-key-first order.
    /// </summary>
    private static string ApplyReplacements(string source, IReadOnlyList<(Regex Pattern, string Replacement)> ordered)
    {
        var current = source;

        foreach (var (pattern, replacement) in ordered)
        {
            current = pattern.Replace(current, replacement);
        }

        return current;
    }

    /// <summary>Internal state machine states used by <see cref="BuildCodeMask"/>.</summary>
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
