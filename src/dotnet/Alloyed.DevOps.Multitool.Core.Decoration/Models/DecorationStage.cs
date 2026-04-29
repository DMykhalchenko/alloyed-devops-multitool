namespace Alloyed.DevOps.Multitool.Core.Decoration.Models;

/// <summary>
/// Identifies the lifecycle phase at which a <see cref="DecorationEvent"/> was emitted.
/// </summary>
public enum DecorationStage
{
    /// <summary>Emitted before the decorated operation begins.</summary>
    Enter = 0,

    /// <summary>Emitted after the decorated operation completes successfully.</summary>
    Exit = 1,

    /// <summary>Emitted when the decorated operation throws an exception.</summary>
    Error = 2,
}
