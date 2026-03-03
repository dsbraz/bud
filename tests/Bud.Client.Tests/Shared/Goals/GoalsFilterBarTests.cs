using Bud.Client.Shared.Goals;
using Bud.Shared.Contracts;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Bud.Client.Tests.Shared.Goals;

public sealed class GoalsFilterBarTests : TestContext
{
    [Fact]
    public void Render_ShouldShowThreeFilterChips()
    {
        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.Filter, GoalFilter.Mine)
            .Add(p => p.ViewMode, "list"));

        cut.Markup.Should().Contain("Minhas missões");
        cut.Markup.Should().Contain("Missões do time");
        cut.Markup.Should().Contain("Todas as missões");
    }

    [Fact]
    public void Click_AllMissions_ShouldInvokeOnSetFilterWithAll()
    {
        GoalFilter? receivedValue = null;

        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.Filter, GoalFilter.Mine)
            .Add(p => p.ViewMode, "list")
            .Add(p => p.OnSetFilter, EventCallback.Factory.Create<GoalFilter>(this, v => receivedValue = v)));

        var chips = cut.FindAll("button.filter-chip");
        var allChip = chips.First(c => c.TextContent.Contains("Todas"));
        allChip.Click();

        receivedValue.Should().Be(GoalFilter.All);
    }

    [Fact]
    public void Click_MyTeamMissions_ShouldInvokeOnSetFilterWithMyTeam()
    {
        GoalFilter? receivedValue = null;

        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.Filter, GoalFilter.Mine)
            .Add(p => p.ViewMode, "list")
            .Add(p => p.OnSetFilter, EventCallback.Factory.Create<GoalFilter>(this, v => receivedValue = v)));

        var chips = cut.FindAll("button.filter-chip");
        var teamChip = chips.First(c => c.TextContent.Contains("time"));
        teamChip.Click();

        receivedValue.Should().Be(GoalFilter.MyTeam);
    }

    [Fact]
    public void Click_ViewModeToggle_ShouldInvokeOnToggleViewMode()
    {
        var called = false;

        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.Filter, GoalFilter.Mine)
            .Add(p => p.ViewMode, "list")
            .Add(p => p.OnToggleViewMode, EventCallback.Factory.Create(this, () => called = true)));

        var chips = cut.FindAll("button.filter-chip");
        var viewModeChip = chips.First(c => c.TextContent.Contains("Lista") || c.TextContent.Contains("Grade"));
        viewModeChip.Click();

        called.Should().BeTrue();
    }

    [Fact]
    public void Render_WhenViewModeList_ShouldShowListLabel()
    {
        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.Filter, GoalFilter.All)
            .Add(p => p.ViewMode, "list"));

        var chips = cut.FindAll("button.filter-chip");
        var viewModeChip = chips.First(c => c.TextContent.Contains("Lista") || c.TextContent.Contains("Grade"));
        viewModeChip.TextContent.Should().Contain("Lista");
    }

    [Fact]
    public void Render_WhenViewModeGrid_ShouldShowGridLabel()
    {
        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.Filter, GoalFilter.All)
            .Add(p => p.ViewMode, "grid"));

        var chips = cut.FindAll("button.filter-chip");
        var viewModeChip = chips.First(c => c.TextContent.Contains("Lista") || c.TextContent.Contains("Grade"));
        viewModeChip.TextContent.Should().Contain("Grade");
    }
}
