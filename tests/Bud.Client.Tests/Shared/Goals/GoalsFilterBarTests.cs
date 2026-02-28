using Bud.Client.Shared.Goals;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Bud.Client.Tests.Shared.Goals;

public sealed class GoalsFilterBarTests : TestContext
{
    [Fact]
    public void Render_ShouldShowMyMetasAndAllMetasChips()
    {
        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.ShowMyGoals, true)
            .Add(p => p.ViewMode, "list"));

        cut.Markup.Should().Contain("Minhas meta");
        cut.Markup.Should().Contain("Todas as meta");
    }

    [Fact]
    public void Click_AllMissions_ShouldInvokeOnSetMyGoalsWithFalse()
    {
        bool? receivedValue = null;

        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.ShowMyGoals, true)
            .Add(p => p.ViewMode, "list")
            .Add(p => p.OnSetMyGoals, EventCallback.Factory.Create<bool>(this, v => receivedValue = v)));

        // "Todas as metas" is the second filter-chip
        var chips = cut.FindAll("button.filter-chip");
        var allMetasChip = chips.First(c => c.TextContent.Contains("Todas"));
        allMetasChip.Click();

        receivedValue.Should().BeFalse();
    }

    [Fact]
    public void Click_ViewModeToggle_ShouldInvokeOnToggleViewMode()
    {
        var called = false;

        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.ShowMyGoals, true)
            .Add(p => p.ViewMode, "list")
            .Add(p => p.OnToggleViewMode, EventCallback.Factory.Create(this, () => called = true)));

        // View mode toggle is the last filter-chip
        var chips = cut.FindAll("button.filter-chip");
        var viewModeChip = chips.First(c => c.TextContent.Contains("Lista") || c.TextContent.Contains("Grade"));
        viewModeChip.Click();

        called.Should().BeTrue();
    }

    [Fact]
    public void Render_WhenViewModeList_ShouldShowListLabel()
    {
        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.ShowMyGoals, false)
            .Add(p => p.ViewMode, "list"));

        var chips = cut.FindAll("button.filter-chip");
        var viewModeChip = chips.First(c => c.TextContent.Contains("Lista") || c.TextContent.Contains("Grade"));
        viewModeChip.TextContent.Should().Contain("Lista");
    }

    [Fact]
    public void Render_WhenViewModeGrid_ShouldShowGridLabel()
    {
        var cut = RenderComponent<GoalsFilterBar>(parameters => parameters
            .Add(p => p.ShowMyGoals, false)
            .Add(p => p.ViewMode, "grid"));

        var chips = cut.FindAll("button.filter-chip");
        var viewModeChip = chips.First(c => c.TextContent.Contains("Lista") || c.TextContent.Contains("Grade"));
        viewModeChip.TextContent.Should().Contain("Grade");
    }
}
