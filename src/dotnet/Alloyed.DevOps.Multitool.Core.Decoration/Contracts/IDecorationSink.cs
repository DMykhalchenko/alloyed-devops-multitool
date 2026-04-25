namespace Alloyed.DevOps.Multitool.Core.Decoration.Contracts;

using Alloyed.DevOps.Multitool.Core.Decoration.Models;

public interface IDecorationSink
{
    void Write(DecorationEvent entry);
}
