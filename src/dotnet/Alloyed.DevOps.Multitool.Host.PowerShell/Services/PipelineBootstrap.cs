namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Alloyed.DevOps.Multitool.Core.Ast.Contracts;
using Alloyed.DevOps.Multitool.Core.Ast.Services;
using Alloyed.DevOps.Multitool.Core.Builders.Contracts;
using Alloyed.DevOps.Multitool.Core.Builders.Services;
using Alloyed.DevOps.Multitool.Core.Catalog.Contracts;
using Alloyed.DevOps.Multitool.Core.Catalog.Services;
using Alloyed.DevOps.Multitool.Host.PowerShell.Contracts;

public static class PipelineBootstrap
{
    public static ITransformationPipeline CreateDefault()
    {
        IScriptAnalyzer analyzer = new HeuristicScriptAnalyzer();
        IWrapperCatalog catalog = new InMemoryWrapperCatalog();
        ICommandTransformer transformer = new TextCommandTransformer();
        IModuleBuilder moduleBuilder = new MinimalModuleBuilder();

        return new TransformationPipeline(analyzer, catalog, transformer, moduleBuilder);
    }

    public static IWrapperCatalog CreateCatalog()
    {
        return new InMemoryWrapperCatalog();
    }
}
