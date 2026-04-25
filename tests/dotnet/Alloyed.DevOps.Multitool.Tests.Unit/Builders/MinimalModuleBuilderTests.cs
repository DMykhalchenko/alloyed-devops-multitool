namespace Alloyed.DevOps.Multitool.Tests.Unit.Builders;

using Alloyed.DevOps.Multitool.Core.Builders.Models;
using Alloyed.DevOps.Multitool.Core.Builders.Services;
using FluentAssertions;

public class MinimalModuleBuilderTests
{
    [Fact]
    public void Build_Should_CreateManifestAndModuleFiles()
    {
        var builder = new MinimalModuleBuilder();
        var outDir = Path.Combine(Path.GetTempPath(), "alloyed-builder-tests", Guid.NewGuid().ToString("N"));

        var result = builder.Build(new ModuleBuildRequest(
            ModuleName: "SampleModule",
            OutputPath: outDir,
            TransformedScript: "Get-AlloyedChildItem -Path .",
            RequiredModules: new[] { "Alloyed.DevOps.Multitool" },
            Author: "test",
            Description: "test module"));

        try
        {
            result.Success.Should().BeTrue();
            result.Files.Should().HaveCount(3);
            File.Exists(Path.Combine(result.ModulePath, "SampleModule.psm1")).Should().BeTrue();
            File.Exists(Path.Combine(result.ModulePath, "SampleModule.psd1")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, recursive: true);
            }
        }
    }
}
