namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Alloyed.DevOps.Multitool.Core.Ast.Contracts;
using Alloyed.DevOps.Multitool.Core.Ast.Services;
using Alloyed.DevOps.Multitool.Core.Builders.Contracts;
using Alloyed.DevOps.Multitool.Core.Builders.Services;
using Alloyed.DevOps.Multitool.Core.Catalog.Contracts;
using Alloyed.DevOps.Multitool.Core.Catalog.Services;
using Alloyed.DevOps.Multitool.Host.PowerShell.Contracts;
using Alloyed.DevOps.Multitool.Host.PowerShell.Models;

public static class PipelineBootstrap
{
    public static ITransformationPipeline CreateDefault(
        string? configurationBasePath = null,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        IScriptAnalyzer analyzer = new PowerShellScriptAnalyzer();
        IWrapperCatalog catalog = new InMemoryWrapperCatalog();
        ICommandTransformer transformer = new TextCommandTransformer();
        IModuleBuilder moduleBuilder = new MinimalModuleBuilder();
        var configuration = CreateRuntimeConfiguration(configurationBasePath, environment);

        return new TransformationPipeline(analyzer, catalog, transformer, moduleBuilder, configuration);
    }

    public static IWrapperCatalog CreateCatalog()
    {
        return new InMemoryWrapperCatalog();
    }

    public static RuntimeConfiguration CreateRuntimeConfiguration(
        string? configurationBasePath = null,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var loader = new RuntimeConfigurationLoader();
        return loader.Load(configurationBasePath, environment);
    }
}
