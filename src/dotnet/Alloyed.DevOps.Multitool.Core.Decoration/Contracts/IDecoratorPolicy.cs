namespace Alloyed.DevOps.Multitool.Core.Decoration.Contracts;

using Alloyed.DevOps.Multitool.Core.Decoration.Models;

public interface IDecoratorPolicy
{
    int Priority { get; }

    string Name { get; }

    bool Enabled(DecorationContext context);
}
