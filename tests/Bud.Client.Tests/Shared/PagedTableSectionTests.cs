using Bud.Client.Shared;
using Bunit;
using FluentAssertions;
using Xunit;

namespace Bud.Client.Tests.Shared;

public sealed class PagedTableSectionTests : TestContext
{
    [Fact]
    public void Render_WhenLoading_ShouldShowLoadingTextOnly()
    {
        var cut = RenderComponent<PagedTableSection>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.LoadingText, "Carregando dados..."));

        cut.Markup.Should().Contain("Carregando dados...");
        cut.Markup.Should().NotContain("Total:");
    }

    [Fact]
    public void Render_WhenEmpty_ShouldShowEmptyTextOnly()
    {
        var cut = RenderComponent<PagedTableSection>(parameters => parameters
            .Add(p => p.IsEmpty, true)
            .Add(p => p.EmptyText, "Sem registros."));

        cut.Markup.Should().Contain("Sem registros.");
        cut.Markup.Should().NotContain("Total:");
    }

    [Fact]
    public void Render_WhenHasData_ShouldShowContentAndTotal()
    {
        var cut = RenderComponent<PagedTableSection>(parameters => parameters
            .Add(p => p.Total, 12)
            .AddChildContent("<table><tbody><tr><td>Linha</td></tr></tbody></table>"));

        cut.Markup.Should().Contain("Linha");
        cut.Markup.Should().Contain("Total: 12");
    }
}
