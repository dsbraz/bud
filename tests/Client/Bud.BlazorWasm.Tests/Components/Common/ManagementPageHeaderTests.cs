using Bud.BlazorWasm.Components.Common;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Bud.BlazorWasm.Tests.Components.Common;

public sealed class ManagementPageHeaderTests : TestContext
{
    [Fact]
    public void Render_ShouldExposeDesignSystemHeaderStructure()
    {
        var cut = RenderComponent<ManagementPageHeader>(parameters => parameters
            .Add(p => p.Kicker, "Pessoas")
            .Add(p => p.Title, "Equipes")
            .Add(p => p.Subtitle, "Gerencie equipes e subequipes.")
            .Add(p => p.PrimaryActionText, "Nova equipe"));

        cut.Find("header.page-header.page-header-shell");
        cut.Find(".page-header-copy");
        cut.Find(".page-header-actions");
        cut.Markup.Should().Contain("Pessoas");
        cut.Markup.Should().Contain("Equipes");
        cut.Markup.Should().Contain("Gerencie equipes e subequipes.");
        cut.Markup.Should().Contain("Nova equipe");
    }

    [Fact]
    public void Click_PrimaryAction_ShouldInvokeCallback()
    {
        var invoked = false;

        var cut = RenderComponent<ManagementPageHeader>(parameters => parameters
            .Add(p => p.Title, "Organizações")
            .Add(p => p.OnPrimaryAction, EventCallback.Factory.Create(this, () => invoked = true)));

        cut.Find(".page-header-actions button").Click();

        invoked.Should().BeTrue();
    }
}
