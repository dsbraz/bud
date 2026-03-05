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
    public void Render_ShouldShowMainFilterChips()
    {
        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.Filter, GoalFilter.Mine));

        cut.Markup.Should().Contain("Mais filtros");
        cut.Markup.Should().Contain("Minhas missões");
        cut.Markup.Should().Contain("Selecionar período");
    }

    [Fact]
    public void SelectScope_All_ShouldInvokeOnSetFilterWithAll()
    {
        GoalFilter? receivedValue = null;

        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.Filter, GoalFilter.Mine)
            .Add(p => p.OnSetFilter, EventCallback.Factory.Create<GoalFilter>(this, v => receivedValue = v)));

        var chips = cut.FindAll("button.filter-chip");
        var scopeChip = chips.First(c => c.TextContent.Contains("missões", StringComparison.OrdinalIgnoreCase));
        scopeChip.Click();

        var allOption = cut.FindAll("button.filter-dropdown-option")
            .First(c => c.TextContent.Contains("Todas as missões", StringComparison.OrdinalIgnoreCase));
        allOption.Click();

        receivedValue.Should().Be(GoalFilter.All);
    }

    [Fact]
    public void SelectScope_MyTeam_ShouldInvokeOnSetFilterWithMyTeam()
    {
        GoalFilter? receivedValue = null;

        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.Filter, GoalFilter.Mine)
            .Add(p => p.OnSetFilter, EventCallback.Factory.Create<GoalFilter>(this, v => receivedValue = v)));

        var chips = cut.FindAll("button.filter-chip");
        var scopeChip = chips.First(c => c.TextContent.Contains("missões", StringComparison.OrdinalIgnoreCase));
        scopeChip.Click();

        var teamOption = cut.FindAll("button.filter-dropdown-option")
            .First(c => c.TextContent.Contains("Missões do time", StringComparison.OrdinalIgnoreCase));
        teamOption.Click();

        receivedValue.Should().Be(GoalFilter.MyTeam);
    }

    [Fact]
    public void ToggleActiveFilter_ShouldInvokeOnToggleActiveFilter()
    {
        var called = false;

        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.Filter, GoalFilter.Mine)
            .Add(p => p.FilterActiveOnly, true)
            .Add(p => p.OnToggleActiveFilter, EventCallback.Factory.Create(this, () => called = true)));

        var chips = cut.FindAll("button.filter-chip");
        var statusChip = chips.First(c => c.TextContent.Contains("Mais filtros", StringComparison.OrdinalIgnoreCase));
        statusChip.Click();

        var statusOption = cut.Find("button.filter-dropdown-option");
        statusOption.Click();

        called.Should().BeTrue();
    }

    [Fact]
    public void Render_WithActiveFilters_ShouldShowClearFilterChip()
    {
        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.Filter, GoalFilter.Mine)
            .Add(p => p.Search, "okrs"));

        cut.Markup.Should().Contain("Limpar filtro");
    }

    [Fact]
    public void Click_ClearFilterChip_ShouldInvokeOnClearFilters()
    {
        var called = false;

        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.Filter, GoalFilter.Mine)
            .Add(p => p.Search, "okrs")
            .Add(p => p.OnClearFilters, EventCallback.Factory.Create(this, () => called = true)));

        var clearButton = cut.Find("button.filter-chip-clear");
        clearButton.Click();

        called.Should().BeTrue();
    }
}
