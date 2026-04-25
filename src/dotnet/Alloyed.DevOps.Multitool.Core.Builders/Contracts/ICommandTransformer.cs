namespace Alloyed.DevOps.Multitool.Core.Builders.Contracts;

public interface ICommandTransformer
{
    string Transform(string sourceText, IReadOnlyDictionary<string, string> replacements);
}
