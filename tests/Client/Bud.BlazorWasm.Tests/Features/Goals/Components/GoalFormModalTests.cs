using Bud.BlazorWasm.Services;
using Bud.BlazorWasm.Components.Common;
using Bud.BlazorWasm.Features.Goals.Components;
using Bud.Shared.Contracts;
using Bud.Shared.Contracts.Responses;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.BlazorWasm.Tests.Features.Goals.Components;

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
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
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
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
            .Add(p => p.OnSave, result => savedResult = result));

        var instance = cut.Instance;
        SetField(instance, "name", "Meta teste");

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
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
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
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
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
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
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
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
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
        GetField<bool>(instance, "_showAddItemForm").Should().BeTrue();
        GetField<ItemType>(instance, "_pendingItemType").Should().Be(ItemType.Indicator);

        // Navigate into child — should close inline form
        instance.NavigateInto(0);
        GetField<bool>(instance, "_showAddItemForm").Should().BeFalse();
    }

    [Fact]
    public void NavigateTo_ShouldCloseInlineForm()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
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
        GetField<bool>(instance, "_showAddItemForm").Should().BeTrue();
        GetField<ItemType>(instance, "_pendingItemType").Should().Be(ItemType.ChildGoal);

        // Navigate to root — should close inline form
        instance.NavigateTo(0);
        GetField<bool>(instance, "_showAddItemForm").Should().BeFalse();
    }

    [Fact]
    public void OpenInlineIndicatorForm_ShouldSetModeAndModel()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineIndicatorForm");

        GetField<bool>(instance, "_showAddItemForm").Should().BeTrue();
        GetField<ItemType>(instance, "_pendingItemType").Should().Be(ItemType.Indicator);
        GetField<int?>(instance, "_editingItemIndex").Should().BeNull();
    }

    [Fact]
    public void OpenEditInlineIndicator_ShouldSetModeAndLoadModel()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Indicators = [new TempIndicator(null, "Revenue", "Quantitative", "Atingir 100 %", QuantitativeType: "Achieve", MaxValue: 100, Unit: "Percentage")]
            }));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenEditInlineIndicator", 0);

        GetField<bool>(instance, "_showAddItemForm").Should().BeTrue();
        GetField<ItemType>(instance, "_pendingItemType").Should().Be(ItemType.Indicator);
        GetField<int?>(instance, "_editingItemIndex").Should().Be(0);

        GetField<string>(instance, "_addItemName").Should().Be("Revenue");
        var model = GetField<IndicatorFormFields.IndicatorFormModel>(instance, "_inlineIndicatorModel");
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

        var collaboratorId = Guid.NewGuid().ToString();
        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 6, 30),
                CollaboratorId = collaboratorId
            }));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineGoalForm");

        GetField<bool>(instance, "_showAddItemForm").Should().BeTrue();
        GetField<ItemType>(instance, "_pendingItemType").Should().Be(ItemType.ChildGoal);
        GetField<string>(instance, "_addItemName").Should().BeEmpty();
        GetField<string?>(instance, "_addItemDimension").Should().BeNull();
        // Should inherit parent's dates
        GetField<DateTime>(instance, "_addItemStartDate").Should().Be(new DateTime(2026, 1, 1));
        GetField<DateTime>(instance, "_addItemEndDate").Should().Be(new DateTime(2026, 6, 30));
    }

    [Fact]
    public void CloseInlineForm_ShouldResetMode()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineIndicatorForm");
        GetField<bool>(instance, "_showAddItemForm").Should().BeTrue();
        GetField<ItemType>(instance, "_pendingItemType").Should().Be(ItemType.Indicator);

        InvokePrivateVoid(instance, "CloseInlineForm");
        GetField<bool>(instance, "_showAddItemForm").Should().BeFalse();
        GetField<ItemType>(instance, "_pendingItemType").Should().Be(ItemType.None);
    }

    [Fact]
    public void CloseInlineForm_ShouldResetGoalFields()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineGoalForm");

        SetField(instance, "_addItemName", "Algo");
        SetField<string?>(instance, "_addItemCollaboratorId", Guid.NewGuid().ToString());

        InvokePrivateVoid(instance, "CloseInlineForm");

        GetField<string>(instance, "_addItemName").Should().BeEmpty();
        GetField<string?>(instance, "_addItemCollaboratorId").Should().BeNull();
    }

    [Fact]
    public void HandleAddItem_Indicator_ShouldAddNewIndicatorToCurrentLevel()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineIndicatorForm");

        SetField(instance, "_addItemName", "Revenue Growth");
        var model = GetField<IndicatorFormFields.IndicatorFormModel>(instance, "_inlineIndicatorModel");
        model.TypeValue = "Qualitative";
        model.TargetText = "Crescer 50%";

        InvokePrivateVoid(instance, "HandleAddItem");

        var indicators = GetField<List<TempIndicator>>(instance, "tempIndicators");
        indicators.Should().ContainSingle();
        indicators[0].Name.Should().Be("Revenue Growth");
        indicators[0].Type.Should().Be("Qualitative");
        indicators[0].Details.Should().Be("Crescer 50%");

        GetField<bool>(instance, "_showAddItemForm").Should().BeFalse();
    }

    [Fact]
    public void HandleAddItem_Indicator_ShouldReplaceExistingIndicator()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Indicators = [new TempIndicator(Guid.NewGuid(), "Original", "Qualitative", "d", TargetText: "x")]
            }));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenEditInlineIndicator", 0);

        SetField(instance, "_addItemName", "Updated");
        var model = GetField<IndicatorFormFields.IndicatorFormModel>(instance, "_inlineIndicatorModel");
        model.TypeValue = "Qualitative";
        model.TargetText = "Novo alvo";

        InvokePrivateVoid(instance, "HandleAddItem");

        var indicators = GetField<List<TempIndicator>>(instance, "tempIndicators");
        indicators.Should().ContainSingle();
        indicators[0].Name.Should().Be("Updated");
        indicators[0].Details.Should().Be("Novo alvo");
    }

    [Fact]
    public void HandleAddItem_Indicator_WhileNavigated_ShouldAddToCurrentLevel()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
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

        SetField(instance, "_addItemName", "Ind child");
        var model = GetField<IndicatorFormFields.IndicatorFormModel>(instance, "_inlineIndicatorModel");
        model.TypeValue = "Qualitative";
        model.TargetText = "x";

        InvokePrivateVoid(instance, "HandleAddItem");

        // Root should have no indicators
        var rootIndicators = GetField<List<TempIndicator>>(instance, "tempIndicators");
        rootIndicators.Should().BeEmpty();

        // Child should have the indicator
        var childGoals = GetField<List<TempGoal>>(instance, "tempGoals");
        childGoals[0].Indicators.Should().ContainSingle();
        childGoals[0].Indicators[0].Name.Should().Be("Ind child");
    }

    [Fact]
    public void HandleAddItem_ChildGoal_ShouldAddChildWithAllFieldsToCurrentLevel()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var collaboratorId = Guid.NewGuid().ToString();
        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineGoalForm");

        SetField(instance, "_addItemName", "Engajamento");
        SetField<string?>(instance, "_addItemDimension", "Processos");
        SetField<string?>(instance, "_addItemCollaboratorId", collaboratorId);
        SetField(instance, "_addItemStartDate", DateTime.Today);
        SetField(instance, "_addItemEndDate", DateTime.Today.AddMonths(3));
        SetField<string?>(instance, "_editingItemStatusValue", "Active");

        InvokePrivateVoid(instance, "HandleAddItem");

        var goals = GetField<List<TempGoal>>(instance, "tempGoals");
        goals.Should().ContainSingle();
        goals[0].Name.Should().Be("Engajamento");
        goals[0].Dimension.Should().Be("Processos");
        goals[0].CollaboratorId.Should().Be(collaboratorId);
        goals[0].StartDate.Should().Be(DateTime.Today);
        goals[0].EndDate.Should().Be(DateTime.Today.AddMonths(3));
        goals[0].StatusValue.Should().Be("Active");

        GetField<bool>(instance, "_showAddItemForm").Should().BeFalse();
    }

    [Fact]
    public void HandleAddItem_ChildGoal_WhenNameIsEmpty_ShouldNotAddAndShowToast()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineGoalForm");

        // Name stays empty
        InvokePrivateVoid(instance, "HandleAddItem");

        var goals = GetField<List<TempGoal>>(instance, "tempGoals");
        goals.Should().BeEmpty();

        // Form should stay open so user can fix
        GetField<bool>(instance, "_showAddItemForm").Should().BeTrue();
        GetField<ItemType>(instance, "_pendingItemType").Should().Be(ItemType.ChildGoal);

        // Should show toast error
        capturedToast.Should().NotBeNull();
        capturedToast!.Message.Should().Contain("título");
    }

    [Fact]
    public void OpenEditInlineGoal_ShouldLoadExistingGoalFields()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var collaboratorId = Guid.NewGuid().ToString();
        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Children =
                [
                    new TempGoal("g-1", "Meta existente", "Desc", Dimension: "Financeiro",
                        StartDate: new DateTime(2026, 1, 1), EndDate: new DateTime(2026, 12, 31),
                        CollaboratorId: collaboratorId, StatusValue: "Active")
                ]
            }));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenEditInlineGoal", 0);

        GetField<bool>(instance, "_showAddItemForm").Should().BeTrue();
        GetField<ItemType>(instance, "_pendingItemType").Should().Be(ItemType.ChildGoal);
        GetField<int?>(instance, "_editingItemIndex").Should().Be(0);
        GetField<string>(instance, "_addItemName").Should().Be("Meta existente");
        GetField<string?>(instance, "_addItemDimension").Should().Be("Financeiro");
        GetField<string?>(instance, "_addItemCollaboratorId").Should().Be(collaboratorId);
        GetField<DateTime>(instance, "_addItemStartDate").Should().Be(new DateTime(2026, 1, 1));
        GetField<DateTime>(instance, "_addItemEndDate").Should().Be(new DateTime(2026, 12, 31));
        GetField<string?>(instance, "_editingItemStatusValue").Should().Be("Active");
    }

    [Fact]
    public void HandleAddItem_ChildGoal_InEditMode_ShouldReplaceExistingGoal()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var originalId = Guid.NewGuid();
        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
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

        SetField(instance, "_addItemName", "Meta atualizada");
        SetField<string?>(instance, "_addItemDimension", "Processos");

        InvokePrivateVoid(instance, "HandleAddItem");

        var goals = GetField<List<TempGoal>>(instance, "tempGoals");
        goals.Should().ContainSingle();
        goals[0].Name.Should().Be("Meta atualizada");
        goals[0].Dimension.Should().Be("Processos");
        goals[0].OriginalId.Should().Be(originalId);
        // Indicators should be preserved
        goals[0].Indicators.Should().ContainSingle();
    }

    [Fact]
    public void HandleAddItem_ChildGoal_WhenStartDateBeforeParentStartDate_ShouldShowToastError()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                StartDate = new DateTime(2026, 3, 1),
                EndDate = new DateTime(2026, 6, 30)
            }));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineGoalForm");

        SetField(instance, "_addItemName", "Meta filha");
        // Set start date BEFORE the parent's start date
        SetField(instance, "_addItemStartDate", new DateTime(2026, 1, 15));
        SetField(instance, "_addItemEndDate", new DateTime(2026, 6, 30));

        InvokePrivateVoid(instance, "HandleAddItem");

        // Should NOT add the child
        var goals = GetField<List<TempGoal>>(instance, "tempGoals");
        goals.Should().BeEmpty();

        // Should show toast error
        capturedToast.Should().NotBeNull();
        capturedToast!.Message.Should().Contain("data de início");

        // Form gets closed after HandleAddItem completes
        GetField<bool>(instance, "_showAddItemForm").Should().BeFalse();
    }

    [Fact]
    public void HandleAddItem_ChildGoal_WhenNavigatedIntoChild_ShouldValidateAgainstChildParentDate()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
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

        SetField(instance, "_addItemName", "Meta neta");
        // Start date before the immediate parent (Meta pai: 2026-03-01), but after root
        SetField(instance, "_addItemStartDate", new DateTime(2026, 2, 15));
        SetField(instance, "_addItemEndDate", new DateTime(2026, 6, 30));

        InvokePrivateVoid(instance, "HandleAddItem");

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
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

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
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
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

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
            .Add(p => p.OnSave, result => savedResult = result)
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
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
            .Add(p => p.OnSave, result => savedResult = result));

        var instance = cut.Instance;
        SetField(instance, "name", "Meta");

        InvokePrivateVoid(instance, "OpenInlineIndicatorForm");
        GetField<bool>(instance, "_showAddItemForm").Should().BeTrue();
        GetField<ItemType>(instance, "_pendingItemType").Should().Be(ItemType.Indicator);

        await InvokePrivateTask(instance, "HandleSave");

        GetField<bool>(instance, "_showAddItemForm").Should().BeFalse();
    }

    [Fact]
    public void HandleAddItem_Indicator_WhenNameIsEmpty_ShouldNotAddAndShowToast()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineIndicatorForm");

        // _addItemName stays empty — don't set anything
        InvokePrivateVoid(instance, "HandleAddItem");

        var indicators = GetField<List<TempIndicator>>(instance, "tempIndicators");
        indicators.Should().BeEmpty();

        // Form should stay open
        GetField<bool>(instance, "_showAddItemForm").Should().BeTrue();
        GetField<ItemType>(instance, "_pendingItemType").Should().Be(ItemType.Indicator);

        // Should show toast error
        capturedToast.Should().NotBeNull();
        capturedToast!.Message.Should().Contain("título");
    }

    [Fact]
    public void HandleAddItem_Indicator_WhenTypeIsEmpty_ShouldNotAddAndShowToast()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "OpenInlineIndicatorForm");

        SetField(instance, "_addItemName", "Revenue Growth");
        // TypeValue stays null on _inlineIndicatorModel

        InvokePrivateVoid(instance, "HandleAddItem");

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
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

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
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

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
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        var title = InvokePrivateString(instance, "GetModalTitle");
        title.Should().Be("Criar modelo");
    }

    [Fact]
    public void Render_EditMode_ShouldShowChildGoalsInExpandableTree()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsEditMode, true)
            .Add(p => p.OrganizationName, "Acme Corp")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Name = "Meta raiz",
                Children =
                [
                    new TempGoal("g-1", "Engajamento", null)
                    {
                        Indicators = [new TempIndicator(null, "Ind1", "Qualitative", "d", TargetText: "x")]
                    }
                ]
            }));

        // In edit mode, child goals are shown in the expandable tree (same as create mode)
        cut.Markup.Should().Contain("goal-form-tree");
        cut.Markup.Should().Contain("Engajamento");
    }

    [Fact]
    public void Render_EditMode_ShouldShowSameContainerAsCreateMode()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsEditMode, true)
            .Add(p => p.OrganizationName, "Acme Corp")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Name = "Meta raiz",
                Children =
                [
                    new TempGoal("g-1", "Engajamento", null)
                ]
            }));

        // Edit mode uses the same unified add-item container as create mode
        cut.Markup.Should().Contain("wizard-add-item-container");
        cut.Markup.Should().NotContain("goal-form-nav-breadcrumb");
    }

    // ---- Wizard Step Navigation Tests ----

    [Fact]
    public void Initialize_InCreateMode_ShouldStartAtChooseTemplateStep()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        GetField<WizardStep>(instance, "_currentStep").Should().Be(WizardStep.ChooseTemplate);
    }

    [Fact]
    public void Initialize_InEditMode_ShouldStartAtBuildGoalStep()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsEditMode, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        GetField<WizardStep>(instance, "_currentStep").Should().Be(WizardStep.BuildGoal);
    }

    [Fact]
    public void Initialize_InTemplateMode_ShouldStartAtBuildGoalStep()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Mode, WizardMode.Template)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        GetField<WizardStep>(instance, "_currentStep").Should().Be(WizardStep.BuildGoal);
    }

    [Fact]
    public void HandleNext_FromChooseTemplate_ShouldAdvanceToBuildGoal()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "HandleNext");

        GetField<WizardStep>(instance, "_currentStep").Should().Be(WizardStep.BuildGoal);
    }

    [Fact]
    public void HandleNext_FromBuildGoal_ShouldAdvanceToReview()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        SetField(instance, "_currentStep", WizardStep.BuildGoal);

        InvokePrivateVoid(instance, "HandleNext");

        GetField<WizardStep>(instance, "_currentStep").Should().Be(WizardStep.Review);
    }

    [Fact]
    public void HandleBack_FromReview_ShouldGoBackToBuildGoal()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        SetField(instance, "_currentStep", WizardStep.Review);

        InvokePrivateVoid(instance, "HandleBack");

        GetField<WizardStep>(instance, "_currentStep").Should().Be(WizardStep.BuildGoal);
    }

    [Fact]
    public void HandleBack_FromBuildGoal_ShouldGoBackToChooseTemplate()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        SetField(instance, "_currentStep", WizardStep.BuildGoal);

        InvokePrivateVoid(instance, "HandleBack");

        GetField<WizardStep>(instance, "_currentStep").Should().Be(WizardStep.ChooseTemplate);
    }

    [Fact]
    public void SelectTemplate_ShouldUpdateSelectedIndex()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
            .Add(p => p.Templates, [new TemplateResponse { Id = Guid.NewGuid(), Name = "T1", Indicators = [] }]));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "SelectTemplate", 0);

        GetField<int>(instance, "_selectedTemplateIndex").Should().Be(0);
    }

    [Fact]
    public void ApplySelectedTemplate_WithTemplate_ShouldPopulateFormFields()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var templateId = Guid.NewGuid();
        var template = new TemplateResponse
        {
            Id = templateId,
            Name = "OKR Template",
            GoalNamePattern = "OKR Q1",
            GoalDescriptionPattern = "Objetivos do Q1",
            Goals = [new TemplateGoalResponse { Id = Guid.NewGuid(), Name = "Meta A", OrderIndex = 0, Indicators = [] }],
            Indicators = [new TemplateIndicatorResponse { Id = Guid.NewGuid(), Name = "Revenue", Type = IndicatorType.Qualitative, TargetText = "Crescer", OrderIndex = 0 }]
        };

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
            .Add(p => p.Templates, [template]));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "SelectTemplate", 0);
        InvokePrivateVoid(instance, "HandleNext");

        GetField<string>(instance, "name").Should().Be("OKR Q1");
        GetField<string?>(instance, "description").Should().Be("Objetivos do Q1");
        GetField<List<TempIndicator>>(instance, "tempIndicators").Should().ContainSingle();
        GetField<List<TempGoal>>(instance, "tempGoals").Should().ContainSingle();
        GetField<WizardStep>(instance, "_currentStep").Should().Be(WizardStep.BuildGoal);
    }

    [Fact]
    public void ApplySelectedTemplate_FromScratch_ShouldKeepBlankForm()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        // -1 is "from scratch" (default)
        InvokePrivateVoid(instance, "HandleNext");

        GetField<string>(instance, "name").Should().BeEmpty();
        GetField<List<TempIndicator>>(instance, "tempIndicators").Should().BeEmpty();
        GetField<WizardStep>(instance, "_currentStep").Should().Be(WizardStep.BuildGoal);
    }

    [Fact]
    public void Render_Step1_ShouldShowTemplateGrid()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
            .Add(p => p.Templates, [new TemplateResponse { Id = Guid.NewGuid(), Name = "Meu Template", Indicators = [] }]));

        cut.Markup.Should().Contain("wizard-template-grid");
        cut.Markup.Should().Contain("Criar do zero");
        cut.Markup.Should().Contain("Meu Template");
    }

    [Fact]
    public void Render_Step3_ShouldShowReviewSummary()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new GoalFormModel
            {
                Name = "Meta Revisão",
                Description = "Descrição da missão",
                Indicators = [new TempIndicator(null, "Ind1", "Qualitative", "Meta qualitativa", TargetText: "x")]
            }));

        var instance = cut.Instance;
        SetField(instance, "_currentStep", WizardStep.Review);
        cut.Render();

        cut.Markup.Should().Contain("wizard-review");
        cut.Markup.Should().Contain("Meta Revisão");
        cut.Markup.Should().Contain("Descrição da missão");
        cut.Markup.Should().Contain("Ind1");
    }

    [Fact]
    public void GoToStep_ShouldNavigateBackToPreviousStep()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        SetField(instance, "_currentStep", WizardStep.Review);

        InvokePrivateVoid(instance, "GoToStep", WizardStep.ChooseTemplate);

        GetField<WizardStep>(instance, "_currentStep").Should().Be(WizardStep.ChooseTemplate);
    }

    [Fact]
    public void GoToStep_ShouldNotNavigateForwardFromPreviousStep()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<GoalFormModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.CollaboratorOptions, Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        // Currently at ChooseTemplate (step 1)
        InvokePrivateVoid(instance, "GoToStep", WizardStep.Review);

        // Should stay at ChooseTemplate — cannot skip forward
        GetField<WizardStep>(instance, "_currentStep").Should().Be(WizardStep.ChooseTemplate);
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
