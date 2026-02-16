using Bud.Client.Shared.Missions;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Bud.Client.Tests.Shared.Missions;

public sealed class MissionMetricRowTests : TestContext
{
    private static Mission CreateMission() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Q1 Mission",
        StartDate = new DateTime(2025, 1, 1),
        EndDate = new DateTime(2025, 3, 31),
        Status = MissionStatus.Active
    };

    private static MissionMetric CreateMetric(string name = "Revenue Growth") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Type = MetricType.Quantitative,
        QuantitativeType = QuantitativeMetricType.Achieve,
        MaxValue = 100,
        Unit = MetricUnit.Percentage
    };

    [Fact]
    public void Render_ShouldShowMetricName()
    {
        var cut = RenderComponent<MissionMetricRow>(parameters => parameters
            .Add(p => p.Mission, CreateMission())
            .Add(p => p.Metric, CreateMetric("NPS Score")));

        cut.Markup.Should().Contain("NPS Score");
    }

    [Fact]
    public void Render_ShouldShowMissionDateRange()
    {
        var cut = RenderComponent<MissionMetricRow>(parameters => parameters
            .Add(p => p.Mission, CreateMission())
            .Add(p => p.Metric, CreateMetric()));

        cut.Markup.Should().Contain("01/01/2025");
        cut.Markup.Should().Contain("31/03/2025");
    }

    [Fact]
    public void Click_CheckinButton_ShouldInvokeOnCheckinClick()
    {
        MissionMetric? received = null;
        var metric = CreateMetric();

        var cut = RenderComponent<MissionMetricRow>(parameters => parameters
            .Add(p => p.Mission, CreateMission())
            .Add(p => p.Metric, metric)
            .Add(p => p.OnCheckinClick, EventCallback.Factory.Create<MissionMetric>(this, m => received = m)));

        var checkinBtn = cut.Find("button.metric-checkin-btn");
        checkinBtn.Click();

        received.Should().NotBeNull();
        received!.Id.Should().Be(metric.Id);
    }

    [Fact]
    public void Click_Row_ShouldInvokeOnHistoryClick()
    {
        MissionMetric? received = null;
        var metric = CreateMetric();

        var cut = RenderComponent<MissionMetricRow>(parameters => parameters
            .Add(p => p.Mission, CreateMission())
            .Add(p => p.Metric, metric)
            .Add(p => p.OnHistoryClick, EventCallback.Factory.Create<MissionMetric>(this, m => received = m)));

        var row = cut.Find(".metric-row");
        row.Click();

        received.Should().NotBeNull();
        received!.Id.Should().Be(metric.Id);
    }
}
