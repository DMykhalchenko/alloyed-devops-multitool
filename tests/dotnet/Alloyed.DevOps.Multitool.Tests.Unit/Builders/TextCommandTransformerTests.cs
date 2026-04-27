namespace Alloyed.DevOps.Multitool.Tests.Unit.Builders;

using Alloyed.DevOps.Multitool.Core.Builders.Services;
using FluentAssertions;

public class TextCommandTransformerTests
{
    [Fact]
    public void Transform_Should_ReplaceOnlyWholeCommandTokens()
    {
        var transformer = new TextCommandTransformer();
        var source = "Get-ChildItem -Path .\nGet-ChildItemExtra";
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Get-ChildItem"] = "Get-AlloyedChildItem",
        };

        var output = transformer.Transform(source, map);

        output.Should().Contain("Get-AlloyedChildItem -Path .");
        output.Should().Contain("Get-ChildItemExtra");
    }

    [Fact]
    public void Transform_Should_NotReplaceInsideQuotes_OrComments()
    {
        var transformer = new TextCommandTransformer();
        var source = "Write-Host \"Get-ChildItem\"\nGet-ChildItem -Path . # Get-ChildItem";
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Get-ChildItem"] = "Get-AlloyedChildItem",
        };

        var output = transformer.Transform(source, map);

        output.Should().Contain("Write-Host \"Get-ChildItem\"");
        output.Should().Contain("Get-AlloyedChildItem -Path . # Get-ChildItem");
        output.Should().NotContain("\"Get-AlloyedChildItem\"");
    }

    [Fact]
    public void Transform_Should_NotReplaceInsideHereString()
    {
        var transformer = new TextCommandTransformer();
        var source = "@\"\nGet-ChildItem -Path .\n\"@\nGet-ChildItem -Path .";
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Get-ChildItem"] = "Get-AlloyedChildItem",
        };

        var output = transformer.Transform(source, map);

        output.Should().Contain("@\"\nGet-ChildItem -Path .\n\"@");
        output.Should().Contain("Get-AlloyedChildItem -Path .");
    }

    [Fact]
    public void Transform_Should_MatchGoldenFixtureOutput()
    {
        var transformer = new TextCommandTransformer();
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Transform");
        var inputPath = Path.Combine(fixtureDir, "golden-input.ps1");
        var expectedPath = Path.Combine(fixtureDir, "golden-expected.ps1");

        var source = File.ReadAllText(inputPath);
        var expected = File.ReadAllText(expectedPath);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Get-ChildItem"] = "Get-AlloyedChildItem",
            ["Get-Item"] = "Get-AlloyedItem",
            ["Test-Path"] = "Test-AlloyedPath",
        };

        var output = transformer.Transform(source, map);

        output.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Should()
            .Be(expected.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Transform_Should_MatchDiagnosticsGroupGoldenFixtureOutput()
    {
        var transformer = new TextCommandTransformer();
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Transform");
        var inputPath = Path.Combine(fixtureDir, "diagnostics-golden-input.ps1");
        var expectedPath = Path.Combine(fixtureDir, "diagnostics-golden-expected.ps1");

        var source = File.ReadAllText(inputPath);
        var expected = File.ReadAllText(expectedPath);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Get-Process"]     = "Get-AlloyedProcess",
            ["Start-Process"]   = "Start-AlloyedProcess",
            ["Stop-Process"]    = "Stop-AlloyedProcess",
            ["Wait-Process"]    = "Wait-AlloyedProcess",
            ["Test-Connection"] = "Test-AlloyedConnection",
            ["Invoke-Command"]  = "Invoke-AlloyedCommand",
        };

        var output = transformer.Transform(source, map);

        output.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Should()
            .Be(expected.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Transform_Should_MatchArchiveGroupGoldenFixtureOutput()
    {
        var transformer = new TextCommandTransformer();
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Transform");

        var source = File.ReadAllText(Path.Combine(fixtureDir, "archive-golden-input.ps1"));
        var expected = File.ReadAllText(Path.Combine(fixtureDir, "archive-golden-expected.ps1"));

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Compress-Archive"] = "Compress-AlloyedArchive",
            ["Expand-Archive"]   = "Expand-AlloyedArchive",
        };

        transformer.Transform(source, map)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Should()
            .Be(expected.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Transform_Should_MatchManagementGroupGoldenFixtureOutput()
    {
        var transformer = new TextCommandTransformer();
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Transform");

        var source = File.ReadAllText(Path.Combine(fixtureDir, "management-golden-input.ps1"));
        var expected = File.ReadAllText(Path.Combine(fixtureDir, "management-golden-expected.ps1"));

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Get-Service"]     = "Get-AlloyedService",
            ["Start-Service"]   = "Start-AlloyedService",
            ["Stop-Service"]    = "Stop-AlloyedService",
            ["Restart-Service"] = "Restart-AlloyedService",
        };

        transformer.Transform(source, map)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Should()
            .Be(expected.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Transform_Should_MatchSecurityGroupGoldenFixtureOutput()
    {
        var transformer = new TextCommandTransformer();
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Transform");

        var source = File.ReadAllText(Path.Combine(fixtureDir, "security-golden-input.ps1"));
        var expected = File.ReadAllText(Path.Combine(fixtureDir, "security-golden-expected.ps1"));

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
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
        };

        transformer.Transform(source, map)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Should()
            .Be(expected.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Transform_Should_MatchHostGroupGoldenFixtureOutput()
    {
        var transformer = new TextCommandTransformer();
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Transform");

        var source = File.ReadAllText(Path.Combine(fixtureDir, "host-golden-input.ps1"));
        var expected = File.ReadAllText(Path.Combine(fixtureDir, "host-golden-expected.ps1"));

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Write-Host"]     = "Write-AlloyedHost",
            ["Read-Host"]      = "Read-AlloyedHost",
            ["Write-Progress"] = "Write-AlloyedProgress",
            ["Clear-Host"]     = "Clear-AlloyedHost",
        };

        transformer.Transform(source, map)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Should()
            .Be(expected.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Transform_Should_MatchFileSystemGroupGoldenFixtureOutput()
    {
        var transformer = new TextCommandTransformer();
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Transform");
        var inputPath = Path.Combine(fixtureDir, "filesystem-golden-input.ps1");
        var expectedPath = Path.Combine(fixtureDir, "filesystem-golden-expected.ps1");

        var source = File.ReadAllText(inputPath);
        var expected = File.ReadAllText(expectedPath);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Copy-Item"] = "Copy-AlloyedItem",
            ["Move-Item"] = "Move-AlloyedItem",
            ["Remove-Item"] = "Remove-AlloyedItem",
            ["New-Item"] = "New-AlloyedItem",
            ["Get-Content"] = "Get-AlloyedContent",
            ["Set-Content"] = "Set-AlloyedContent",
        };

        var output = transformer.Transform(source, map);

        output.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Should()
            .Be(expected.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Transform_Should_MatchPathLocationGroupGoldenFixtureOutput()
    {
        var transformer = new TextCommandTransformer();
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Transform");
        var inputPath = Path.Combine(fixtureDir, "path-location-golden-input.ps1");
        var expectedPath = Path.Combine(fixtureDir, "path-location-golden-expected.ps1");

        var source = File.ReadAllText(inputPath);
        var expected = File.ReadAllText(expectedPath);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Get-Location"] = "Get-AlloyedLocation",
            ["Set-Location"] = "Set-AlloyedLocation",
            ["Push-Location"] = "Push-AlloyedLocation",
            ["Pop-Location"] = "Pop-AlloyedLocation",
            ["Join-Path"] = "Join-AlloyedPath",
            ["Split-Path"] = "Split-AlloyedPath",
            ["Resolve-Path"] = "Resolve-AlloyedPath",
        };

        var output = transformer.Transform(source, map);

        output.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Should()
            .Be(expected.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Transform_Should_MatchUtilityGroupGoldenFixtureOutput()
    {
        var transformer = new TextCommandTransformer();
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Transform");
        var inputPath = Path.Combine(fixtureDir, "utility-golden-input.ps1");
        var expectedPath = Path.Combine(fixtureDir, "utility-golden-expected.ps1");

        var source = File.ReadAllText(inputPath);
        var expected = File.ReadAllText(expectedPath);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Select-String"]    = "Select-AlloyedString",
            ["ConvertTo-Json"]   = "ConvertTo-AlloyedJson",
            ["ConvertFrom-Json"] = "ConvertFrom-AlloyedJson",
            ["ConvertTo-Xml"]    = "ConvertTo-AlloyedXml",
            ["Get-Random"]       = "Get-AlloyedRandom",
            ["Measure-Object"]   = "Measure-AlloyedObject",
            ["Sort-Object"]      = "Sort-AlloyedObject",
            ["Group-Object"]     = "Group-AlloyedObject",
        };

        var output = transformer.Transform(source, map);

        output.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Should()
            .Be(expected.Replace("\r\n", "\n", StringComparison.Ordinal));
    }
}
