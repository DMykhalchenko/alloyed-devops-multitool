namespace Alloyed.DevOps.Multitool.Tests.Unit.Catalog;

using Alloyed.DevOps.Multitool.Core.Catalog.Services;
using FluentAssertions;

public class InMemoryWrapperCatalogTests
{
    [Fact]
    public void Resolve_Should_MapKnownCommands_AndMarkUnknownAsMissing()
    {
        var catalog = new InMemoryWrapperCatalog();

        var result = catalog.Resolve(new[] { "Get-ChildItem", "Unknown-Command" });

        result.Replacements["Get-ChildItem"].Should().Be("Get-AlloyedChildItem");
        result.Replacements["Unknown-Command"].Should().Be("Unknown-Command");
        result.MissingCommands.Should().ContainSingle().Which.Should().Be("Unknown-Command");
        result.RequiredModules.Should().Contain("Alloyed.DevOps.Multitool");
    }

    [Fact]
    public void Resolve_Should_MapKnownAliases_ToWrappers()
    {
        var catalog = new InMemoryWrapperCatalog();

        var result = catalog.Resolve(new[] { "gci", "gi", "tp" });

        result.Replacements["gci"].Should().Be("Get-AlloyedChildItem");
        result.Replacements["gi"].Should().Be("Get-AlloyedItem");
        result.Replacements["tp"].Should().Be("Test-AlloyedPath");
        result.MissingCommands.Should().BeEmpty();
        result.RequiredModules.Should().Contain("Alloyed.DevOps.Multitool");
    }

