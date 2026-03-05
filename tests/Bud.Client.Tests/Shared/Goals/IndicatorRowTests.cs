using Bud.Client.Shared.Goals;
using Bud.Shared.Contracts;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Bud.Client.Tests.Shared.Goals;

public sealed class IndicatorRowTests : TestContext
{
    private static GoalResponse CreateGoal() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Q1 Goal",
        StartDate = new DateTime(2025, 1, 1),
        EndDate = new DateTime(2025, 3, 31),
        Status = GoalStatus.Active
    };

    private static IndicatorResponse CreateIndicator(string name = "Revenue Growth") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Type = IndicatorType.Quantitative,
        QuantitativeType = QuantitativeIndicatorType.Achieve,
        MaxValue = 100,
        Unit = IndicatorUnit.Percentage
    };

    [Fact]
    public void Render_ShouldShowIndicatorName()
    {
        var cut = RenderComponent<IndicatorRow>(parameters => parameters
            .Add(p => p.Goal, CreateGoal())
            .Add(p => p.Indicator, CreateIndicator("NPS Score")));

        cut.Markup.Should().Contain("NPS Score");
    }

    [Fact]
    public void Render_ShouldShowGoalDateRange()
    {
        var cut = RenderComponent<IndicatorRow>(parameters => parameters
            .Add(p => p.Goal, CreateGoal())
            .Add(p => p.Indicator, CreateIndicator()));

        cut.Markup.Should().Contain("01/01/2025");
        cut.Markup.Should().Contain("31/03/2025");
    }

    [Fact]
    public void Click_CheckinButton_ShouldInvokeOnCheckinClick()
    {
        IndicatorResponse? received = null;
        var indicator = CreateIndicator();

        var cut = RenderComponent<IndicatorRow>(parameters => parameters
            .Add(p => p.Goal, CreateGoal())
            .Add(p => p.Indicator, indicator)
            .Add(p => p.OnCheckinClick, EventCallback.Factory.Create<IndicatorResponse>(this, m => received = m)));

        var checkinBtn = cut.Find("button.indicator-row-caret");
        checkinBtn.Click();

        received.Should().NotBeNull();
        received!.Id.Should().Be(indicator.Id);
    }

    [Fact]
    public void Click_Row_ShouldInvokeOnHistoryClick()
    {
        IndicatorResponse? received = null;
        var indicator = CreateIndicator();

        var cut = RenderComponent<IndicatorRow>(parameters => parameters
            .Add(p => p.Goal, CreateGoal())
            .Add(p => p.Indicator, indicator)
            .Add(p => p.OnHistoryClick, EventCallback.Factory.Create<IndicatorResponse>(this, m => received = m)));

        var row = cut.Find(".indicator-row");
        row.Click();

        received.Should().NotBeNull();
        received!.Id.Should().Be(indicator.Id);
    }
}
