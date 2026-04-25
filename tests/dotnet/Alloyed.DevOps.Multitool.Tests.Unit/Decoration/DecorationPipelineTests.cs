namespace Alloyed.DevOps.Multitool.Tests.Unit.Decoration;

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
}
