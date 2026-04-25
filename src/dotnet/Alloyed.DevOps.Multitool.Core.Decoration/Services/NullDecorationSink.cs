namespace Alloyed.DevOps.Multitool.Core.Decoration.Services;

using Alloyed.DevOps.Multitool.Core.Decoration.Contracts;
using Alloyed.DevOps.Multitool.Core.Decoration.Models;

public sealed class NullDecorationSink : IDecorationSink
{
    public void Write(DecorationEvent entry)
    {
        _ = entry;
    }
}
