namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Alloyed.DevOps.Multitool.Core.Ast.Contracts;
using Alloyed.DevOps.Multitool.Core.Ast.Services;
using Alloyed.DevOps.Multitool.Core.Builders.Contracts;
using Alloyed.DevOps.Multitool.Core.Builders.Services;
using Alloyed.DevOps.Multitool.Core.Catalog.Contracts;
using Alloyed.DevOps.Multitool.Core.Catalog.Services;
using Alloyed.DevOps.Multitool.Host.PowerShell.Contracts;
using Alloyed.DevOps.Multitool.Host.PowerShell.Models;

/// <summary>
/// Static factory that wires up the full transformation pipeline and its dependencies without
/// requiring an IoC container. Entry point for PowerShell cmdlets and other host integrations.
/// </summary>
public static class PipelineBootstrap
{
    /// <summary>
    /// Creates a fully configured <see cref="ITransformationPipeline"/> using production
    /// implementations of all services, with configuration loaded from the standard hierarchy
    /// (defaults → JSON → YAML → environment variables).
    /// </summary>
    /// <param name="configurationBasePath">
    /// Base directory from which <c>config/appsettings.json</c> and
    /// <c>config/appsettings.yml</c> are resolved. Defaults to the current working directory.
    /// </param>
    /// <param name="environment">
    /// Optional environment variable overrides. When <see langword="null"/>, the process
    /// environment is read directly.
    /// </param>
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

    /// <summary>
    /// Creates a standalone <see cref="IWrapperCatalog"/> using the catalog source path resolved
    /// from configuration. Useful when callers need catalog access without running a full pipeline.
    /// </summary>
    /// <param name="configurationBasePath">Base directory for config file resolution.</param>
    /// <param name="environment">Optional environment variable overrides.</param>
    public static IWrapperCatalog CreateCatalog(
        string? configurationBasePath = null,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var configuration = CreateRuntimeConfiguration(configurationBasePath, environment);
        return CreateCatalog(configurationBasePath, configuration);
    }

    /// <summary>
    /// Loads and returns the <see cref="RuntimeConfiguration"/> for the given base path and
    /// environment without constructing the full pipeline.
    /// </summary>
    /// <param name="configurationBasePath">Base directory for config file resolution.</param>
    /// <param name="environment">Optional environment variable overrides.</param>
    public static RuntimeConfiguration CreateRuntimeConfiguration(
        string? configurationBasePath = null,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var loader = new RuntimeConfigurationLoader();
        return loader.Load(configurationBasePath, environment);
    }

    /// <summary>
    /// Resolves the catalog source path from <paramref name="configuration"/> and creates an
    /// <see cref="InMemoryWrapperCatalog"/>, using the embedded catalog when no path is configured.
    /// </summary>
    private static IWrapperCatalog CreateCatalog(
        string? configurationBasePath,
        RuntimeConfiguration configuration)
    {
        var sourcePath = ResolveCatalogSourcePath(configurationBasePath, configuration);
        return new InMemoryWrapperCatalog(sourcePath);
    }

    /// <summary>
    /// Resolves the effective catalog file path. Relative paths in configuration are resolved
    /// against <paramref name="configurationBasePath"/> (or the current working directory).
    /// Returns <see langword="null"/> when no explicit source path is configured, causing the
    /// embedded catalog to be used.
    /// </summary>
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
