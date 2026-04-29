namespace Alloyed.DevOps.Multitool.Core.Ast.Models;

/// <summary>
/// Describes a single command invocation detected inside a PowerShell script.
/// </summary>
/// <param name="CommandName">The bare command name, without any module qualifier (e.g. <c>Get-Item</c>).</param>
/// <param name="ModuleName">
/// The module qualifier extracted from a <c>Module\Command</c> notation, or <see langword="null"/>
/// when the command is called without an explicit module prefix.
/// </param>
/// <param name="Line">One-based line number of the command invocation in the source script.</param>
/// <param name="Column">One-based column number of the command invocation in the source script.</param>
/// <param name="IsQualified">
/// <see langword="true"/> when the command was written as <c>Module\Command</c>;
/// <see langword="false"/> when no module qualifier was present.
/// </param>
public sealed record CommandUsage(
    string CommandName,
    string? ModuleName,
    int Line,
    int Column,
    bool IsQualified
);
