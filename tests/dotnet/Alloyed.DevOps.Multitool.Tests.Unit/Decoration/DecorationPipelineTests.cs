namespace Alloyed.DevOps.Multitool.Tests.Unit.Decoration;

using Alloyed.DevOps.Multitool.Core.Decoration.Contracts;
using Alloyed.DevOps.Multitool.Core.Decoration.Decorators;
using Alloyed.DevOps.Multitool.Core.Decoration.Models;
using Alloyed.DevOps.Multitool.Core.Decoration.Services;
using FluentAssertions;

public class DecorationPipelineTests
{
    [Fact]
    public void Execute_Should_AssignCorrelationAndWrapExceptions()
    {
        var pipeline = new DecorationPipeline(new object[]
        {
            new ErrorHandlingDecorator(),
            new ObservabilityDecorator(),
            new CorrelationDecorator(),
        }.Cast<Alloyed.DevOps.Multitool.Core.Decoration.Contracts.IDecorator>());

        var context = new DecorationContext("unit-test");

        Action act = () => pipeline.Execute<int>(context, () => throw new InvalidOperationException("boom"));

        act.Should().Throw<DecorationExecutionException>()
            .Which.Operation.Should().Be("unit-test");
        context.GetTag(CorrelationDecorator.CorrelationIdTag).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Execute_Should_EmitTransparencyEvents_WhenEnabled_WithRedaction()
    {
        var sink = new RecordingSink();
        var pipeline = new DecorationPipeline(new IDecorator[]
        {
            new ErrorHandlingDecorator(),
            new ObservabilityDecorator(sink),
            new CorrelationDecorator(),
            new TransparencyDecorator(sink),
        });

        var context = new DecorationContext("watch-test", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [TransparencyDecorator.EnableTransparencyTag] = "true",
            ["apiToken"] = "super-secret-value",
            ["resource"] = "bucket-a",
        });

        var result = pipeline.Execute(context, () => 42);

        result.Should().Be(42);
        sink.Events.Should().Contain(e => e.Decorator == nameof(TransparencyDecorator) && e.Stage == DecorationStage.Enter);
        sink.Events.Should().Contain(e => e.Decorator == nameof(TransparencyDecorator) && e.Stage == DecorationStage.Exit);
        sink.Events.Should().Contain(e =>
            e.Decorator == nameof(TransparencyDecorator) &&
            e.Message != null &&
            e.Message.Contains("***REDACTED***", StringComparison.Ordinal) &&
            !e.Message.Contains("super-secret-value", StringComparison.Ordinal));
    }

    [Fact]
    public void Execute_Should_NotEmitTransparencyEvents_WhenDisabled()
    {
        var sink = new RecordingSink();
        var pipeline = new DecorationPipeline(new IDecorator[]
        {
            new ObservabilityDecorator(sink),
            new TransparencyDecorator(sink),
        });

        var context = new DecorationContext("watch-test-disabled", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [TransparencyDecorator.EnableTransparencyTag] = "false",
        });

        var result = pipeline.Execute(context, () => "ok");

        result.Should().Be("ok");
        sink.Events.Should().Contain(e => e.Decorator == nameof(ObservabilityDecorator));
        sink.Events.Should().NotContain(e => e.Decorator == nameof(TransparencyDecorator));
    }

    [Fact]
    public void Execute_Should_KeepDecoratorOrder_WithTransparencyBetweenObservabilityAndAction()
    {
        var sink = new RecordingSink();
        var pipeline = new DecorationPipeline(new IDecorator[]
        {
            new ErrorHandlingDecorator(),
            new CorrelationDecorator(),
            new ObservabilityDecorator(sink),
            new TransparencyDecorator(sink),
        });

        var order = new List<string>();
        var context = new DecorationContext("order-test", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [TransparencyDecorator.EnableTransparencyTag] = "true",
        });

        var _ = pipeline.Execute(context, () =>
        {
            order.Add("action");
            return 1;
        });

        var enterDecorators = sink.Events
            .Where(e => e.Stage == DecorationStage.Enter)
            .Select(e => e.Decorator)
            .ToList();

        enterDecorators.Should().ContainInOrder(
            nameof(ObservabilityDecorator),
            nameof(TransparencyDecorator));

        order.Should().ContainSingle().Which.Should().Be("action");
    }

    private sealed class RecordingSink : IDecorationSink
    {
        public IList<DecorationEvent> Events { get; } = new List<DecorationEvent>();

        public void Write(DecorationEvent @event)
        {
            Events.Add(@event);
        }
    }
}
