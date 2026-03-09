using Bud.BlazorWasm.Features.Goals.Components;
using Bunit;
using FluentAssertions;
using Xunit;

namespace Bud.BlazorWasm.Tests.Features.Goals.Components;

public sealed class IndicatorFormModalTests : TestContext
{
    [Fact]
    public void Render_WhenClosed_ShouldRenderNothing()
    {
        var cut = RenderComponent<IndicatorFormModal>(parameters => parameters
            .Add(p => p.IsOpen, false));

        cut.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Render_WhenOpenForCreate_ShouldShowNewTitle()
    {
        var cut = RenderComponent<IndicatorFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true));

        cut.Markup.Should().Contain("Novo indicador");
    }

    [Fact]
    public void Render_WhenOpenForEdit_ShouldShowEditTitle()
    {
        var initial = new TempIndicator(Guid.NewGuid(), "Existing", "Qualitative", "desc", TargetText: "x");

        var cut = RenderComponent<IndicatorFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsEditMode, true)
            .Add(p => p.InitialModel, initial));

        cut.Markup.Should().Contain("Editar indicador");
    }

    [Fact]
    public void Render_WhenOpenWithInitialModel_ShouldPopulateFields()
    {
        var initial = new TempIndicator(null, "Revenue Growth", "Qualitative", "desc", TargetText: "Grow revenue");

        var cut = RenderComponent<IndicatorFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.InitialModel, initial));

        var instance = cut.Instance;
        var model = GetField<IndicatorFormFields.IndicatorFormModel>(instance, "_model");
        model.Name.Should().Be("Revenue Growth");
        model.TargetText.Should().Be("Grow revenue");
    }

    private static T GetField<T>(object instance, string name)
        => (T)instance.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(instance)!;
}
