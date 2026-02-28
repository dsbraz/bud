using Bud.Client.Shared.Goals;
using Bud.Shared.Contracts;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Bud.Client.Tests.Shared.Goals;

public sealed class CheckinModalTests : TestContext
{
    private static IndicatorResponse CreateQuantitativeIndicator() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Sales Target",
        Type = IndicatorType.Quantitative,
        QuantitativeType = QuantitativeIndicatorType.Achieve,
        MaxValue = 1000,
        Unit = IndicatorUnit.Integer
    };

    private static IndicatorResponse CreateQualitativeIndicator() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Team Culture",
        Type = IndicatorType.Qualitative,
        TargetText = "Improve morale"
    };

    [Fact]
    public void Render_WhenIsOpenFalse_ShouldNotRenderModal()
    {
        var cut = RenderComponent<CheckinModal>(parameters => parameters
            .Add(p => p.IsOpen, false)
            .Add(p => p.Indicator, CreateQuantitativeIndicator()));

        cut.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Render_WhenIsOpenTrueAndIndicatorNull_ShouldNotRenderModal()
    {
        var cut = RenderComponent<CheckinModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Indicator, null));

        cut.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Render_WhenQuantitativeIndicator_ShouldShowValueField()
    {
        var cut = RenderComponent<CheckinModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Indicator, CreateQuantitativeIndicator()));

        cut.Markup.Should().Contain("Valor atual");
        cut.Markup.Should().Contain("Sales Target");
    }

    [Fact]
    public void Render_WhenQualitativeIndicator_ShouldShowTextField()
    {
        var cut = RenderComponent<CheckinModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Indicator, CreateQualitativeIndicator()));

        cut.Markup.Should().Contain("Texto de progresso");
        cut.Markup.Should().Contain("Team Culture");
    }

    [Fact]
    public void Click_Submit_ShouldInvokeOnSubmitCallback()
    {
        CreateCheckinRequest? submitted = null;
        var indicator = CreateQuantitativeIndicator();

        var cut = RenderComponent<CheckinModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Indicator, indicator)
            .Add(p => p.OnSubmit, EventCallback.Factory.Create<CreateCheckinRequest>(this, r => submitted = r)));

        var submitButton = cut.Find("button.button.primary");
        submitButton.Click();

        submitted.Should().NotBeNull();
        submitted!.Should().NotBeNull();
    }
}
