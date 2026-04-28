namespace Alloyed.DevOps.Multitool.Tests.Sandbox;

using Bogus;

// Generates deterministic PowerShell script content for sandbox scenarios.
// All paths and argument values are Bogus-generated — only command names are significant.
internal sealed class SandboxScriptFaker
{
    // Canonical command names present in ports.catalog.json.
    internal static readonly string[] CatalogCommands =
    [
        "Get-ChildItem", "Get-Item", "Test-Path", "Copy-Item", "Move-Item",
        "Remove-Item", "New-Item", "Get-Content", "Set-Content", "Get-Process",
        "Stop-Process", "Start-Process", "Get-Service", "Start-Service", "Stop-Service",
        "Write-Host", "Read-Host", "Get-Location", "Set-Location", "Join-Path",
        "Split-Path", "Resolve-Path", "Test-Connection", "Invoke-Command",
        "ConvertTo-Json", "ConvertFrom-Json", "Sort-Object", "Group-Object",
        "Measure-Object", "Select-String", "Compress-Archive", "Expand-Archive",
        "Get-Acl", "Set-Acl", "Get-Credential", "Get-Random", "Write-Progress",
    ];

    // Aliases present in ports.catalog.json that resolve to catalog wrappers.
    internal static readonly string[] CatalogAliases = ["gci", "gi", "tp"];

    // Commands that are NOT in the catalog — pipeline will flag them as missing.
    internal static readonly string[] NonCatalogCommands =
    [
        "Do-Business", "Invoke-Work", "Get-Report", "Send-Notification",
        "Sync-Data", "Export-Results", "Import-Records", "Test-Scenario",
    ];

    private readonly Faker _faker;

    internal SandboxScriptFaker(int seed = SandboxSeeds.Default)
    {
        _faker = new Faker { Random = new Randomizer(seed) };
    }

    internal string ModuleName()
    {
        var word = _faker.Commerce.Department().Replace(" ", string.Empty, StringComparison.Ordinal);
        return $"{word}Module";
    }

    internal string[] PickCatalogCommands(int count)
    {
        var capped = Math.Min(count, CatalogCommands.Length);
        return _faker.Random.ListItems(CatalogCommands, capped).ToArray();
    }

    internal string[] PickNonCatalogCommands(int count)
    {
        var capped = Math.Min(count, NonCatalogCommands.Length);
        return _faker.Random.ListItems(NonCatalogCommands, capped).ToArray();
    }

    internal string BuildScript(IEnumerable<string> commands)
    {
        return string.Join("\n", commands.Select(BuildCommandLine));
    }

    // Generates a script where the given commands appear only inside double-quoted strings,
    // not as actual command invocations. The outer call uses a catalog command so the
    // pipeline still has something real to replace, exercising the string-preservation path.
    internal string BuildScriptWithCommandsEmbeddedInStrings(
        string outerCatalogCommand,
        IEnumerable<string> embeddedCommands)
    {
        var lines = embeddedCommands
            .Select(cmd => $"{outerCatalogCommand} \"{cmd} is redirected to its Alloyed wrapper\"");
        return string.Join("\n", lines);
    }

    private string BuildCommandLine(string command)
    {
        var noun = _faker.Lorem.Word();
        return $"{command} -Path \"./{noun}\"";
    }
}
