namespace Alloyed.DevOps.Multitool.Tests.Sandbox;

using Alloyed.DevOps.Multitool.Host.PowerShell.Models;
using Alloyed.DevOps.Multitool.Host.PowerShell.Services;

// Split model:
//   REAL (controlled by multitool): PowerShellScriptAnalyzer, InMemoryWrapperCatalog,
//       TextCommandTransformer, MinimalModuleBuilder, TransformationPipeline.
//   EMULATED (Bogus-generated): script content, command selection, argument values, module names.
//
// All scenarios use fixed seeds so the generated inputs are identical across runs and environments.
public class SandboxPipelineScenarioTests
{
    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Pipeline_ShouldHandle_SandboxScenario(SandboxScenario scenario)
    {
        var root = Path.Combine(Path.GetTempPath(), "alloyed-sandbox", Guid.NewGuid().ToString("N"));
        var scriptPath = Path.Combine(root, $"{scenario.ModuleName}.ps1");
        var outputPath = Path.Combine(root, "out");

        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(scriptPath, scenario.ScriptContent);

            var pipeline = PipelineBootstrap.CreateDefault();
            var result = pipeline.Execute(
                new PipelineRequest(scriptPath, scenario.ModuleName, outputPath, Force: true));

            result.Success.Should().BeTrue(because: $"[{scenario.Label}] pipeline must succeed");

            result.CommandsReplaced.Should().Be(
                scenario.CatalogCommandsUsed.Length,
                because: $"[{scenario.Label}] every distinct catalog command in the script must produce one replacement");

            result.MissingCommands.Should().BeEquivalentTo(
                scenario.NonCatalogCommandsUsed,
                because: $"[{scenario.Label}] non-catalog commands must be flagged as missing");

            if (scenario.GeneratedContentShouldContain is not null)
            {
                var psm1 = File.ReadAllText(
                    Path.Combine(result.ModulePath, $"{scenario.ModuleName}.psm1"));
                psm1.Should().Contain(
                    scenario.GeneratedContentShouldContain,
                    because: $"[{scenario.Label}] generated module content must preserve embedded strings unchanged");
            }
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    public static IEnumerable<object[]> Scenarios() =>
        BuildScenarios().Select(static s => new object[] { s });

    private static IEnumerable<SandboxScenario> BuildScenarios()
    {
        var gen = new SandboxScriptFaker(SandboxSeeds.Default);

        // 1. Five catalog commands — all replaced, none missing.
        var allCatalog = gen.PickCatalogCommands(5);
        yield return new SandboxScenario(
            Label: "AllCatalog_5Commands_Seed42",
            ScriptContent: gen.BuildScript(allCatalog),
            ModuleName: gen.ModuleName(),
            CatalogCommandsUsed: allCatalog,
            NonCatalogCommandsUsed: []);

        // 2. Three catalog + two unknown — partial replacement, two missing.
        var mixedGen = new SandboxScriptFaker(SandboxSeeds.Mixed);
        var mixedCatalog = mixedGen.PickCatalogCommands(3);
        var mixedUnknown = mixedGen.PickNonCatalogCommands(2);
        yield return new SandboxScenario(
            Label: "Mixed_3Catalog_2Unknown_Seed137",
            ScriptContent: mixedGen.BuildScript([.. mixedCatalog, .. mixedUnknown]),
            ModuleName: mixedGen.ModuleName(),
            CatalogCommandsUsed: mixedCatalog,
            NonCatalogCommandsUsed: mixedUnknown);

        // 3. Only unknown commands — zero replacements, all missing.
        var unknownGen = new SandboxScriptFaker(SandboxSeeds.Default);
        var unknownOnly = unknownGen.PickNonCatalogCommands(3);
        yield return new SandboxScenario(
            Label: "OnlyUnknown_3Commands_Seed42",
            ScriptContent: unknownGen.BuildScript(unknownOnly),
            ModuleName: unknownGen.ModuleName(),
            CatalogCommandsUsed: [],
            NonCatalogCommandsUsed: unknownOnly);

        // 4. Empty script — nothing found, nothing replaced.
        yield return new SandboxScenario(
            Label: "EmptyScript",
            ScriptContent: string.Empty,
            ModuleName: "EmptySandboxModule",
            CatalogCommandsUsed: [],
            NonCatalogCommandsUsed: []);

        // 5. Large script: 20 catalog commands — all replaced.
        var largeGen = new SandboxScriptFaker(SandboxSeeds.Large);
        var largeCatalog = largeGen.PickCatalogCommands(20);
        yield return new SandboxScenario(
            Label: "LargeScript_20CatalogCommands_Seed2718",
            ScriptContent: largeGen.BuildScript(largeCatalog),
            ModuleName: largeGen.ModuleName(),
            CatalogCommandsUsed: largeCatalog,
            NonCatalogCommandsUsed: []);

        // 6. Aliases only — gci/gi/tp resolve via catalog alias map, all replaced.
        var aliasGen = new SandboxScriptFaker(SandboxSeeds.Default);
        yield return new SandboxScenario(
            Label: "AliasesOnly_gci_gi_tp_Seed42",
            ScriptContent: aliasGen.BuildScript(SandboxScriptFaker.CatalogAliases),
            ModuleName: aliasGen.ModuleName(),
            CatalogCommandsUsed: SandboxScriptFaker.CatalogAliases,
            NonCatalogCommandsUsed: []);

        // 7. Commands embedded inside double-quoted strings are NOT replaced by the transformer.
        //    The outer Write-Host call IS in the catalog and IS replaced.
        //    The embedded command names (Get-ChildItem, Test-Path) appear only as string content
        //    and must survive unchanged in the generated module.
        const string embeddedCmd = "Get-ChildItem";
        var stringsGen = new SandboxScriptFaker(SandboxSeeds.Default);
        var stringsScript = stringsGen.BuildScriptWithCommandsEmbeddedInStrings(
            outerCatalogCommand: "Write-Host",
            embeddedCommands: [embeddedCmd, "Test-Path"]);
        yield return new SandboxScenario(
            Label: "CommandsEmbeddedInStrings_Seed42",
            ScriptContent: stringsScript,
            ModuleName: stringsGen.ModuleName(),
            CatalogCommandsUsed: ["Write-Host"],
            NonCatalogCommandsUsed: [],
            GeneratedContentShouldContain: embeddedCmd);
    }
}
