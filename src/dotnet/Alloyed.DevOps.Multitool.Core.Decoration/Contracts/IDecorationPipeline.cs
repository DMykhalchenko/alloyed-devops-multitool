namespace Alloyed.DevOps.Multitool.Core.Decoration.Contracts;

using Alloyed.DevOps.Multitool.Core.Decoration.Models;

public interface IDecorationPipeline
{
    T Execute<T>(DecorationContext context, Func<T> action);
}
