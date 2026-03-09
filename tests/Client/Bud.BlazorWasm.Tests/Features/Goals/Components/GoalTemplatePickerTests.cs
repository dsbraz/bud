using Bud.BlazorWasm.Features.Goals.Components;
using Bud.Shared.Contracts;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Bud.BlazorWasm.Tests.Features.Goals.Components;

public sealed class GoalTemplatePickerTests : TestContext
{
    [Fact]
    public void Render_WhenIsOpenFalse_ShouldNotRenderModal()
    {
        var cut = RenderComponent<GoalTemplatePicker>(parameters => parameters
            .Add(p => p.IsOpen, false)
            .Add(p => p.Templates, []));

        cut.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Render_WhenIsOpenTrue_ShouldRenderModalWithTitle()
    {
        var cut = RenderComponent<GoalTemplatePicker>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Templates, []));

        cut.Markup.Should().Contain("Como deseja criar sua meta");
    }

    [Fact]
    public void Render_WhenTemplatesProvided_ShouldShowTemplateNames()
    {
        var templates = new List<TemplateResponse>
        {
            new() { Id = Guid.NewGuid(), Name = "Template Alpha", Description = "Desc A", Indicators = new List<TemplateIndicatorResponse> { new() } },
            new() { Id = Guid.NewGuid(), Name = "Template Beta", Indicators = new List<TemplateIndicatorResponse>() }
        };

        var cut = RenderComponent<GoalTemplatePicker>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Templates, templates));

        cut.Markup.Should().Contain("Template Alpha");
        cut.Markup.Should().Contain("Template Beta");
        cut.Markup.Should().Contain("Desc A");
        cut.Markup.Should().Contain("Sem descri");
    }

    [Fact]
    public void Click_CreateFromScratch_ShouldInvokeCallback()
    {
        var called = false;

        var cut = RenderComponent<GoalTemplatePicker>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Templates, [])
            .Add(p => p.OnCreateFromScratch, EventCallback.Factory.Create(this, () => called = true)));

        var scratchCard = cut.Find(".template-picker-card");
        scratchCard.Click();

        called.Should().BeTrue();
    }

    [Fact]
    public void Click_Template_ShouldInvokeOnSelectTemplateWithTemplate()
    {
        TemplateResponse? selected = null;
        var template = new TemplateResponse
        {
            Id = Guid.NewGuid(),
            Name = "My Template",
            Indicators = new List<TemplateIndicatorResponse>()
        };

        var cut = RenderComponent<GoalTemplatePicker>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Templates, [template])
            .Add(p => p.OnSelectTemplate, EventCallback.Factory.Create<TemplateResponse>(this, t => selected = t)));

        // First card is "from scratch", second is the template
        var cards = cut.FindAll(".template-picker-card");
        cards[1].Click();

        selected.Should().NotBeNull();
        selected!.Name.Should().Be("My Template");
    }
}
