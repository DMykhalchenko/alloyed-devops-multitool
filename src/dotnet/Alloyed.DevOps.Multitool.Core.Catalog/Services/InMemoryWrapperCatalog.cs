namespace Alloyed.DevOps.Multitool.Core.Catalog.Services;

using Alloyed.DevOps.Multitool.Core.Catalog.Contracts;
using Alloyed.DevOps.Multitool.Core.Catalog.Models;

public sealed class InMemoryWrapperCatalog : IWrapperCatalog
{
    private const string WrapperModuleName = "Alloyed.DevOps.Multitool";

    private static readonly IReadOnlyDictionary<string, string> WrapperMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Provider.FileSystem
            ["Get-ChildItem"] = "Get-AlloyedChildItem",
            ["Get-Item"]      = "Get-AlloyedItem",
            ["Test-Path"]     = "Test-AlloyedPath",
            ["Copy-Item"]     = "Copy-AlloyedItem",
            ["Move-Item"]     = "Move-AlloyedItem",
            ["Remove-Item"]   = "Remove-AlloyedItem",
            ["New-Item"]      = "New-AlloyedItem",
            ["Get-Content"]   = "Get-AlloyedContent",
            ["Set-Content"]   = "Set-AlloyedContent",
            ["gci"]           = "Get-AlloyedChildItem",
            ["gi"]            = "Get-AlloyedItem",
            ["tp"]            = "Test-AlloyedPath",
            ["cp"]            = "Copy-AlloyedItem",
            ["copy"]          = "Copy-AlloyedItem",
            ["mi"]            = "Move-AlloyedItem",
            ["move"]          = "Move-AlloyedItem",
            ["rm"]            = "Remove-AlloyedItem",
            ["ri"]            = "Remove-AlloyedItem",
            ["del"]           = "Remove-AlloyedItem",
            ["ni"]            = "New-AlloyedItem",
            ["gc"]            = "Get-AlloyedContent",
            ["sc"]            = "Set-AlloyedContent",

            // System.Utility
            ["Select-String"]    = "Select-AlloyedString",
            ["ConvertTo-Json"]   = "ConvertTo-AlloyedJson",
            ["ConvertFrom-Json"] = "ConvertFrom-AlloyedJson",
            ["ConvertTo-Xml"]    = "ConvertTo-AlloyedXml",
            ["Get-Random"]       = "Get-AlloyedRandom",
            ["Measure-Object"]   = "Measure-AlloyedObject",
            ["Sort-Object"]      = "Sort-AlloyedObject",
            ["Group-Object"]     = "Group-AlloyedObject",
            ["sls"]              = "Select-AlloyedString",
            ["measure"]          = "Measure-AlloyedObject",
            ["sort"]             = "Sort-AlloyedObject",
            ["group"]            = "Group-AlloyedObject",

            // System.Diagnostics
            ["Get-Process"]      = "Get-AlloyedProcess",
            ["Start-Process"]    = "Start-AlloyedProcess",
            ["Stop-Process"]     = "Stop-AlloyedProcess",
            ["Wait-Process"]     = "Wait-AlloyedProcess",
            ["Test-Connection"]  = "Test-AlloyedConnection",
            ["Invoke-Command"]   = "Invoke-AlloyedCommand",
            ["ps"]               = "Get-AlloyedProcess",
            ["gps"]              = "Get-AlloyedProcess",
            ["saps"]             = "Start-AlloyedProcess",
            ["start"]            = "Start-AlloyedProcess",
            ["kill"]             = "Stop-AlloyedProcess",
            ["spps"]             = "Stop-AlloyedProcess",
            ["icm"]              = "Invoke-AlloyedCommand",

            // System.Archive
            ["Compress-Archive"] = "Compress-AlloyedArchive",
            ["Expand-Archive"]   = "Expand-AlloyedArchive",

            // System.Management
            ["Get-Service"]      = "Get-AlloyedService",
            ["Start-Service"]    = "Start-AlloyedService",
            ["Stop-Service"]     = "Stop-AlloyedService",
            ["Restart-Service"]  = "Restart-AlloyedService",
            ["gsv"]              = "Get-AlloyedService",
            ["sasv"]             = "Start-AlloyedService",
            ["spsv"]             = "Stop-AlloyedService",

            // System.Security
            ["Get-Acl"]                   = "Get-AlloyedAcl",
            ["Set-Acl"]                   = "Set-AlloyedAcl",
            ["Get-Credential"]            = "Get-AlloyedCredential",
            ["ConvertTo-SecureString"]    = "ConvertTo-AlloyedSecureString",
            ["ConvertFrom-SecureString"]  = "ConvertFrom-AlloyedSecureString",
            ["Get-AuthenticodeSignature"] = "Get-AlloyedAuthenticodeSignature",
            ["Set-AuthenticodeSignature"] = "Set-AlloyedAuthenticodeSignature",
            ["New-SelfSignedCertificate"] = "New-AlloyedSelfSignedCertificate",
            ["Get-PfxCertificate"]        = "Get-AlloyedPfxCertificate",
            ["Export-PfxCertificate"]     = "Export-AlloyedPfxCertificate",

            // System.Host
            ["Write-Host"]     = "Write-AlloyedHost",
            ["Read-Host"]      = "Read-AlloyedHost",
            ["Write-Progress"] = "Write-AlloyedProgress",
            ["Clear-Host"]     = "Clear-AlloyedHost",
            ["cls"]            = "Clear-AlloyedHost",
            ["clear"]          = "Clear-AlloyedHost",
        };

    public bool HasWrapper(string commandName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);
        return WrapperMap.ContainsKey(commandName);
    }

    public string GetWrapperName(string commandName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);

        if (!WrapperMap.TryGetValue(commandName, out var wrapperName))
        {
            throw new KeyNotFoundException($"Wrapper mapping was not found for command '{commandName}'.");
        }

        return wrapperName;
    }

    public ResolutionResult Resolve(IEnumerable<string> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        var normalized = commands
            .Where(static c => !string.IsNullOrWhiteSpace(c))
            .Select(static c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var missing = new List<string>();

        foreach (var command in normalized)
        {
            if (WrapperMap.TryGetValue(command, out var wrapper))
            {
                replacements[command] = wrapper;
                continue;
            }

            replacements[command] = command;
            missing.Add(command);
        }

        var requiredModules = GetRequiredModules(normalized);

        return new ResolutionResult(
            Replacements: replacements,
            MissingCommands: missing,
            RequiredModules: requiredModules);
    }

    public IReadOnlyList<string> GetRequiredModules(IEnumerable<string> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        var hasMappedCommands = commands.Any(static c =>
            !string.IsNullOrWhiteSpace(c) && WrapperMap.ContainsKey(c.Trim()));

        if (!hasMappedCommands)
        {
            return Array.Empty<string>();
        }

        return new[] { WrapperModuleName };
    }

    public IReadOnlyDictionary<string, string> GetMappings()
    {
        return WrapperMap
            .OrderBy(static kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static kv => kv.Key, static kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    }
}
