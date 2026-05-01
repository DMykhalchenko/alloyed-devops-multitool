namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Contracts;
using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// Structured Spectre.Console renderable for one console activity/event entry.
/// Keeps activity output stream-friendly while giving category, stage, metadata, and message
/// their own visual zones.
/// </summary>
internal sealed class ConsoleActivityRenderable : IRenderable
{
    private readonly IRenderable _inner;

    public ConsoleActivityRenderable(ConsoleActivityEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap());
        grid.AddColumn(new GridColumn().NoWrap());
        grid.AddColumn(new GridColumn().NoWrap());
        grid.AddColumn();

        var categoryStyle = entry.Level switch
        {
            ConsoleMessageLevel.Info => "black on cyan1",
            ConsoleMessageLevel.Warning => "black on yellow1",
            ConsoleMessageLevel.Error => "white on red",
            _ => "white on grey",
        };

        var stageStyle = entry.Stage.Equals("Error", StringComparison.OrdinalIgnoreCase)
            ? "bold red"
            : entry.Stage.Equals("Exit", StringComparison.OrdinalIgnoreCase)
                ? "green"
                : "blue";

        var correlationId = string.IsNullOrWhiteSpace(entry.CorrelationId) ? "-" : entry.CorrelationId;
        var meta = $"op={entry.Operation} corr={correlationId} elapsedMs={entry.ElapsedMilliseconds}";

        grid.AddRow(
            new Markup($"[{categoryStyle}] {Escape(entry.Category)} [/]"),
            new Markup($"[{stageStyle}]{Escape(entry.Stage)}[/]"),
            new Markup($"[grey]{Escape(meta)}[/]"),
            new Markup($"[white]{Escape(entry.Message)}[/]"));

        _inner = grid;
    }

    public Measurement Measure(RenderOptions options, int maxWidth)
    {
        return _inner.Measure(options, maxWidth);
    }

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        return _inner.Render(options, maxWidth);
    }

    private static string Escape(string value)
    {
        return Markup.Escape(value ?? string.Empty);
    }
}
