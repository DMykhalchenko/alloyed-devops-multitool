namespace Alloyed.DevOps.Multitool.Core.Ast.Models;

public sealed record CommandUsage(
    string CommandName,
    string? ModuleName,
    int Line,
    int Column,
    bool IsQualified
);
