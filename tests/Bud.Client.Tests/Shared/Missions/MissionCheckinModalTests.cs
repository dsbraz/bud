using Bud.Client.Shared.Missions;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Bud.Client.Tests.Shared.Missions;

public sealed class MissionCheckinModalTests : TestContext
{
    private static MissionMetric CreateQuantitativeMetric() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Sales Target",
        Type = MetricType.Quantitative,
        QuantitativeType = QuantitativeMetricType.Achieve,
        MaxValue = 1000,
        Unit = MetricUnit.Integer
    };

    private static MissionMetric CreateQualitativeMetric() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Team Culture",
        Type = MetricType.Qualitative,
        TargetText = "Improve morale"
    };

    [Fact]
    public void Render_WhenIsOpenFalse_ShouldNotRenderModal()
    {
        var cut = RenderComponent<MissionCheckinModal>(parameters => parameters
            .Add(p => p.IsOpen, false)
            .Add(p => p.Metric, CreateQuantitativeMetric()));

        cut.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Render_WhenIsOpenTrueAndMetricNull_ShouldNotRenderModal()
    {
        var cut = RenderComponent<MissionCheckinModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Metric, null));

        cut.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Render_WhenQuantitativeMetric_ShouldShowValueField()
    {
        var cut = RenderComponent<MissionCheckinModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Metric, CreateQuantitativeMetric()));

        cut.Markup.Should().Contain("Valor atual");
        cut.Markup.Should().Contain("Sales Target");
    }

    [Fact]
    public void Render_WhenQualitativeMetric_ShouldShowTextField()
    {
        var cut = RenderComponent<MissionCheckinModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Metric, CreateQualitativeMetric()));

        cut.Markup.Should().Contain("Texto de progresso");
        cut.Markup.Should().Contain("Team Culture");
    }

    [Fact]
    public void Click_Submit_ShouldInvokeOnSubmitCallback()
    {
        CreateMetricCheckinRequest? submitted = null;
        var metric = CreateQuantitativeMetric();

        var cut = RenderComponent<MissionCheckinModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Metric, metric)
            .Add(p => p.OnSubmit, EventCallback.Factory.Create<CreateMetricCheckinRequest>(this, r => submitted = r)));

        var submitButton = cut.Find("button.button.primary");
        submitButton.Click();

        submitted.Should().NotBeNull();
        submitted!.MissionMetricId.Should().Be(metric.Id);
    }
}
