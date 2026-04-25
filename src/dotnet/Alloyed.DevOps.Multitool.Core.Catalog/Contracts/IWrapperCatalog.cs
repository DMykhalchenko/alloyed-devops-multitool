namespace Alloyed.DevOps.Multitool.Core.Catalog.Contracts;

using Alloyed.DevOps.Multitool.Core.Catalog.Models;

public interface IWrapperCatalog
{
    bool HasWrapper(string commandName);

    string GetWrapperName(string commandName);

    ResolutionResult Resolve(IEnumerable<string> commands);

    IReadOnlyList<string> GetRequiredModules(IEnumerable<string> commands);

    IReadOnlyDictionary<string, string> GetMappings();
}
