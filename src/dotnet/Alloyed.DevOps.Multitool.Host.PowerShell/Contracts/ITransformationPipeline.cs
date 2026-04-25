namespace Alloyed.DevOps.Multitool.Host.PowerShell.Contracts;

using Alloyed.DevOps.Multitool.Host.PowerShell.Models;

public interface ITransformationPipeline
{
    PipelineResult Execute(PipelineRequest request);
}
