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
        var configuration = CreateRuntimeConfiguration(configurationBasePath, environment);

        IScriptAnalyzer analyzer = new PowerShellScriptAnalyzer();
        IWrapperCatalog catalog = CreateCatalog(configurationBasePath, configuration);
        ICommandTransformer transformer = new TextCommandTransformer();
        IModuleBuilder moduleBuilder = new MinimalModuleBuilder();

        return new TransformationPipeline(analyzer, catalog, transformer, moduleBuilder, configuration);
    }

    public static IWrapperCatalog CreateCatalog(
        string? configurationBasePath = null,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var configuration = CreateRuntimeConfiguration(configurationBasePath, environment);
        return CreateCatalog(configurationBasePath, configuration);
    }

    public static RuntimeConfiguration CreateRuntimeConfiguration(
        string? configurationBasePath = null,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var loader = new RuntimeConfigurationLoader();
        return loader.Load(configurationBasePath, environment);
    }

    private static IWrapperCatalog CreateCatalog(
        string? configurationBasePath,
        RuntimeConfiguration configuration)
    {
        var sourcePath = ResolveCatalogSourcePath(configurationBasePath, configuration);
        return new InMemoryWrapperCatalog(sourcePath);
    }

    private static string? ResolveCatalogSourcePath(
        string? configurationBasePath,
        RuntimeConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.Catalog.SourcePath))
        {
            return null;
        }

        if (Path.IsPathRooted(configuration.Catalog.SourcePath))
        {
            return configuration.Catalog.SourcePath;
        }

        var effectiveBasePath = string.IsNullOrWhiteSpace(configurationBasePath)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(configurationBasePath);

        return Path.GetFullPath(Path.Combine(effectiveBasePath, configuration.Catalog.SourcePath));
    }
}
