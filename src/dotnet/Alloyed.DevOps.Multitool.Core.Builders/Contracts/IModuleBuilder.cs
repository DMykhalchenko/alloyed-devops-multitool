namespace Alloyed.DevOps.Multitool.Core.Builders.Contracts;

using Alloyed.DevOps.Multitool.Core.Builders.Models;

public interface IModuleBuilder
{
    ModuleBuildResult Build(ModuleBuildRequest request);
}
