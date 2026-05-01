namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Contracts;
using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// Structured Spectre.Console renderable for one console activity/event entry.
/// Renders a compact single-line row: icon, stage, operation, and optional elapsed time.
/// </summary>
internal sealed class ConsoleActivityRenderable : IRenderable
{
    private readonly IRenderable _inner;

    public ConsoleActivityRenderable(ConsoleActivityEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var isEnter = entry.Stage.Equals("Enter", StringComparison.OrdinalIgnoreCase);
        var isExit  = entry.Stage.Equals("Exit",  StringComparison.OrdinalIgnoreCase);
        var isError = entry.Stage.Equals("Error", StringComparison.OrdinalIgnoreCase);

        var (icon, iconStyle, stageStyle, operationStyle) = (isEnter, isExit, isError) switch
        {
            (true, _, _) => ("▶", "dim cyan1",  "cyan1",    "white"),
            (_, true, _) => ("✓", "green3",     "green3",   "white"),
            (_, _, true) => ("✗", "bold red",   "bold red", "bold red"),
            _             => ("·", "dim grey",   "grey",     "white"),
        };

        var elapsed = entry.ElapsedMilliseconds switch
        {
            > 0 and < 1000 => $"{entry.ElapsedMilliseconds}ms",
            >= 1000        => $"{entry.ElapsedMilliseconds / 1000.0:F1}s",
            _              => string.Empty,
        };

        var showMessage = !string.IsNullOrWhiteSpace(entry.Message)
            && !entry.Message.Equals("activity", StringComparison.OrdinalIgnoreCase);

        var operationCell = showMessage
            ? new Markup($"[{operationStyle}]{Escape(entry.Operation)}[/]  [dim]{Escape(entry.Message)}[/]")
            : new Markup($"[{operationStyle}]{Escape(entry.Operation)}[/]");

        var stagePadded = entry.Stage.PadRight(5);

        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn(string.Empty).NoWrap())
            .AddColumn(new TableColumn(string.Empty).NoWrap())
            .AddColumn(new TableColumn(string.Empty))
            .AddColumn(new TableColumn(string.Empty).NoWrap().RightAligned());

        table.AddRow(
            new Markup($"[{iconStyle}]{icon}[/]"),
            new Markup($"[{stageStyle}]{Escape(stagePadded)}[/]"),
            operationCell,
            new Markup(elapsed.Length > 0 ? $"[dim]{elapsed}[/]" : string.Empty));

        _inner = table;
    }

    public Measurement Measure(RenderOptions options, int maxWidth)
        => _inner.Measure(options, maxWidth);

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        => _inner.Render(options, maxWidth);

    private static string Escape(string value)
        => Markup.Escape(value);
}
