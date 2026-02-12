using Bud.Client.Pages;
using Bunit;
using FluentAssertions;
using Xunit;

namespace Bud.Client.Tests.Pages;

public sealed class NotFoundPageTests : TestContext
{
    [Fact]
    public void Render_ShouldShowNotFoundText()
    {
        var cut = RenderComponent<NotFound>();

        cut.Markup.Should().Contain("Página não encontrada");
        cut.Markup.Should().Contain("Voltar para o início");
    }
}
