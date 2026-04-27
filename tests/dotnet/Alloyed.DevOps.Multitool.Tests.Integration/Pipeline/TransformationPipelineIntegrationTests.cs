namespace Alloyed.DevOps.Multitool.Tests.Integration.Pipeline;

using System.Text.RegularExpressions;
using Alloyed.DevOps.Multitool.Host.PowerShell.Models;
using Alloyed.DevOps.Multitool.Host.PowerShell.Services;

public class TransformationPipelineIntegrationTests
{
    [Fact]
    public void Execute_Should_GenerateModule_AndReportReplacements()
    {
        var root = Path.Combine(Path.GetTempPath(), "alloyed-integration-tests", Guid.NewGuid().ToString("N"));
        var scriptPath = Path.Combine(root, "input.ps1");
        var outputPath = Path.Combine(root, "out");

        Directory.CreateDirectory(root);
        File.WriteAllText(scriptPath, "Get-ChildItem -Path .\nGet-Item -Path .\nTest-Path -Path .");

        try
        {
            var pipeline = PipelineBootstrap.CreateDefault();
            var result = pipeline.Execute(new PipelineRequest(scriptPath, "IntegrationModule", outputPath, true));

            result.Success.Should().BeTrue();
            result.CommandsFound.Should().Be(3);
            result.CommandsReplaced.Should().Be(3);
            result.MissingCommands.Should().BeEmpty();
            result.Diagnostics.Should().BeEmpty();

            var generatedPsm1 = Path.Combine(result.ModulePath, "IntegrationModule.psm1");
            File.Exists(generatedPsm1).Should().BeTrue();

            var content = File.ReadAllText(generatedPsm1);
            content.Should().Contain("Get-AlloyedChildItem");
            content.Should().Contain("Get-AlloyedItem");
            content.Should().Contain("Test-AlloyedPath");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Execute_Should_ReplaceSupportedAliases()
    {
        var root = Path.Combine(Path.GetTempPath(), "alloyed-integration-tests", Guid.NewGuid().ToString("N"));
        var scriptPath = Path.Combine(root, "input.ps1");
        var outputPath = Path.Combine(root, "out");

        Directory.CreateDirectory(root);
        File.WriteAllText(scriptPath, "gci -Path .\ngi -Path .\ntp -Path .");

        try
        {
            var pipeline = PipelineBootstrap.CreateDefault();
            var result = pipeline.Execute(new PipelineRequest(scriptPath, "AliasIntegrationModule", outputPath, true));

            result.Success.Should().BeTrue();
            result.CommandsFound.Should().Be(3);
            result.CommandsReplaced.Should().Be(3);
            result.MissingCommands.Should().BeEmpty();

            var generatedPsm1 = Path.Combine(result.ModulePath, "AliasIntegrationModule.psm1");
            File.Exists(generatedPsm1).Should().BeTrue();

            var content = File.ReadAllText(generatedPsm1);
            content.Should().Contain("Get-AlloyedChildItem");
            content.Should().Contain("Get-AlloyedItem");
            content.Should().Contain("Test-AlloyedPath");
            content.Should().NotContain("gci -Path .");
            content.Should().NotContain("gi -Path .");
            content.Should().NotContain("tp -Path .");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Execute_Should_ReplaceFileSystemPorts()
    {
        var root = Path.Combine(Path.GetTempPath(), "alloyed-integration-tests", Guid.NewGuid().ToString("N"));
        var scriptPath = Path.Combine(root, "input.ps1");
        var outputPath = Path.Combine(root, "out");

        Directory.CreateDirectory(root);
        File.WriteAllText(scriptPath, """
            Copy-Item -Path ./a.txt -Destination ./b.txt
            Move-Item -Path ./b.txt -Destination ./c.txt
            Remove-Item -Path ./c.txt -Force
            New-Item -Path ./new.txt -ItemType File
            Get-Content -Path ./new.txt
            Set-Content -Path ./new.txt -Value "hello"
            """);

        try
        {
            var pipeline = PipelineBootstrap.CreateDefault();
            var result = pipeline.Execute(new PipelineRequest(scriptPath, "FileSystemIntegrationModule", outputPath, true));

            result.Success.Should().BeTrue();
            result.CommandsFound.Should().Be(6);
            result.CommandsReplaced.Should().Be(6);
            result.MissingCommands.Should().BeEmpty();

            var generatedPsm1 = Path.Combine(result.ModulePath, "FileSystemIntegrationModule.psm1");
            File.Exists(generatedPsm1).Should().BeTrue();

            var content = File.ReadAllText(generatedPsm1);
            content.Should().Contain("Copy-AlloyedItem");
            content.Should().Contain("Move-AlloyedItem");
            content.Should().Contain("Remove-AlloyedItem");
            content.Should().Contain("New-AlloyedItem");
            content.Should().Contain("Get-AlloyedContent");
            content.Should().Contain("Set-AlloyedContent");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Execute_Should_RefuseOverwrite_WhenForceIsFalse()
    {
        var root = Path.Combine(Path.GetTempPath(), "alloyed-integration-tests", Guid.NewGuid().ToString("N"));
        var scriptPath = Path.Combine(root, "input.ps1");
        var outputPath = Path.Combine(root, "out");
        var modulePath = Path.Combine(outputPath, "ExistingModule");

        Directory.CreateDirectory(modulePath);
        Directory.CreateDirectory(root);
        File.WriteAllText(scriptPath, "Get-ChildItem -Path .");

        try
        {
            var pipeline = PipelineBootstrap.CreateDefault();
            var result = pipeline.Execute(new PipelineRequest(scriptPath, "ExistingModule", outputPath, false));

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("already exists");
            result.ModulePath.Should().Be(modulePath);
            result.Diagnostics.Should().ContainSingle(d => d.Code == "PIPELINE-OUTPUT-EXISTS");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Execute_Should_ExposeAnalyzerDiagnostics_InStructuredForm()
    {
        var root = Path.Combine(Path.GetTempPath(), "alloyed-integration-tests", Guid.NewGuid().ToString("N"));
        var scriptPath = Path.Combine(root, "input.ps1");
        var outputPath = Path.Combine(root, "out");

        Directory.CreateDirectory(root);
        File.WriteAllText(scriptPath, "Write-Host \"unterminated\nGet-ChildItem -Path .");

        try
        {
            var pipeline = PipelineBootstrap.CreateDefault();
            var result = pipeline.Execute(new PipelineRequest(scriptPath, "DiagnosticModule", outputPath, true));

            result.Success.Should().BeTrue();
            result.Diagnostics.Should().Contain(d => d.Code == "AST-UNTERMINATED-STRING" && d.Source == "ast-analyzer");
            result.Diagnostics.Should().OnlyContain(d => d.Line >= 0 && d.Column >= 0);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Execute_Should_FailOnWarningSeverity_WhenEnabled()
    {
        var root = Path.Combine(Path.GetTempPath(), "alloyed-integration-tests", Guid.NewGuid().ToString("N"));
        var scriptPath = Path.Combine(root, "input.ps1");
        var outputPath = Path.Combine(root, "out");

        Directory.CreateDirectory(root);
        File.WriteAllText(scriptPath, "Write-Host \"unterminated\nGet-ChildItem -Path .");

        try
        {
            var pipeline = PipelineBootstrap.CreateDefault();
            var result = pipeline.Execute(new PipelineRequest(
                scriptPath,
                "FailOnWarnModule",
                outputPath,
                Force: true,
                FailOnSeverity: PipelineDiagnosticSeverity.Warning));

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().ContainEquivalentOf("fail policy");
            result.Diagnostics.Should().Contain(d => d.Code == "AST-UNTERMINATED-STRING");
            result.Diagnostics.Should().ContainSingle(d => d.Code == "PIPELINE-FAIL-ON-SEVERITY");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Execute_Should_NotFailOnErrorSeverity_WhenOnlyWarningsPresent()
    {
        var root = Path.Combine(Path.GetTempPath(), "alloyed-integration-tests", Guid.NewGuid().ToString("N"));
        var scriptPath = Path.Combine(root, "input.ps1");
        var outputPath = Path.Combine(root, "out");

        Directory.CreateDirectory(root);
        File.WriteAllText(scriptPath, "Write-Host \"unterminated\nGet-ChildItem -Path .");

        try
        {
            var pipeline = PipelineBootstrap.CreateDefault();
            var result = pipeline.Execute(new PipelineRequest(
                scriptPath,
                "NoFailOnWarnAtErrorThreshold",
                outputPath,
                Force: true,
                FailOnSeverity: PipelineDiagnosticSeverity.Error));

            result.Success.Should().BeTrue();
            result.Diagnostics.Should().Contain(d => d.Code == "AST-UNTERMINATED-STRING");
            result.Diagnostics.Should().NotContain(d => d.Code == "PIPELINE-FAIL-ON-SEVERITY");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Execute_Should_MatchGoldenPipelineFixtureOutput()
    {
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Pipeline");
        var inputPath = Path.Combine(fixtureDir, "golden-input.ps1");
        var expectedPath = Path.Combine(fixtureDir, "golden-expected.psm1");

        var root = Path.Combine(Path.GetTempPath(), "alloyed-integration-tests", Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(root, "out");

        Directory.CreateDirectory(root);

        try
        {
            var pipeline = PipelineBootstrap.CreateDefault();
            var result = pipeline.Execute(new PipelineRequest(inputPath, "GoldenPipelineModule", outputPath, Force: true));

            result.Success.Should().BeTrue();
            result.CommandsFound.Should().BeGreaterThan(0);
            result.CommandsReplaced.Should().Be(3);
            result.MissingCommands.Should().BeEquivalentTo(new[] { "Unknown-Command" });

            var generatedPsm1 = Path.Combine(result.ModulePath, "GoldenPipelineModule.psm1");
            File.Exists(generatedPsm1).Should().BeTrue();

            var actual = NormalizeTextForGoldenComparison(File.ReadAllText(generatedPsm1));
            var expected = NormalizeTextForGoldenComparison(File.ReadAllText(expectedPath));

            actual.Should().Be(expected);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Execute_Should_MatchGoldenPipelineManifestFixtureOutput()
    {
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Pipeline");
        var inputPath = Path.Combine(fixtureDir, "golden-input.ps1");
        var expectedPath = Path.Combine(fixtureDir, "golden-expected.psd1");

        var root = Path.Combine(Path.GetTempPath(), "alloyed-integration-tests", Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(root, "out");

        Directory.CreateDirectory(root);

        try
        {
            var pipeline = PipelineBootstrap.CreateDefault();
            var result = pipeline.Execute(new PipelineRequest(inputPath, "GoldenPipelineModule", outputPath, Force: true));

            result.Success.Should().BeTrue();

            var generatedPsd1 = Path.Combine(result.ModulePath, "GoldenPipelineModule.psd1");
            File.Exists(generatedPsd1).Should().BeTrue();

            var actual = NormalizeManifestForGoldenComparison(File.ReadAllText(generatedPsd1));
            var expected = NormalizeManifestForGoldenComparison(File.ReadAllText(expectedPath));

            actual.Should().Be(expected);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static string NormalizeManifestForGoldenComparison(string manifest)
    {
        var normalized = NormalizeTextForGoldenComparison(manifest);
        normalized = Regex.Replace(normalized, "(GUID\\s*=\\s*')[^']+(')", "$1__GUID__$2", RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, "(Author\\s*=\\s*')[^']+(')", "$1__AUTHOR__$2", RegexOptions.CultureInvariant);
        return normalized;
    }

    private static string NormalizeTextForGoldenComparison(string text)
    {
        var normalized = text.Replace("\uFEFF", string.Empty, StringComparison.Ordinal);
        normalized = normalized.Replace("\r\n", "\n", StringComparison.Ordinal);
        normalized = Regex.Replace(normalized, "[ \\t]+\\n", "\n", RegexOptions.CultureInvariant);
        return normalized.TrimEnd();
    }
}