    [Fact]
    public void Resolve_Should_MapFileSystemGroupCommands()
    {
        var catalog = new InMemoryWrapperCatalog();

        var result = catalog.Resolve(new[]
        {
            "Get-ChildItem", "Get-Item", "Test-Path",
            "Copy-Item", "Move-Item", "Remove-Item",
            "New-Item", "Get-Content", "Set-Content",
        });

        result.Replacements["Get-ChildItem"].Should().Be("Get-AlloyedChildItem");
        result.Replacements["Get-Item"].Should().Be("Get-AlloyedItem");
        result.Replacements["Test-Path"].Should().Be("Test-AlloyedPath");
        result.Replacements["Copy-Item"].Should().Be("Copy-AlloyedItem");
        result.Replacements["Move-Item"].Should().Be("Move-AlloyedItem");
        result.Replacements["Remove-Item"].Should().Be("Remove-AlloyedItem");
        result.Replacements["New-Item"].Should().Be("New-AlloyedItem");
        result.Replacements["Get-Content"].Should().Be("Get-AlloyedContent");
        result.Replacements["Set-Content"].Should().Be("Set-AlloyedContent");
        result.MissingCommands.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_Should_MapFileSystemGroupAliases()
    {
        var catalog = new InMemoryWrapperCatalog();

        var result = catalog.Resolve(new[]
        {
            "gci", "gi", "tp",
            "cp", "copy",
            "mi", "move",
            "rm", "ri", "del",
            "ni", "gc", "sc",
        });

        result.Replacements["gci"].Should().Be("Get-AlloyedChildItem");
        result.Replacements["gi"].Should().Be("Get-AlloyedItem");
        result.Replacements["tp"].Should().Be("Test-AlloyedPath");
        result.Replacements["cp"].Should().Be("Copy-AlloyedItem");
        result.Replacements["copy"].Should().Be("Copy-AlloyedItem");
        result.Replacements["mi"].Should().Be("Move-AlloyedItem");
        result.Replacements["move"].Should().Be("Move-AlloyedItem");
        result.Replacements["rm"].Should().Be("Remove-AlloyedItem");
        result.Replacements["ri"].Should().Be("Remove-AlloyedItem");
        result.Replacements["del"].Should().Be("Remove-AlloyedItem");
        result.Replacements["ni"].Should().Be("New-AlloyedItem");
        result.Replacements["gc"].Should().Be("Get-AlloyedContent");
        result.Replacements["sc"].Should().Be("Set-AlloyedContent");
        result.MissingCommands.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_Should_MapUtilityGroupCommands()
    {
        var catalog = new InMemoryWrapperCatalog();

        var result = catalog.Resolve(new[]
        {
            "Select-String", "ConvertTo-Json", "ConvertFrom-Json", "ConvertTo-Xml",
            "Get-Random", "Measure-Object", "Sort-Object", "Group-Object",
        });

        result.Replacements["Select-String"].Should().Be("Select-AlloyedString");
        result.Replacements["ConvertTo-Json"].Should().Be("ConvertTo-AlloyedJson");
        result.Replacements["ConvertFrom-Json"].Should().Be("ConvertFrom-AlloyedJson");
        result.Replacements["ConvertTo-Xml"].Should().Be("ConvertTo-AlloyedXml");
        result.Replacements["Get-Random"].Should().Be("Get-AlloyedRandom");
        result.Replacements["Measure-Object"].Should().Be("Measure-AlloyedObject");
        result.Replacements["Sort-Object"].Should().Be("Sort-AlloyedObject");
        result.Replacements["Group-Object"].Should().Be("Group-AlloyedObject");
        result.MissingCommands.Should().BeEmpty();
        result.RequiredModules.Should().Contain("Alloyed.DevOps.Multitool");
    }

    [Fact]
    public void Resolve_Should_MapUtilityGroupAliases()
    {
        var catalog = new InMemoryWrapperCatalog();

        var result = catalog.Resolve(new[] { "sls", "measure", "sort", "group" });

        result.Replacements["sls"].Should().Be("Select-AlloyedString");
        result.Replacements["measure"].Should().Be("Measure-AlloyedObject");
        result.Replacements["sort"].Should().Be("Sort-AlloyedObject");
        result.Replacements["group"].Should().Be("Group-AlloyedObject");
        result.MissingCommands.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_Should_MapDiagnosticsGroupCommands()
    {
        var catalog = new InMemoryWrapperCatalog();

        var result = catalog.Resolve(new[]
        {
            "Get-Process", "Start-Process", "Stop-Process",
            "Wait-Process", "Test-Connection", "Invoke-Command",
        });

        result.Replacements["Get-Process"].Should().Be("Get-AlloyedProcess");
        result.Replacements["Start-Process"].Should().Be("Start-AlloyedProcess");
        result.Replacements["Stop-Process"].Should().Be("Stop-AlloyedProcess");
        result.Replacements["Wait-Process"].Should().Be("Wait-AlloyedProcess");
        result.Replacements["Test-Connection"].Should().Be("Test-AlloyedConnection");
        result.Replacements["Invoke-Command"].Should().Be("Invoke-AlloyedCommand");
        result.MissingCommands.Should().BeEmpty();
        result.RequiredModules.Should().Contain("Alloyed.DevOps.Multitool");
    }

    [Fact]
    public void Resolve_Should_MapDiagnosticsGroupAliases()
    {
        var catalog = new InMemoryWrapperCatalog();

        var result = catalog.Resolve(new[] { "ps", "gps", "saps", "start", "kill", "spps", "icm" });

        result.Replacements["ps"].Should().Be("Get-AlloyedProcess");
        result.Replacements["gps"].Should().Be("Get-AlloyedProcess");
        result.Replacements["saps"].Should().Be("Start-AlloyedProcess");
        result.Replacements["start"].Should().Be("Start-AlloyedProcess");
        result.Replacements["kill"].Should().Be("Stop-AlloyedProcess");
        result.Replacements["spps"].Should().Be("Stop-AlloyedProcess");
        result.Replacements["icm"].Should().Be("Invoke-AlloyedCommand");
        result.MissingCommands.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_Should_MapArchiveGroupCommands()
    {
        var catalog = new InMemoryWrapperCatalog();

        var result = catalog.Resolve(new[] { "Compress-Archive", "Expand-Archive" });

        result.Replacements["Compress-Archive"].Should().Be("Compress-AlloyedArchive");
        result.Replacements["Expand-Archive"].Should().Be("Expand-AlloyedArchive");
        result.MissingCommands.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_Should_MapManagementGroupCommands()
    {
        var catalog = new InMemoryWrapperCatalog();

        var result = catalog.Resolve(new[]
        {
            "Get-Service", "Start-Service", "Stop-Service", "Restart-Service",
        });

        result.Replacements["Get-Service"].Should().Be("Get-AlloyedService");
        result.Replacements["Start-Service"].Should().Be("Start-AlloyedService");
        result.Replacements["Stop-Service"].Should().Be("Stop-AlloyedService");
        result.Replacements["Restart-Service"].Should().Be("Restart-AlloyedService");
        result.MissingCommands.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_Should_MapManagementGroupAliases()
    {
        var catalog = new InMemoryWrapperCatalog();

        var result = catalog.Resolve(new[] { "gsv", "sasv", "spsv" });

        result.Replacements["gsv"].Should().Be("Get-AlloyedService");
        result.Replacements["sasv"].Should().Be("Start-AlloyedService");
        result.Replacements["spsv"].Should().Be("Stop-AlloyedService");
        result.MissingCommands.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_Should_MapSecurityGroupCommands()
    {
        var catalog = new InMemoryWrapperCatalog();

        var result = catalog.Resolve(new[]
        {
            "Get-Acl", "Set-Acl", "Get-Credential",
            "ConvertTo-SecureString", "ConvertFrom-SecureString",
            "Get-AuthenticodeSignature", "Set-AuthenticodeSignature",
            "New-SelfSignedCertificate", "Get-PfxCertificate", "Export-PfxCertificate",
        });

        result.Replacements["Get-Acl"].Should().Be("Get-AlloyedAcl");
        result.Replacements["Set-Acl"].Should().Be("Set-AlloyedAcl");
        result.Replacements["Get-Credential"].Should().Be("Get-AlloyedCredential");
        result.Replacements["ConvertTo-SecureString"].Should().Be("ConvertTo-AlloyedSecureString");
        result.Replacements["ConvertFrom-SecureString"].Should().Be("ConvertFrom-AlloyedSecureString");
        result.Replacements["Get-AuthenticodeSignature"].Should().Be("Get-AlloyedAuthenticodeSignature");
        result.Replacements["Set-AuthenticodeSignature"].Should().Be("Set-AlloyedAuthenticodeSignature");
        result.Replacements["New-SelfSignedCertificate"].Should().Be("New-AlloyedSelfSignedCertificate");
        result.Replacements["Get-PfxCertificate"].Should().Be("Get-AlloyedPfxCertificate");
        result.Replacements["Export-PfxCertificate"].Should().Be("Export-AlloyedPfxCertificate");
        result.MissingCommands.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_Should_MapHostGroupCommands()
    {
        var catalog = new InMemoryWrapperCatalog();

        var result = catalog.Resolve(new[]
        {
            "Write-Host", "Read-Host", "Write-Progress", "Clear-Host",
        });

        result.Replacements["Write-Host"].Should().Be("Write-AlloyedHost");
        result.Replacements["Read-Host"].Should().Be("Read-AlloyedHost");
        result.Replacements["Write-Progress"].Should().Be("Write-AlloyedProgress");
        result.Replacements["Clear-Host"].Should().Be("Clear-AlloyedHost");
        result.MissingCommands.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_Should_MapHostGroupAliases()
    {
        var catalog = new InMemoryWrapperCatalog();

        var result = catalog.Resolve(new[] { "cls", "clear" });

        result.Replacements["cls"].Should().Be("Clear-AlloyedHost");
        result.Replacements["clear"].Should().Be("Clear-AlloyedHost");
        result.MissingCommands.Should().BeEmpty();
    }
}
