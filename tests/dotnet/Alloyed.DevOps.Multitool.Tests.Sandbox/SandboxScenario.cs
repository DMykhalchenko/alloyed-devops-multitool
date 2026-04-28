namespace Alloyed.DevOps.Multitool.Tests.Sandbox;

// What multitool controls (real): pipeline execution, catalog resolution, AST analysis, transformation logic.
// What Bogus emulates: script content, command selection, argument values, module names.
public sealed record SandboxScenario(
    string Label,
    string ScriptContent,
    string ModuleName,
    string[] CatalogCommandsUsed,
    string[] NonCatalogCommandsUsed,
    string? GeneratedContentShouldContain = null);
