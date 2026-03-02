using Bud.Client.Services;
using Bud.Client.Shared;
using Bud.Client.Shared.Goals;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Client.Tests.Shared.Goals;

public sealed class GoalFormModalTests : TestContext
{
    [Fact]
    public async Task HandleSave_WhenNameIsEmpty_ShouldShowValidationError()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;
        Services.AddSingleton(toastService);

        GoalFormResult? savedResult = null;
        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.OnSave, result => savedResult = result));

        var instance = cut.Instance;
        SetField(instance, "name", "");

        await InvokePrivateTask(instance, "HandleSave");

        savedResult.Should().BeNull();
        capturedToast.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleSave_WithValidGoalFields_ShouldInvokeOnSave()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;
        Services.AddSingleton(toastService);

        GoalFormResult? savedResult = null;
        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.OnSave, result => savedResult = result));

        var instance = cut.Instance;
        SetField(instance, "name", "Meta teste");
        SetField(instance, "scopeTypeValue", "Organization");
        SetField(instance, "scopeId", Guid.NewGuid().ToString());

        await InvokePrivateTask(instance, "HandleSave");

        savedResult.Should().NotBeNull();
        savedResult!.Name.Should().Be("Meta teste");
        capturedToast.Should().BeNull();
    }

    [Fact]
    public void DeleteSubgoalByIndex_ShouldRemoveGoalAndCollectDeletedIds()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var originalGoalId = Guid.NewGuid();
        var originalIndicatorId = Guid.NewGuid();

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsEditMode, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Children =
                [
                    new TempGoal("g-1", "Meta 1", null, originalGoalId)
                    {
                        Indicators = [new TempIndicator(originalIndicatorId, "Ind1", "Qualitative", "d", TargetText: "x")]
                    }
                ]
            }));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "DeleteSubgoalByIndex", 0);

        var goals = GetField<List<TempGoal>>(instance, "tempGoals");
        goals.Should().BeEmpty();

        var deletedGoalIds = GetField<HashSet<Guid>>(instance, "deletedGoalIds");
        deletedGoalIds.Should().Contain(originalGoalId);

        var deletedIndicatorIds = GetField<HashSet<Guid>>(instance, "deletedIndicatorIds");
        deletedIndicatorIds.Should().Contain(originalIndicatorId);
    }

    [Fact]
    public void NavigateInto_ShouldUpdatePathWithoutChangingRootFields()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Name = "Meta raiz",
                Children =
                [
                    new TempGoal("g-1", "Meta A", "Desc A", Dimension: "Financeiro")
                    {
                        Indicators = [new TempIndicator(null, "Ind1", "Qualitative", "d", TargetText: "x")]
                    }
                ]
            }));

        var instance = cut.Instance;
        instance.NavigateInto(0);

        // Root fields should NOT change — they stay as the root mission
        var name = GetField<string>(instance, "name");
        name.Should().Be("Meta raiz");

        var path = GetField<List<int>>(instance, "_navigationPath");
        path.Should().Equal(0);
    }

    [Fact]
    public void NavigateTo_ShouldTruncatePath()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Name = "Meta raiz",
                Children =
                [
                    new TempGoal("g-1", "Meta A", "Desc A")
                    {
                        Children =
                        [
                            new TempGoal("g-2", "Sub-meta B", null)
                        ]
                    }
                ]
            }));

        var instance = cut.Instance;
        instance.NavigateInto(0);
        instance.NavigateInto(0);
        GetField<List<int>>(instance, "_navigationPath").Should().Equal(0, 0);

        // Navigate back to root
        instance.NavigateTo(0);
        GetField<List<int>>(instance, "_navigationPath").Should().BeEmpty();
        // Root name is unchanged
        GetField<string>(instance, "name").Should().Be("Meta raiz");
    }

    [Fact]
    public void NavigateInto_ShouldCloseInlineForm()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Name = "Meta raiz",
                Children =
                [
                    new TempGoal("g-1", "Meta A", null)
                ]
            }));

        var instance = cut.Instance;

        // Open inline indicator form
        InvokePrivateVoid(instance, "OpenInlineIndicatorForm");
        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.NewIndicator);

        // Navigate into child — should close inline form
        instance.NavigateInto(0);
        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.None);
    }

    [Fact]
    public void NavigateTo_ShouldCloseInlineForm()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Name = "Meta raiz",
                Children =
                [
                    new TempGoal("g-1", "Meta A", null)
                ]
            }));

        var instance = cut.Instance;
        instance.NavigateInto(0);

        // Open inline goal form
        InvokePrivateVoid(instance, "OpenInlineGoalForm");
        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.NewGoal);

        // Navigate to root — should close inline form
        instance.NavigateTo(0);
        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.None);
    }

    [Fact]
    public void OpenInlineIndicatorForm_ShouldSetModeAndModel()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineIndicatorForm");

        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.NewIndicator);
        GetField<int?>(instance, "_editingInlineIndicatorIndex").Should().BeNull();
    }

    [Fact]
    public void OpenEditInlineIndicator_ShouldSetModeAndLoadModel()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Indicators = [new TempIndicator(null, "Revenue", "Quantitative", "Atingir 100 %", QuantitativeType: "Achieve", MaxValue: 100, Unit: "Percentage")]
            }));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenEditInlineIndicator", 0);

        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.EditIndicator);
        GetField<int?>(instance, "_editingInlineIndicatorIndex").Should().Be(0);

        var model = GetField<IndicatorFormFields.IndicatorFormModel>(instance, "_inlineIndicatorModel");
        model.Name.Should().Be("Revenue");
        model.TypeValue.Should().Be("Quantitative");
        model.QuantitativeTypeValue.Should().Be("Achieve");
        model.MaxValue.Should().Be(100);
        model.UnitValue.Should().Be("Percentage");
    }

    [Fact]
    public void OpenInlineGoalForm_ShouldInheritParentDefaults()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var scopeId = Guid.NewGuid().ToString();
        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 6, 30),
                ScopeTypeValue = "Team",
                ScopeId = scopeId
            }));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineGoalForm");

        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.NewGoal);
        GetField<string>(instance, "_inlineGoalName").Should().BeEmpty();
        GetField<string?>(instance, "_inlineGoalDimension").Should().BeNull();
        // Should inherit parent's dates and scope
        GetField<DateTime>(instance, "_inlineGoalStartDate").Should().Be(new DateTime(2026, 1, 1));
        GetField<DateTime>(instance, "_inlineGoalEndDate").Should().Be(new DateTime(2026, 6, 30));
        GetField<string?>(instance, "_inlineGoalScopeTypeValue").Should().Be("Team");
        GetField<string?>(instance, "_inlineGoalScopeId").Should().Be(scopeId);
    }

    [Fact]
    public void CloseInlineForm_ShouldResetMode()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineIndicatorForm");
        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.NewIndicator);

        InvokePrivateVoid(instance, "CloseInlineForm");
        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.None);
    }

    [Fact]
    public void CloseInlineForm_ShouldResetGoalFields()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineGoalForm");

        SetField(instance, "_inlineGoalName", "Algo");
        SetField<string?>(instance, "_inlineGoalScopeTypeValue", "Team");
        SetField<string?>(instance, "_inlineGoalScopeId", Guid.NewGuid().ToString());

        InvokePrivateVoid(instance, "CloseInlineForm");

        GetField<string>(instance, "_inlineGoalName").Should().BeEmpty();
        GetField<string?>(instance, "_inlineGoalScopeTypeValue").Should().BeNull();
        GetField<string?>(instance, "_inlineGoalScopeId").Should().BeNull();
    }

    [Fact]
    public void HandleInlineIndicatorSave_ShouldAddNewIndicatorToCurrentLevel()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineIndicatorForm");

        var model = GetField<IndicatorFormFields.IndicatorFormModel>(instance, "_inlineIndicatorModel");
        model.Name = "Revenue Growth";
        model.TypeValue = "Qualitative";
        model.TargetText = "Crescer 50%";

        InvokePrivateVoid(instance, "HandleInlineIndicatorSave");

        var indicators = GetField<List<TempIndicator>>(instance, "tempIndicators");
        indicators.Should().ContainSingle();
        indicators[0].Name.Should().Be("Revenue Growth");
        indicators[0].Type.Should().Be("Qualitative");
        indicators[0].Details.Should().Be("Crescer 50%");

        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.None);
    }

    [Fact]
    public void HandleInlineIndicatorSave_ShouldReplaceExistingIndicator()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Indicators = [new TempIndicator(Guid.NewGuid(), "Original", "Qualitative", "d", TargetText: "x")]
            }));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenEditInlineIndicator", 0);

        var model = GetField<IndicatorFormFields.IndicatorFormModel>(instance, "_inlineIndicatorModel");
        model.Name = "Updated";
        model.TypeValue = "Qualitative";
        model.TargetText = "Novo alvo";

        InvokePrivateVoid(instance, "HandleInlineIndicatorSave");

        var indicators = GetField<List<TempIndicator>>(instance, "tempIndicators");
        indicators.Should().ContainSingle();
        indicators[0].Name.Should().Be("Updated");
        indicators[0].Details.Should().Be("Novo alvo");
    }

    [Fact]
    public void HandleInlineIndicatorSave_WhileNavigated_ShouldAddToCurrentLevel()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Name = "Meta raiz",
                Children =
                [
                    new TempGoal("g-1", "Meta A", null)
                ]
            }));

        var instance = cut.Instance;
        instance.NavigateInto(0);

        InvokePrivateVoid(instance, "OpenInlineIndicatorForm");

        var model = GetField<IndicatorFormFields.IndicatorFormModel>(instance, "_inlineIndicatorModel");
        model.Name = "Ind child";
        model.TypeValue = "Qualitative";
        model.TargetText = "x";

        InvokePrivateVoid(instance, "HandleInlineIndicatorSave");

        // Root should have no indicators
        var rootIndicators = GetField<List<TempIndicator>>(instance, "tempIndicators");
        rootIndicators.Should().BeEmpty();

        // Child should have the indicator
        var childGoals = GetField<List<TempGoal>>(instance, "tempGoals");
        childGoals[0].Indicators.Should().ContainSingle();
        childGoals[0].Indicators[0].Name.Should().Be("Ind child");
    }

    [Fact]
    public void HandleInlineGoalSave_ShouldAddChildWithAllFieldsToCurrentLevel()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var scopeId = Guid.NewGuid().ToString();
        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineGoalForm");

        SetField(instance, "_inlineGoalName", "Engajamento");
        SetField<string?>(instance, "_inlineGoalDimension", "Processos");
        SetField<string?>(instance, "_inlineGoalScopeTypeValue", "Team");
        SetField<string?>(instance, "_inlineGoalScopeId", scopeId);
        SetField(instance, "_inlineGoalStartDate", new DateTime(2026, 3, 1));
        SetField(instance, "_inlineGoalEndDate", new DateTime(2026, 6, 30));
        SetField<string?>(instance, "_inlineGoalStatusValue", "Active");

        InvokePrivateVoid(instance, "HandleInlineGoalSave");

        var goals = GetField<List<TempGoal>>(instance, "tempGoals");
        goals.Should().ContainSingle();
        goals[0].Name.Should().Be("Engajamento");
        goals[0].Dimension.Should().Be("Processos");
        goals[0].ScopeTypeValue.Should().Be("Team");
        goals[0].ScopeId.Should().Be(scopeId);
        goals[0].StartDate.Should().Be(new DateTime(2026, 3, 1));
        goals[0].EndDate.Should().Be(new DateTime(2026, 6, 30));
        goals[0].StatusValue.Should().Be("Active");

        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.None);
    }

    [Fact]
    public void HandleInlineGoalSave_WhenNameIsEmpty_ShouldNotAddAndShowToast()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineGoalForm");

        // Name stays empty
        InvokePrivateVoid(instance, "HandleInlineGoalSave");

        var goals = GetField<List<TempGoal>>(instance, "tempGoals");
        goals.Should().BeEmpty();

        // Form should stay open so user can fix
        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.NewGoal);

        // Should show toast error
        capturedToast.Should().NotBeNull();
        capturedToast!.Message.Should().Contain("nome");
    }

    [Fact]
    public void OpenEditInlineGoal_ShouldLoadExistingGoalFields()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var scopeId = Guid.NewGuid().ToString();
        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Children =
                [
                    new TempGoal("g-1", "Meta existente", "Desc", Dimension: "Financeiro",
                        StartDate: new DateTime(2026, 1, 1), EndDate: new DateTime(2026, 12, 31),
                        ScopeTypeValue: "Team", ScopeId: scopeId, StatusValue: "Active")
                ]
            }));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenEditInlineGoal", 0);

        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.EditGoal);
        GetField<int?>(instance, "_editingInlineGoalIndex").Should().Be(0);
        GetField<string>(instance, "_inlineGoalName").Should().Be("Meta existente");
        GetField<string?>(instance, "_inlineGoalDimension").Should().Be("Financeiro");
        GetField<string?>(instance, "_inlineGoalScopeTypeValue").Should().Be("Team");
        GetField<string?>(instance, "_inlineGoalScopeId").Should().Be(scopeId);
        GetField<DateTime>(instance, "_inlineGoalStartDate").Should().Be(new DateTime(2026, 1, 1));
        GetField<DateTime>(instance, "_inlineGoalEndDate").Should().Be(new DateTime(2026, 12, 31));
        GetField<string?>(instance, "_inlineGoalStatusValue").Should().Be("Active");
    }

    [Fact]
    public void HandleInlineGoalSave_InEditMode_ShouldReplaceExistingGoal()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var originalId = Guid.NewGuid();
        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Children =
                [
                    new TempGoal("g-1", "Meta original", "Desc", originalId, "Financeiro")
                    {
                        Indicators = [new TempIndicator(null, "Ind1", "Qualitative", "d", TargetText: "x")]
                    }
                ]
            }));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenEditInlineGoal", 0);

        SetField(instance, "_inlineGoalName", "Meta atualizada");
        SetField<string?>(instance, "_inlineGoalDimension", "Processos");

        InvokePrivateVoid(instance, "HandleInlineGoalSave");

        var goals = GetField<List<TempGoal>>(instance, "tempGoals");
        goals.Should().ContainSingle();
        goals[0].Name.Should().Be("Meta atualizada");
        goals[0].Dimension.Should().Be("Processos");
        goals[0].OriginalId.Should().Be(originalId);
        // Indicators should be preserved
        goals[0].Indicators.Should().ContainSingle();
    }

    [Fact]
    public void HandleInlineGoalSave_WhenStartDateBeforeParentStartDate_ShouldShowToastError()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                StartDate = new DateTime(2026, 3, 1),
                EndDate = new DateTime(2026, 6, 30)
            }));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineGoalForm");

        SetField(instance, "_inlineGoalName", "Meta filha");
        // Set start date BEFORE the parent's start date
        SetField(instance, "_inlineGoalStartDate", new DateTime(2026, 1, 15));
        SetField(instance, "_inlineGoalEndDate", new DateTime(2026, 6, 30));

        InvokePrivateVoid(instance, "HandleInlineGoalSave");

        // Should NOT add the child
        var goals = GetField<List<TempGoal>>(instance, "tempGoals");
        goals.Should().BeEmpty();

        // Should show toast error
        capturedToast.Should().NotBeNull();
        capturedToast!.Message.Should().Contain("data de início");

        // Form should stay open
        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.NewGoal);
    }

    [Fact]
    public void HandleInlineGoalSave_WhenNavigatedIntoChild_ShouldValidateAgainstChildParentDate()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                Children =
                [
                    new TempGoal("g-1", "Meta pai", null,
                        StartDate: new DateTime(2026, 3, 1),
                        EndDate: new DateTime(2026, 6, 30))
                ]
            }));

        var instance = cut.Instance;
        instance.NavigateInto(0);

        InvokePrivateVoid(instance, "OpenInlineGoalForm");

        SetField(instance, "_inlineGoalName", "Meta neta");
        // Start date before the immediate parent (Meta pai: 2026-03-01), but after root
        SetField(instance, "_inlineGoalStartDate", new DateTime(2026, 2, 15));
        SetField(instance, "_inlineGoalEndDate", new DateTime(2026, 6, 30));

        InvokePrivateVoid(instance, "HandleInlineGoalSave");

        // Should NOT add the child
        var children = GetField<List<TempGoal>>(instance, "tempGoals")[0].Children;
        children.Should().BeEmpty();

        capturedToast.Should().NotBeNull();
    }

    [Fact]
    public void GetBreadcrumbSegments_AtRoot_ShouldReturnEmpty()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Acme Corp")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        SetField(instance, "name", "Minha Meta");
        var segments = instance.GetBreadcrumbSegments();

        // At root, no navigation segments (root shown in the fixed context above)
        segments.Should().BeEmpty();
    }

    [Fact]
    public void GetBreadcrumbSegments_InsideChild_ShouldReturnChildPath()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Acme Corp")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Name = "Meta raiz",
                Children =
                [
                    new TempGoal("g-1", "Engajamento", null)
                    {
                        Children =
                        [
                            new TempGoal("g-2", "NPS", null)
                        ]
                    }
                ]
            }));

        var instance = cut.Instance;
        instance.NavigateInto(0);
        instance.NavigateInto(0);

        var segments = instance.GetBreadcrumbSegments();

        // Only navigation children, no org
        segments.Should().HaveCount(2);
        segments[0].Name.Should().Be("Engajamento");
        segments[1].Name.Should().Be("NPS");
    }

    [Fact]
    public async Task HandleSave_WhileNavigated_ShouldPreserveRootFieldsAndChildren()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        GoalFormResult? savedResult = null;
        var scopeId = Guid.NewGuid().ToString();

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.OnSave, result => savedResult = result)
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Name = "Meta raiz",
                ScopeTypeValue = "Organization",
                ScopeId = scopeId,
                Children =
                [
                    new TempGoal("g-1", "Meta A", null)
                ]
            }));

        var instance = cut.Instance;
        instance.NavigateInto(0);

        // Save while navigated — root fields should be intact
        await InvokePrivateTask(instance, "HandleSave");

        savedResult.Should().NotBeNull();
        savedResult!.Name.Should().Be("Meta raiz");
        savedResult.Children.Should().ContainSingle();
        savedResult.Children[0].Name.Should().Be("Meta A");
    }

    [Fact]
    public async Task HandleSave_ShouldCloseInlineForm()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        GoalFormResult? savedResult = null;
        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.OnSave, result => savedResult = result));

        var instance = cut.Instance;
        SetField(instance, "name", "Meta");
        SetField(instance, "scopeTypeValue", "Organization");
        SetField(instance, "scopeId", Guid.NewGuid().ToString());

        InvokePrivateVoid(instance, "OpenInlineIndicatorForm");
        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.NewIndicator);

        await InvokePrivateTask(instance, "HandleSave");

        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.None);
    }

    [Fact]
    public void HandleInlineIndicatorSave_WhenNameIsEmpty_ShouldNotAddAndShowToast()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineIndicatorForm");

        // Model stays with empty name — don't set anything
        InvokePrivateVoid(instance, "HandleInlineIndicatorSave");

        var indicators = GetField<List<TempIndicator>>(instance, "tempIndicators");
        indicators.Should().BeEmpty();

        // Form should stay open
        GetField<InlineFormMode>(instance, "_inlineFormMode").Should().Be(InlineFormMode.NewIndicator);

        // Should show toast error
        capturedToast.Should().NotBeNull();
        capturedToast!.Message.Should().Contain("nome");
    }

    [Fact]
    public void HandleInlineIndicatorSave_WhenTypeIsEmpty_ShouldNotAddAndShowToast()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineIndicatorForm");

        var model = GetField<IndicatorFormFields.IndicatorFormModel>(instance, "_inlineIndicatorModel");
        model.Name = "Revenue Growth";
        // TypeValue stays null

        InvokePrivateVoid(instance, "HandleInlineIndicatorSave");

        var indicators = GetField<List<TempIndicator>>(instance, "tempIndicators");
        indicators.Should().BeEmpty();

        capturedToast.Should().NotBeNull();
        capturedToast!.Message.Should().Contain("tipo");
    }

    [Fact]
    public void GetModalTitle_GoalMode_ShouldReturnMissao()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Mode, WizardMode.Goal)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        var title = InvokePrivateString(instance, "GetModalTitle");
        title.Should().Be("Criar missão");
    }

    [Fact]
    public void GetModalTitle_GoalEditMode_ShouldReturnEditarMissao()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsEditMode, true)
            .Add(p => p.Mode, WizardMode.Goal)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        var title = InvokePrivateString(instance, "GetModalTitle");
        title.Should().Be("Editar missão");
    }

    [Fact]
    public void GetModalTitle_TemplateMode_ShouldReturnModelo()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Mode, WizardMode.Template)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        var title = InvokePrivateString(instance, "GetModalTitle");
        title.Should().Be("Criar modelo");
    }

    [Fact]
    public void Render_WhenNavigatedIntoChild_ShouldShowBreadcrumbLinks()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Acme Corp")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Name = "Missão raiz",
                Children =
                [
                    new TempGoal("g-1", "Engajamento", null)
                    {
                        Indicators = [new TempIndicator(null, "Ind1", "Qualitative", "d", TargetText: "x")]
                    }
                ]
            }));

        var instance = cut.Instance;
        instance.NavigateInto(0);
        cut.Render();

        // Should render breadcrumb with root mission link
        cut.Markup.Should().Contain("goal-form-nav-breadcrumb-link");
        cut.Markup.Should().Contain("Missão raiz");
    }

    [Fact]
    public void Render_WhenNavigatedIntoChild_ShouldShowCurrentChildName()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Acme Corp")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Name = "Missão raiz",
                Children =
                [
                    new TempGoal("g-1", "Engajamento", null)
                ]
            }));

        var instance = cut.Instance;
        instance.NavigateInto(0);
        cut.Render();

        // The current segment should be the child's name
        cut.Markup.Should().Contain("goal-form-nav-breadcrumb-current");
        cut.Markup.Should().Contain("Engajamento");
    }

    private static T GetField<T>(object instance, string name)
        => (T)instance.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(instance)!;

    private static void InvokePrivateVoid(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        method.Invoke(instance, args);
    }

    private static string InvokePrivateString(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (string)method.Invoke(instance, args)!;
    }

    private static void SetField<T>(object instance, string name, T value)
        => instance.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(instance, value);

    private static async Task InvokePrivateTask(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        if (method.Invoke(instance, args) is Task task)
        {
            await task;
        }
    }
}
