namespace Alloyed.DevOps.Multitool.Tests.Integration.Pipeline;

using Alloyed.DevOps.Multitool.Host.PowerShell.Models;
using Alloyed.DevOps.Multitool.Host.PowerShell.Services;
using FluentAssertions;

public class RuntimeConfigurationIntegrationTests
{
    [Fact]
    public void CreateRuntimeConfiguration_Should_RespectPrecedence_EnvOverYamlOverJsonOverDefaults()
    {
        var root = Path.Combine(Path.GetTempPath(), "alloyed-config-tests", Guid.NewGuid().ToString("N"));
        var configDir = Path.Combine(root, "config");
        Directory.CreateDirectory(configDir);

        File.WriteAllText(Path.Combine(configDir, "appsettings.json"), """
            {
              "Alloyed": {
                "Runtime": { "FailOnSeverity": "Error" },
                "Session": { "Enabled": false },
                "Mocking": { "Mode": "Moq" },
                "Decoration": { "TransparencyProfile": "Minimal" }
              }
            }
            """);

        File.WriteAllText(Path.Combine(configDir, "appsettings.yml"), """
            Alloyed:
              Runtime:
                FailOnSeverity: Warning
              Session:
                Enabled: true
              Decoration:
                TransparencyProfile: Debug
              Mocking:
                Mode: Custom
            """);

        var env = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ALLOYED__RUNTIME__FAILONSEVERITY"] = "Info",
            ["ALLOYED__RUNTIME__DEFAULTOUTPUTPATH"] = "generated-modules",
            ["ALLOYED__MOCKING__ENABLED"] = "true",
            ["ALLOYED__CATALOG__SOURCEPATH"] = "tools/ports/ports.catalog.json",
            ["ALLOYED__DECORATION__TRANSPARENCYPROFILE"] = "Standard",
        };

        try
        {
            var configuration = PipelineBootstrap.CreateRuntimeConfiguration(root, env);

            configuration.Runtime.FailOnSeverity.Should().Be(PipelineDiagnosticSeverity.Info);
            configuration.Runtime.DefaultOutputPath.Should().Be("generated-modules");
            configuration.Session.Enabled.Should().BeTrue();
            configuration.Decoration.EnableErrorHandling.Should().BeTrue(); // default
            configuration.Decoration.TransparencyProfile.Should().Be(TransparencyProfile.Standard);
            configuration.Mocking.Mode.Should().Be(MockingMode.Custom);
            configuration.Mocking.Enabled.Should().BeTrue();
            configuration.Catalog.SourcePath.Should().Be("tools/ports/ports.catalog.json");
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
    public void CreateRuntimeConfiguration_Should_ThrowActionableError_WhenMockingModeIsInvalid()
    {
        var root = Path.Combine(Path.GetTempPath(), "alloyed-config-tests", Guid.NewGuid().ToString("N"));
        var configDir = Path.Combine(root, "config");
        Directory.CreateDirectory(configDir);

        File.WriteAllText(Path.Combine(configDir, "appsettings.yml"), """
            Alloyed:
              Mocking:
                Mode: TotallyInvalid
            """);

        try
        {
            var act = () => PipelineBootstrap.CreateRuntimeConfiguration(root);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Alloyed:Mocking:Mode*");
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
    public void CreateRuntimeConfiguration_Should_AllowTafCompatibilityEnvKeys()
    {
        var env = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["TAF__MOCKING__ENABLED"] = "true",
            ["TAF__MOCKING__MODE"] = "Moq",
            ["TAF__DECORATION__ENABLETRANSPARENCY"] = "true",
        };

        var configuration = PipelineBootstrap.CreateRuntimeConfiguration(environment: env);

        configuration.Mocking.Enabled.Should().BeTrue();
        configuration.Mocking.Mode.Should().Be(MockingMode.Moq);
        configuration.Decoration.EnableTransparency.Should().BeTrue();
        configuration.Runtime.DefaultOutputPath.Should().Be("out");
        configuration.Catalog.SourcePath.Should().BeNull();
    }

    [Fact]
    public void Execute_Should_UseConfiguredFailOnSeverity_WhenRequestHasNoOverride()
    {
        var root = Path.Combine(Path.GetTempPath(), "alloyed-config-tests", Guid.NewGuid().ToString("N"));
        var scriptPath = Path.Combine(root, "input.ps1");
        var outputPath = Path.Combine(root, "out");
        Directory.CreateDirectory(root);

        File.WriteAllText(scriptPath, "Write-Host \"unterminated\nGet-ChildItem -Path .");

        var env = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ALLOYED__RUNTIME__FAILONSEVERITY"] = "Warning",
        };

        try
        {
            var pipeline = PipelineBootstrap.CreateDefault(environment: env);
            var result = pipeline.Execute(new PipelineRequest(scriptPath, "ConfiguredFailPolicy", outputPath, Force: true));

            result.Success.Should().BeFalse();
            result.Diagnostics.Should().Contain(d => d.Code == "PIPELINE-FAIL-ON-SEVERITY");
            result.ErrorMessage.Should().ContainEquivalentOf("fail policy");
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
    public void CreateDefault_Should_ThrowActionableError_WhenConfiguredCatalogPathDoesNotExist()
    {
        var root = Path.Combine(Path.GetTempPath(), "alloyed-config-tests", Guid.NewGuid().ToString("N"));
        var configDir = Path.Combine(root, "config");
        Directory.CreateDirectory(configDir);

        File.WriteAllText(Path.Combine(configDir, "appsettings.yml"), """
            Alloyed:
              Catalog:
                SourcePath: ./missing/catalog.json
            """);

        try
        {
            var act = () => PipelineBootstrap.CreateDefault(root);

            act.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*Ports catalog file was not found*");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
