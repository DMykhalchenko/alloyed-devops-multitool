namespace Alloyed.DevOps.Multitool.Core.Builders.Contracts;

/// <summary>
/// Rewrites PowerShell source text by substituting command names according to a replacement map,
/// while preserving the content of string literals, here-strings, and line comments unchanged.
/// </summary>
public interface ICommandTransformer
{
    /// <summary>
    /// Applies all entries in <paramref name="replacements"/> to <paramref name="sourceText"/> and
    /// returns the transformed script.
    /// </summary>
    /// <param name="sourceText">Original PowerShell script text.</param>
    /// <param name="replacements">
    /// A map of <c>originalCommand → replacementCommand</c>. Entries where key equals value
    /// (case-insensitive) are skipped. Longer keys are applied first to avoid partial matches.
    /// </param>
    /// <returns>
    /// The rewritten script text. Returns <paramref name="sourceText"/> unchanged when
    /// <paramref name="replacements"/> contains no actionable entries.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="sourceText"/> or <paramref name="replacements"/> is <see langword="null"/>.
    /// </exception>
    string Transform(string sourceText, IReadOnlyDictionary<string, string> replacements);
}
