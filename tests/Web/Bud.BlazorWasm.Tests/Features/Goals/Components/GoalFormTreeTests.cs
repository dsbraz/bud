using Bud.BlazorWasm.Features.Goals.Components;
using Bunit;
using FluentAssertions;
using Xunit;

namespace Bud.BlazorWasm.Tests.Features.Goals.Components;

public sealed class GoalFormTreeTests : TestContext
{
    [Fact]
    public void Render_WithNoItems_ShouldRenderNothing()
    {
        var cut = RenderComponent<GoalFormTree>(parameters => parameters
            .Add(p => p.Indicators, [])
            .Add(p => p.Children, []));

        cut.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Render_WithIndicators_ShouldShowIndicatorNodes()
    {
        var indicators = new List<TempIndicator>
        {
            new(null, "Revenue Growth", "Quantitative", "Atingir 100%"),
            new(null, "NPS", "Qualitative", "Acima de 80")
        };

        var cut = RenderComponent<GoalFormTree>(parameters => parameters
            .Add(p => p.Indicators, indicators)
            .Add(p => p.Children, []));

        cut.Markup.Should().Contain("Revenue Growth");
        cut.Markup.Should().Contain("NPS");
    }

    [Fact]
    public void Render_WithChildren_ShouldNotShowNestedIndicators()
    {
        var children = new List<TempGoal>
        {
            new("g-1", "Engajamento", "Aumentar engajamento")
            {
                Indicators = [new TempIndicator(null, "NPS Score", "Quantitative", "Acima de 80")]
            }
        };

        var cut = RenderComponent<GoalFormTree>(parameters => parameters
            .Add(p => p.Indicators, [])
            .Add(p => p.Children, children));

        cut.Markup.Should().Contain("Engajamento");
        cut.Markup.Should().NotContain("NPS Score");
    }

    [Fact]
    public void Render_ChildWithBadge_ShouldShowItemCount()
    {
        var children = new List<TempGoal>
        {
            new("g-1", "Meta", null)
            {
                Indicators =
                [
                    new TempIndicator(null, "Ind1", "Qualitative", "d"),
                    new TempIndicator(null, "Ind2", "Qualitative", "d")
                ]
            }
        };

        var cut = RenderComponent<GoalFormTree>(parameters => parameters
            .Add(p => p.Indicators, [])
            .Add(p => p.Children, children));

        cut.Find(".goal-form-tree-badge").TextContent.Should().Be("2");
    }

    [Fact]
    public void Render_ChildDimension_ShouldShowAsDetails()
    {
        var children = new List<TempGoal>
        {
            new("g-1", "Meta", null, Dimension: "Financeiro")
        };

        var cut = RenderComponent<GoalFormTree>(parameters => parameters
            .Add(p => p.Indicators, [])
            .Add(p => p.Children, children));

        cut.Markup.Should().Contain("Financeiro");
    }

    [Fact]
    public void Render_ChildGoal_ShouldShowEditButton()
    {
        var children = new List<TempGoal>
        {
            new("g-1", "Meta editável", null)
        };

        int? editedIndex = null;
        var cut = RenderComponent<GoalFormTree>(parameters => parameters
            .Add(p => p.Indicators, [])
            .Add(p => p.Children, children)
            .Add(p => p.OnEditGoal, idx => editedIndex = idx));

        var editButtons = cut.FindAll(".goal-form-tree-action-btn:not(.delete)");
        editButtons.Should().NotBeEmpty();
    }

    [Fact]
    public void Render_ClickEditButton_ShouldInvokeOnEditGoal()
    {
        var children = new List<TempGoal>
        {
            new("g-1", "Meta editável", null)
        };

        int? editedIndex = null;
        var cut = RenderComponent<GoalFormTree>(parameters => parameters
            .Add(p => p.Indicators, [])
            .Add(p => p.Children, children)
            .Add(p => p.OnEditGoal, idx => editedIndex = idx));

        var goalNode = cut.Find(".goal-form-tree-goal .goal-form-tree-actions");
        var editButton = goalNode.QuerySelector("[title='Editar meta']");
        editButton.Should().NotBeNull();
        editButton!.Click();

        editedIndex.Should().Be(0);
    }

    [Fact]
    public void Render_DeleteButton_ShouldHaveExcluirMetaTitle()
    {
        var children = new List<TempGoal>
        {
            new("g-1", "Meta", null)
        };

        var cut = RenderComponent<GoalFormTree>(parameters => parameters
            .Add(p => p.Indicators, [])
            .Add(p => p.Children, children));

        var deleteButton = cut.Find(".goal-form-tree-goal .goal-form-tree-action-btn.delete");
        deleteButton.GetAttribute("title").Should().Be("Excluir meta");
    }

    [Fact]
    public void Render_ClickGoalCard_ShouldInvokeOnNavigateInto()
    {
        var children = new List<TempGoal>
        {
            new("g-1", "Meta navegável", null)
        };

        int? navigatedIndex = null;
        var cut = RenderComponent<GoalFormTree>(parameters => parameters
            .Add(p => p.Indicators, [])
            .Add(p => p.Children, children)
            .Add(p => p.OnNavigateInto, idx => navigatedIndex = idx));

        cut.Find(".goal-form-tree-row").Click();

        navigatedIndex.Should().Be(0);
    }

    [Fact]
    public void Render_ChildWithContents_ShouldShowMiniIcons()
    {
        var children = new List<TempGoal>
        {
            new("g-1", "Meta", null)
            {
                Indicators =
                [
                    new TempIndicator(null, "Ind1", "Qualitative", "d"),
                    new TempIndicator(null, "Ind2", "Qualitative", "d")
                ],
                Children = [new TempGoal("g-2", "Sub", null)]
            }
        };

        var cut = RenderComponent<GoalFormTree>(parameters => parameters
            .Add(p => p.Indicators, [])
            .Add(p => p.Children, children));

        var miniIcons = cut.FindAll(".goal-form-tree-mini-icon");
        miniIcons.Should().HaveCount(3);
    }

    [Fact]
    public void Render_IndicatorCard_ShouldShowIndicadorLabel()
    {
        var indicators = new List<TempIndicator>
        {
            new(null, "Revenue", "Quantitative", "Atingir 100%")
        };

        var cut = RenderComponent<GoalFormTree>(parameters => parameters
            .Add(p => p.Indicators, indicators)
            .Add(p => p.Children, []));

        var label = cut.Find(".goal-form-tree-indicator .goal-form-tree-type-label");
        label.TextContent.Should().Be("Indicador");
    }

    [Fact]
    public void Render_GoalCard_ShouldNotShowMetaLabel()
    {
        var children = new List<TempGoal>
        {
            new("g-1", "Engajamento", null)
        };

        var cut = RenderComponent<GoalFormTree>(parameters => parameters
            .Add(p => p.Indicators, [])
            .Add(p => p.Children, children));

        var labels = cut.FindAll(".goal-form-tree-goal .goal-form-tree-type-label");
        labels.Should().BeEmpty();
    }
}
