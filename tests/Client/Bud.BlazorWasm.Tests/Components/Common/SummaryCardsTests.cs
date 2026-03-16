using Bud.BlazorWasm.Components.Common;
using Bunit;
using FluentAssertions;
using Xunit;

namespace Bud.BlazorWasm.Tests.Components.Common;

public sealed class SummaryCardsTests : TestContext
{
    [Fact]
    public void Render_ShouldApplyDesignSystemSummaryGridClass()
    {
        var cut = RenderComponent<SummaryCards>(parameters => parameters
            .Add(p => p.CssClass, "goals-summary")
            .AddChildContent("<div>Card</div>"));

        cut.Find(".summary-cards.goals-summary");
        cut.Markup.Should().Contain("Card");
    }
}
