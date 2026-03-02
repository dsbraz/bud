using Bud.Client.Shared.Goals;
using Bud.Shared.Contracts;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Bud.Client.Tests.Shared.Goals;

public sealed class CheckinHistoryModalTests : TestContext
{
    private static IndicatorResponse CreateQuantitativeIndicator(string name = "Revenue") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Type = IndicatorType.Quantitative,
        QuantitativeType = QuantitativeIndicatorType.Achieve,
        MaxValue = 100,
        Unit = IndicatorUnit.Percentage
    };

    [Fact]
    public void Render_WhenIsOpenFalse_ShouldNotRenderModal()
    {
        var cut = RenderComponent<CheckinHistoryModal>(parameters => parameters
            .Add(p => p.IsOpen, false)
            .Add(p => p.Indicator, CreateQuantitativeIndicator())
            .Add(p => p.Checkins, []));

        cut.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Render_WhenIsOpenTrueAndIndicatorNull_ShouldNotRenderModal()
    {
        var cut = RenderComponent<CheckinHistoryModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Indicator, null)
            .Add(p => p.Checkins, []));

        cut.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Render_WhenIsLoading_ShouldShowLoadingMessage()
    {
        var cut = RenderComponent<CheckinHistoryModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Indicator, CreateQuantitativeIndicator())
            .Add(p => p.IsLoading, true)
            .Add(p => p.Checkins, []));

        cut.Markup.Should().Contain("Carregando check-ins...");
    }

    [Fact]
    public void Render_WhenNoCheckins_ShouldShowEmptyState()
    {
        var cut = RenderComponent<CheckinHistoryModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Indicator, CreateQuantitativeIndicator())
            .Add(p => p.IsLoading, false)
            .Add(p => p.Checkins, []));

        cut.Markup.Should().Contain("Nenhum check-in registrado");
    }

    [Fact]
    public void Render_WhenCheckinsExist_ShouldShowTimelineItems()
    {
        var checkins = new List<CheckinResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Value = 42,
                CheckinDate = new DateTime(2025, 6, 15),
                ConfidenceLevel = 4,
                Note = "Good progress",
                Collaborator = new CollaboratorResponse { FullName = "Ana Silva" }
            }
        };

        var cut = RenderComponent<CheckinHistoryModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Indicator, CreateQuantitativeIndicator())
            .Add(p => p.IsLoading, false)
            .Add(p => p.Checkins, checkins));

        cut.Markup.Should().Contain("Ana Silva");
        cut.Markup.Should().Contain("15/06/2025");
        cut.Markup.Should().Contain("Good progress");
    }

    [Fact]
    public void Click_NewCheckin_ShouldInvokeCallback()
    {
        var called = false;

        var cut = RenderComponent<CheckinHistoryModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Indicator, CreateQuantitativeIndicator())
            .Add(p => p.Checkins, [])
            .Add(p => p.OnNewCheckin, EventCallback.Factory.Create(this, () => called = true)));

        var newCheckinButton = cut.Find("button.button.primary");
        newCheckinButton.Click();

        called.Should().BeTrue();
    }
}
