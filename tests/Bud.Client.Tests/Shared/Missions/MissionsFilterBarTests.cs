using Bud.Client.Shared.Missions;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Bud.Client.Tests.Shared.Missions;

public sealed class MissionsFilterBarTests : TestContext
{
    [Fact]
    public void Render_ShouldShowMyMissionsAndAllMissionsChips()
    {
        var cut = RenderComponent<MissionsFilterBar>(parameters => parameters
            .Add(p => p.ShowMyMissions, true)
            .Add(p => p.ViewMode, "list"));

        cut.Markup.Should().Contain("Minhas miss");
        cut.Markup.Should().Contain("Todas as miss");
    }

    [Fact]
    public void Click_AllMissions_ShouldInvokeOnSetMyMissionsWithFalse()
    {
        bool? receivedValue = null;

        var cut = RenderComponent<MissionsFilterBar>(parameters => parameters
            .Add(p => p.ShowMyMissions, true)
            .Add(p => p.ViewMode, "list")
            .Add(p => p.OnSetMyMissions, EventCallback.Factory.Create<bool>(this, v => receivedValue = v)));

        // "Todas as missoes" is the second filter-chip
        var chips = cut.FindAll("button.filter-chip");
        var allMissionsChip = chips.First(c => c.TextContent.Contains("Todas"));
        allMissionsChip.Click();

        receivedValue.Should().BeFalse();
    }

    [Fact]
    public void Click_ViewModeToggle_ShouldInvokeOnToggleViewMode()
    {
        var called = false;

        var cut = RenderComponent<MissionsFilterBar>(parameters => parameters
            .Add(p => p.ShowMyMissions, true)
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
        var cut = RenderComponent<MissionsFilterBar>(parameters => parameters
            .Add(p => p.ShowMyMissions, false)
            .Add(p => p.ViewMode, "list"));

        var chips = cut.FindAll("button.filter-chip");
        var viewModeChip = chips.First(c => c.TextContent.Contains("Lista") || c.TextContent.Contains("Grade"));
        viewModeChip.TextContent.Should().Contain("Lista");
    }

    [Fact]
    public void Render_WhenViewModeGrid_ShouldShowGridLabel()
    {
        var cut = RenderComponent<MissionsFilterBar>(parameters => parameters
            .Add(p => p.ShowMyMissions, false)
            .Add(p => p.ViewMode, "grid"));

        var chips = cut.FindAll("button.filter-chip");
        var viewModeChip = chips.First(c => c.TextContent.Contains("Lista") || c.TextContent.Contains("Grade"));
        viewModeChip.TextContent.Should().Contain("Grade");
    }
}
