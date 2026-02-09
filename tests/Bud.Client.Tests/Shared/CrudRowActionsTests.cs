using Bud.Client.Shared;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Bud.Client.Tests.Shared;

public sealed class CrudRowActionsTests : TestContext
{
    [Fact]
    public void Click_EditAndDelete_ShouldInvokeCallbacks()
    {
        var editCalled = false;
        var deleteCalled = false;

        var cut = RenderComponent<CrudRowActions>(parameters => parameters
            .Add(p => p.OnEdit, EventCallback.Factory.Create(this, () => editCalled = true))
            .Add(p => p.OnDelete, EventCallback.Factory.Create(this, () => deleteCalled = true))
            .Add(p => p.EditTitle, "Editar item")
            .Add(p => p.DeleteTitle, "Excluir item"));

        var buttons = cut.FindAll("button");
        buttons.Count.Should().Be(2);

        buttons[0].Click();
        buttons[1].Click();

        editCalled.Should().BeTrue();
        deleteCalled.Should().BeTrue();
    }

    [Fact]
    public void Render_WhenDeleteIsConfirming_ShouldShowConfirmText()
    {
        var cut = RenderComponent<CrudRowActions>(parameters => parameters
            .Add(p => p.IsDeleteConfirming, true)
            .Add(p => p.DeleteConfirmText, "Confirma agora?"));

        cut.Markup.Should().Contain("Confirma agora?");
    }
}
