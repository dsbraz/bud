using Bud.BlazorWasm.Components.Common;
using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Components;

#pragma warning disable IDE0011

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class GoalFormModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public bool IsEditMode { get; set; }
    [Parameter] public WizardMode Mode { get; set; } = WizardMode.Goal;
    [Parameter] public string OrganizationName { get; set; } = "Organização";
    [Parameter] public GoalFormModel? InitialModel { get; set; }
    [Parameter] public IEnumerable<ScopeOption> CollaboratorOptions { get; set; } = [];
    [Parameter] public List<TemplateResponse> Templates { get; set; } = [];
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<GoalFormResult> OnSave { get; set; }
    [Parameter] public EventCallback<GoalFormResult> OnSaveDraft { get; set; }

    // Wizard step state
    private WizardStep _currentStep = WizardStep.ChooseTemplate;
    private int _selectedTemplateIndex = -1; // -1 = "from scratch"
    private bool _showAddItemForm;
    private ItemType _pendingItemType = ItemType.None;

    // Unified add/edit item form fields
    private string _addItemName = "";
    private string? _addItemDescription;
    private string? _addItemDimension;
    private string? _addItemCollaboratorId;
    private DateTime _addItemStartDate = DateTime.Today;
    private DateTime _addItemEndDate = DateTime.Today.AddDays(7);
    private string? _addItemTargetGoalId; // null = root, set = TempId of target goal
    private int? _editingItemIndex; // non-null = editing existing item at this index
    private string? _editingItemStatusValue; // preserve status when editing goal

    // Root-level form fields (always visible, always editable)
    private string name = "";
    private string? description;
    private DateTime startDate = DateTime.Today;
    private DateTime endDate = DateTime.Today.AddDays(7);
    private string? collaboratorId;
    private string? statusValue;

    // Tree data (root level)
    private List<TempIndicator> tempIndicators = [];
    private List<TempTask> tempTasks = [];
    private List<TempGoal> tempGoals = [];

    // Navigation stack: indices into children at each depth
    private List<int> _navigationPath = [];

    // Inline form state
    private IndicatorFormFields.IndicatorFormModel _inlineIndicatorModel = new();

    // Edit tracking
    private HashSet<Guid> deletedIndicatorIds = [];
    private HashSet<Guid> deletedTaskIds = [];
    private HashSet<Guid> deletedGoalIds = [];

    private bool wasOpen;

    // ---- Computed properties ----

    private bool IsAtRoot => _navigationPath.Count == 0;

    private List<TempIndicator> CurrentIndicators
    {
        get
        {
            if (IsAtRoot) return tempIndicators;
            var goal = ResolveGoalAtPath();
            return goal.Indicators;
        }
    }

    private List<TempTask> CurrentTasks
    {
        get
        {
            if (IsAtRoot) return tempTasks;
            var goal = ResolveGoalAtPath();
            return goal.Tasks;
        }
    }

    private List<TempGoal> CurrentChildren
    {
        get
        {
            if (IsAtRoot) return tempGoals;
            var goal = ResolveGoalAtPath();
            return goal.Children;
        }
    }

    protected override void OnParametersSet()
    {
        if (IsOpen && !wasOpen)
        {
            InitializeFromModel(InitialModel);
        }
        wasOpen = IsOpen;
    }

    private void InitializeFromModel(GoalFormModel? model)
    {
        name = model?.Name ?? "";
        description = model?.Description;
        startDate = model?.StartDate ?? DateTime.Today;
        endDate = model?.EndDate ?? DateTime.Today.AddDays(7);
        collaboratorId = model?.CollaboratorId;
        statusValue = model?.StatusValue;
        tempIndicators = model?.Indicators?.ToList() ?? [];
        tempTasks = model?.Tasks?.ToList() ?? [];
        tempGoals = model?.Children?.ToList() ?? [];
        _navigationPath = [];
        CloseInlineForm();
        deletedIndicatorIds = [];
        deletedTaskIds = [];
        deletedGoalIds = [];

        // Wizard: edit mode and template mode skip to build step
        if (IsEditMode || Mode == WizardMode.Template)
            _currentStep = WizardStep.BuildGoal;
        else
            _currentStep = WizardStep.ChooseTemplate;

        _selectedTemplateIndex = -1;
    }

    // ---- Wizard Step Navigation ----

    private void SelectTemplate(int index)
    {
        _selectedTemplateIndex = index;
    }

    private void OpenUnifiedAddForm()
    {
        _showAddItemForm = true;
        _pendingItemType = ItemType.None;
        _addItemName = "";
        _addItemDescription = null;
        _addItemDimension = null;
        _addItemCollaboratorId = collaboratorId;
        _addItemStartDate = startDate;
        _addItemEndDate = endDate;
        _addItemTargetGoalId = null;
        _inlineIndicatorModel = new IndicatorFormFields.IndicatorFormModel();
    }

    private void OpenAddItemForChild(string goalTempId)
    {
        OpenUnifiedAddForm();
        _addItemTargetGoalId = goalTempId;
    }

    private string _pendingItemTypeValue
    {
        get => _pendingItemType.ToString();
        set
        {
            _pendingItemType = Enum.TryParse<ItemType>(value, out var t) ? t : ItemType.None;
            // Reset indicator model when switching types
            _inlineIndicatorModel = new IndicatorFormFields.IndicatorFormModel();
            _addItemDimension = null;
        }
    }

    private bool IsEditingItem => _editingItemIndex.HasValue;

    private void HandleAddItem()
    {
        if (string.IsNullOrWhiteSpace(_addItemName))
        {
            var verb = IsEditingItem ? "salvar" : "adicionar";
            ToastService.ShowError($"Erro ao {verb} item", "Informe o título do item.");
            return;
        }

        switch (_pendingItemType)
        {
            case ItemType.Indicator:
                HandleAddIndicatorItem();
                break;
            case ItemType.Task:
                HandleAddTaskItem();
                break;
            case ItemType.ChildGoal:
                HandleAddGoalItem();
                break;
            default:
                ToastService.ShowError("Erro ao adicionar item", "Selecione o modo de mensuração.");
                return;
        }

        CloseInlineForm();
    }

    private static TempGoal? FindGoalByTempId(string tempId, List<TempGoal> goals)
    {
        foreach (var g in goals)
        {
            if (g.TempId == tempId) return g;
            var found = FindGoalByTempId(tempId, g.Children);
            if (found != null) return found;
        }
        return null;
    }

    private TempGoal? AddItemTargetGoal =>
        _addItemTargetGoalId != null ? FindGoalByTempId(_addItemTargetGoalId, tempGoals) : null;

    private List<TempIndicator> AddItemTargetIndicators =>
        AddItemTargetGoal?.Indicators ?? CurrentIndicators;

    private List<TempTask> AddItemTargetTasks =>
        AddItemTargetGoal?.Tasks ?? CurrentTasks;

    private List<TempGoal> AddItemTargetChildren =>
        AddItemTargetGoal?.Children ?? CurrentChildren;

    private void HandleAddIndicatorItem()
    {
        var verb = IsEditingItem ? "salvar" : "adicionar";
        if (string.IsNullOrWhiteSpace(_inlineIndicatorModel.TypeValue))
        {
            ToastService.ShowError($"Erro ao {verb} indicador", "Selecione o tipo do indicador.");
            return;
        }

        var details = _inlineIndicatorModel.TypeValue == "Quantitative"
            ? BuildQuantitativeDetails(_inlineIndicatorModel.QuantitativeTypeValue, _inlineIndicatorModel.MinValue, _inlineIndicatorModel.MaxValue, _inlineIndicatorModel.UnitValue)
            : _inlineIndicatorModel.TargetText ?? "";

        var indicators = AddItemTargetIndicators;
        var originalId = IsEditingItem && _editingItemIndex!.Value < indicators.Count
            ? indicators[_editingItemIndex.Value].OriginalId
            : null;

        var indicator = new TempIndicator(
            OriginalId: originalId,
            Name: _addItemName.Trim(),
            Type: _inlineIndicatorModel.TypeValue,
            Details: details,
            QuantitativeType: _inlineIndicatorModel.QuantitativeTypeValue,
            MinValue: _inlineIndicatorModel.MinValue,
            MaxValue: _inlineIndicatorModel.MaxValue,
            TargetText: _inlineIndicatorModel.TargetText,
            Unit: _inlineIndicatorModel.UnitValue);

        if (IsEditingItem && _editingItemIndex!.Value < indicators.Count)
            indicators[_editingItemIndex.Value] = indicator;
        else
            indicators.Add(indicator);
    }

    private void HandleAddTaskItem()
    {
        var tasks = AddItemTargetTasks;
        var originalId = IsEditingItem && _editingItemIndex!.Value < tasks.Count
            ? tasks[_editingItemIndex.Value].OriginalId
            : null;
        var existingState = IsEditingItem && _editingItemIndex!.Value < tasks.Count
            ? tasks[_editingItemIndex.Value].State
            : TaskState.ToDo;

        var task = new TempTask(
            OriginalId: originalId,
            Name: _addItemName.Trim(),
            Description: string.IsNullOrWhiteSpace(_addItemDescription) ? null : _addItemDescription.Trim(),
            State: existingState,
            DueDate: null);

        if (IsEditingItem && _editingItemIndex!.Value < tasks.Count)
            tasks[_editingItemIndex.Value] = task;
        else
            tasks.Add(task);
    }

    private void HandleAddGoalItem()
    {
        if (Mode == WizardMode.Goal)
        {
            var parentStart = GetCurrentParentStartDate();
            if (_addItemStartDate < parentStart)
            {
                ToastService.ShowError("Erro ao salvar meta",
                    $"A data de início da meta não pode ser anterior à do pai ({parentStart:dd/MM/yyyy}).");
                return;
            }

            if (_addItemEndDate < _addItemStartDate)
            {
                ToastService.ShowError("Erro ao salvar meta",
                    "A data de fim precisa ser igual ou maior que a data de início.");
                return;
            }
        }

        var children = AddItemTargetChildren;

        var originalId = IsEditingItem && _editingItemIndex!.Value < children.Count
            ? children[_editingItemIndex.Value].OriginalId
            : null;
        var existingTempId = IsEditingItem && _editingItemIndex!.Value < children.Count
            ? children[_editingItemIndex.Value].TempId
            : Guid.NewGuid().ToString();
        var existingIndicators = IsEditingItem && _editingItemIndex!.Value < children.Count
            ? children[_editingItemIndex.Value].Indicators
            : [];
        var existingTasks = IsEditingItem && _editingItemIndex!.Value < children.Count
            ? children[_editingItemIndex.Value].Tasks
            : [];
        var existingChildren = IsEditingItem && _editingItemIndex!.Value < children.Count
            ? children[_editingItemIndex.Value].Children
            : [];

        var newGoal = new TempGoal(
            TempId: existingTempId,
            Name: _addItemName.Trim(),
            Description: string.IsNullOrWhiteSpace(_addItemDescription) ? null : _addItemDescription.Trim(),
            OriginalId: originalId,
            Dimension: string.IsNullOrWhiteSpace(_addItemDimension) ? null : _addItemDimension.Trim(),
            StartDate: _addItemStartDate,
            EndDate: _addItemEndDate,
            CollaboratorId: _addItemCollaboratorId,
            StatusValue: _editingItemStatusValue)
        {
            Indicators = existingIndicators,
            Tasks = existingTasks,
            Children = existingChildren
        };

        if (IsEditingItem && _editingItemIndex!.Value < children.Count)
            children[_editingItemIndex.Value] = newGoal;
        else
            children.Add(newGoal);
    }

    private void GoToStep(WizardStep step)
    {
        if (step < _currentStep)
            _currentStep = step;
    }

    private void HandleNext()
    {
        if (_currentStep == WizardStep.ChooseTemplate)
        {
            ApplySelectedTemplate();
            _currentStep = WizardStep.BuildGoal;
        }
        else if (_currentStep == WizardStep.BuildGoal)
        {
            CloseInlineForm();
            _currentStep = WizardStep.Review;
        }
    }

    private void HandleBack()
    {
        if (_currentStep == WizardStep.Review)
            _currentStep = WizardStep.BuildGoal;
        else if (_currentStep == WizardStep.BuildGoal)
            _currentStep = WizardStep.ChooseTemplate;
    }

    private void ApplySelectedTemplate()
    {
        if (_selectedTemplateIndex < 0 || _selectedTemplateIndex >= Templates.Count)
        {
            // "From scratch" — reset to blank
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "";
                description = null;
                startDate = DateTime.Today;
                endDate = DateTime.Today.AddDays(7);
                tempIndicators = [];
                tempTasks = [];
                tempGoals = [];
            }
            return;
        }

        var template = Templates[_selectedTemplateIndex];

        var directIndicators = template.Indicators
            .Where(m => !m.TemplateGoalId.HasValue)
            .OrderBy(m => m.OrderIndex)
            .Select(BuildTempIndicatorFromTemplate)
            .ToList();

        var children = template.Goals
            .OrderBy(o => o.OrderIndex)
            .Select(o =>
            {
                var goalIndicators = template.Indicators
                    .Where(m => m.TemplateGoalId == o.Id)
                    .OrderBy(m => m.OrderIndex)
                    .Select(BuildTempIndicatorFromTemplate)
                    .ToList();

                return new TempGoal(
                    Guid.NewGuid().ToString(),
                    o.Name,
                    o.Description,
                    null,
                    o.Dimension)
                {
                    Indicators = goalIndicators
                };
            })
            .ToList();

        name = template.GoalNamePattern ?? "";
        description = template.GoalDescriptionPattern;
        startDate = DateTime.Today;
        endDate = DateTime.Today.AddDays(90);
        tempIndicators = directIndicators;
        tempGoals = children;
        tempTasks = [];
    }

    private static TempIndicator BuildTempIndicatorFromTemplate(TemplateIndicatorResponse m)
    {
        return new TempIndicator(
            null, m.Name, m.Type.ToString(), GetTemplateIndicatorDetails(m),
            m.QuantitativeType?.ToString(), m.MinValue, m.MaxValue, m.TargetText, m.Unit?.ToString());
    }

    private static string GetTemplateIndicatorDetails(TemplateIndicatorResponse indicator)
    {
        if (indicator.Type == IndicatorType.Qualitative)
            return $"Qualitativa — {indicator.TargetText}";

        var parts = new List<string> { "Quantitativa" };
        if (indicator.QuantitativeType.HasValue)
            parts.Add(indicator.QuantitativeType.Value.ToString());
        if (indicator.Unit.HasValue)
            parts.Add(indicator.Unit.Value.ToString());
        if (indicator.MinValue.HasValue)
            parts.Add($"Min: {indicator.MinValue}");
        if (indicator.MaxValue.HasValue)
            parts.Add($"Max: {indicator.MaxValue}");
        return string.Join(" — ", parts);
    }

    private void HandleIndicatorTypeChanged(ChangeEventArgs e)
    {
        _inlineIndicatorModel.TypeValue = e.Value?.ToString();
        _inlineIndicatorModel.QuantitativeTypeValue = null;
        _inlineIndicatorModel.UnitValue = null;
        _inlineIndicatorModel.MinValue = null;
        _inlineIndicatorModel.MaxValue = null;
        _inlineIndicatorModel.TargetText = null;
    }

    // ---- Navigation ----

    private TempGoal ResolveGoalAtPath()
    {
        var children = tempGoals;
        TempGoal current = null!;
        foreach (var index in _navigationPath)
        {
            current = children[index];
            children = current.Children;
        }
        return current;
    }

    public void NavigateInto(int childIndex)
    {
        var currentChildren = CurrentChildren;
        if (childIndex < 0 || childIndex >= currentChildren.Count) return;

        CloseInlineForm();
        _navigationPath.Add(childIndex);
    }

    private void HandleNavigateInto(int childIndex)
    {
        // In create mode, items are flat — no deep navigation
        if (!IsEditMode) return;
        NavigateInto(childIndex);
    }

    public void NavigateTo(int depth)
    {
        if (depth < 0 || depth > _navigationPath.Count) return;
        if (depth == _navigationPath.Count) return; // Already at this level

        CloseInlineForm();
        _navigationPath = _navigationPath.Take(depth).ToList();
    }

    public List<(string Name, int Depth)> GetBreadcrumbSegments()
    {
        var segments = new List<(string Name, int Depth)>();

        var children = tempGoals;
        for (var i = 0; i < _navigationPath.Count; i++)
        {
            var index = _navigationPath[i];
            var goal = children[index];
            segments.Add((string.IsNullOrWhiteSpace(goal.Name) ? "Meta" : goal.Name, i + 1));
            children = goal.Children;
        }

        return segments;
    }

    // ---- Save / Close ----

    private async Task HandleSave()
    {
        CloseInlineForm();

        var entity = Mode == WizardMode.Template ? "modelo" : "missão";
        var errorTitle = IsEditMode ? "Erro ao salvar" : $"Erro ao criar {entity}";
        if (!Validate(errorTitle)) return;

        await OnSave.InvokeAsync(BuildResult());
    }

    private async Task HandleSaveDraft()
    {
        CloseInlineForm();

        if (!Validate("Erro ao salvar rascunho")) return;
        await OnSaveDraft.InvokeAsync(BuildResult());
    }

    private async Task HandleClose() => await OnClose.InvokeAsync();

    private bool Validate(string errorTitle)
    {
        var entity = Mode == WizardMode.Template ? "modelo" : "missão";
        if (string.IsNullOrWhiteSpace(name))
        {
            ToastService.ShowError(errorTitle, $"Informe o nome do {entity}.");
            return false;
        }
        if (Mode == WizardMode.Goal)
        {
            if (endDate < startDate)
            {
                ToastService.ShowError(errorTitle, "A data de fim precisa ser igual ou maior que a data de início.");
                return false;
            }
        }
        return true;
    }

    private GoalFormResult BuildResult() => new()
    {
        Name = name.Trim(),
        Description = description,
        StartDate = startDate,
        EndDate = endDate,
        CollaboratorId = collaboratorId,
        StatusValue = statusValue,
        Indicators = tempIndicators.ToList(),
        Tasks = tempTasks.ToList(),
        Children = tempGoals.ToList(),
        DeletedIndicatorIds = new HashSet<Guid>(deletedIndicatorIds),
        DeletedTaskIds = new HashSet<Guid>(deletedTaskIds),
        DeletedGoalIds = new HashSet<Guid>(deletedGoalIds)
    };

    // ---- Collaborator ----

    private void HandleCollaboratorChanged(ChangeEventArgs e)
    {
        var value = e.Value?.ToString();
        collaboratorId = string.IsNullOrEmpty(value) ? null : value;
    }

    private void OpenInlineIndicatorForm()
    {
        OpenUnifiedAddForm();
        _pendingItemType = ItemType.Indicator;
    }

    private void OpenInlineTaskForm()
    {
        OpenUnifiedAddForm();
        _pendingItemType = ItemType.Task;
    }

    private void OpenInlineGoalForm()
    {
        OpenUnifiedAddForm();
        _pendingItemType = ItemType.ChildGoal;
        _addItemCollaboratorId = collaboratorId;
    }

    private void OpenEditInlineIndicator(int index)
    {
        var indicators = CurrentIndicators;
        if (index < 0 || index >= indicators.Count) return;

        var existing = indicators[index];
        _showAddItemForm = true;
        _addItemTargetGoalId = null;
        _pendingItemType = ItemType.Indicator;
        _addItemName = existing.Name;
        _addItemDescription = null;
        _editingItemIndex = index;
        _addItemStartDate = startDate;
        _addItemEndDate = endDate;
        _inlineIndicatorModel = new IndicatorFormFields.IndicatorFormModel
        {
            TypeValue = existing.Type,
            QuantitativeTypeValue = existing.QuantitativeType,
            UnitValue = existing.Unit,
            MinValue = existing.MinValue,
            MaxValue = existing.MaxValue,
            TargetText = existing.TargetText
        };
    }

    private void OpenEditInlineTask(int index)
    {
        var tasks = CurrentTasks;
        if (index < 0 || index >= tasks.Count) return;

        var existing = tasks[index];
        _showAddItemForm = true;
        _addItemTargetGoalId = null;
        _pendingItemType = ItemType.Task;
        _addItemName = existing.Name;
        _addItemDescription = existing.Description;
        _editingItemIndex = index;
        _addItemStartDate = startDate;
        _addItemEndDate = endDate;
    }

    private void OpenEditInlineGoal(int index)
    {
        var children = CurrentChildren;
        if (index < 0 || index >= children.Count) return;

        var existing = children[index];
        _showAddItemForm = true;
        _addItemTargetGoalId = null;
        _pendingItemType = ItemType.ChildGoal;
        _addItemName = existing.Name;
        _addItemDescription = existing.Description;
        _addItemDimension = existing.Dimension;
        _addItemCollaboratorId = existing.CollaboratorId;
        _addItemStartDate = existing.StartDate ?? DateTime.Today;
        _addItemEndDate = existing.EndDate ?? DateTime.Today.AddDays(7);
        _editingItemIndex = index;
        _editingItemStatusValue = existing.StatusValue;
        _inlineIndicatorModel = new IndicatorFormFields.IndicatorFormModel();
    }

    // ---- Child-level Edit (expanded tree) ----

    private void EditChildIndicator((int childIndex, int itemIndex) args)
    {
        var children = CurrentChildren;
        if (args.childIndex < 0 || args.childIndex >= children.Count) return;
        var child = children[args.childIndex];
        if (args.itemIndex < 0 || args.itemIndex >= child.Indicators.Count) return;

        var existing = child.Indicators[args.itemIndex];
        _showAddItemForm = true;
        _addItemTargetGoalId = child.TempId;
        _pendingItemType = ItemType.Indicator;
        _addItemName = existing.Name;
        _addItemDescription = null;
        _editingItemIndex = args.itemIndex;
        _addItemStartDate = child.StartDate ?? startDate;
        _addItemEndDate = child.EndDate ?? endDate;
        _inlineIndicatorModel = new IndicatorFormFields.IndicatorFormModel
        {
            TypeValue = existing.Type,
            QuantitativeTypeValue = existing.QuantitativeType,
            UnitValue = existing.Unit,
            MinValue = existing.MinValue,
            MaxValue = existing.MaxValue,
            TargetText = existing.TargetText
        };
    }

    private void EditChildTask((int childIndex, int itemIndex) args)
    {
        var children = CurrentChildren;
        if (args.childIndex < 0 || args.childIndex >= children.Count) return;
        var child = children[args.childIndex];
        if (args.itemIndex < 0 || args.itemIndex >= child.Tasks.Count) return;

        var existing = child.Tasks[args.itemIndex];
        _showAddItemForm = true;
        _addItemTargetGoalId = child.TempId;
        _pendingItemType = ItemType.Task;
        _addItemName = existing.Name;
        _addItemDescription = existing.Description;
        _editingItemIndex = args.itemIndex;
        _addItemStartDate = child.StartDate ?? startDate;
        _addItemEndDate = child.EndDate ?? endDate;
        _inlineIndicatorModel = new IndicatorFormFields.IndicatorFormModel();
    }

    private void EditChildGoal((int childIndex, int itemIndex) args)
    {
        var children = CurrentChildren;
        if (args.childIndex < 0 || args.childIndex >= children.Count) return;
        var child = children[args.childIndex];
        if (args.itemIndex < 0 || args.itemIndex >= child.Children.Count) return;

        var existing = child.Children[args.itemIndex];
        _showAddItemForm = true;
        _addItemTargetGoalId = child.TempId;
        _pendingItemType = ItemType.ChildGoal;
        _addItemName = existing.Name;
        _addItemDescription = existing.Description;
        _addItemDimension = existing.Dimension;
        _addItemCollaboratorId = existing.CollaboratorId;
        _addItemStartDate = existing.StartDate ?? DateTime.Today;
        _addItemEndDate = existing.EndDate ?? DateTime.Today.AddDays(7);
        _editingItemIndex = args.itemIndex;
        _editingItemStatusValue = existing.StatusValue;
        _inlineIndicatorModel = new IndicatorFormFields.IndicatorFormModel();
    }

    // ---- Child-level Delete (expanded tree) ----

    private void DeleteChildIndicator((int childIndex, int itemIndex) args)
    {
        var children = CurrentChildren;
        if (args.childIndex < 0 || args.childIndex >= children.Count) return;
        var child = children[args.childIndex];
        if (args.itemIndex < 0 || args.itemIndex >= child.Indicators.Count) return;

        var indicator = child.Indicators[args.itemIndex];
        if (IsEditMode && indicator.OriginalId.HasValue)
            deletedIndicatorIds.Add(indicator.OriginalId.Value);
        child.Indicators.RemoveAt(args.itemIndex);
    }

    private void DeleteChildTask((int childIndex, int itemIndex) args)
    {
        var children = CurrentChildren;
        if (args.childIndex < 0 || args.childIndex >= children.Count) return;
        var child = children[args.childIndex];
        if (args.itemIndex < 0 || args.itemIndex >= child.Tasks.Count) return;

        var task = child.Tasks[args.itemIndex];
        if (IsEditMode && task.OriginalId.HasValue)
            deletedTaskIds.Add(task.OriginalId.Value);
        child.Tasks.RemoveAt(args.itemIndex);
    }

    private void DeleteChildGoal((int childIndex, int itemIndex) args)
    {
        var children = CurrentChildren;
        if (args.childIndex < 0 || args.childIndex >= children.Count) return;
        var child = children[args.childIndex];
        if (args.itemIndex < 0 || args.itemIndex >= child.Children.Count) return;

        var goal = child.Children[args.itemIndex];
        if (IsEditMode) CollectDeletedIds(goal);
        child.Children.RemoveAt(args.itemIndex);
    }

    private DateTime GetCurrentParentStartDate()
    {
        if (IsAtRoot) return startDate;
        var goal = ResolveGoalAtPath();
        return goal.StartDate ?? startDate;
    }

    // ---- Close Inline Form ----

    private void CloseInlineForm()
    {
        _showAddItemForm = false;
        _pendingItemType = ItemType.None;
        _addItemName = "";
        _addItemDescription = null;
        _addItemDimension = null;
        _addItemCollaboratorId = null;
        _addItemTargetGoalId = null;
        _editingItemIndex = null;
        _editingItemStatusValue = null;
        _inlineIndicatorModel = new IndicatorFormFields.IndicatorFormModel();
    }

    // ---- Delete ----

    private void DeleteIndicatorByIndex(int index)
    {
        var indicators = CurrentIndicators;
        if (index < 0 || index >= indicators.Count) return;
        var indicator = indicators[index];
        if (IsEditMode && indicator.OriginalId.HasValue)
        {
            deletedIndicatorIds.Add(indicator.OriginalId.Value);
        }
        indicators.RemoveAt(index);
    }

    private void DeleteTaskByIndex(int index)
    {
        var tasks = CurrentTasks;
        if (index < 0 || index >= tasks.Count) return;
        var task = tasks[index];
        if (IsEditMode && task.OriginalId.HasValue)
        {
            deletedTaskIds.Add(task.OriginalId.Value);
        }
        tasks.RemoveAt(index);
    }

    private void DeleteSubgoalByIndex(int index)
    {
        var children = CurrentChildren;
        if (index < 0 || index >= children.Count) return;
        var goal = children[index];

        if (IsEditMode)
        {
            CollectDeletedIds(goal);
        }

        children.RemoveAt(index);
    }

    private void CollectDeletedIds(TempGoal goal)
    {
        if (goal.OriginalId.HasValue)
            deletedGoalIds.Add(goal.OriginalId.Value);

        foreach (var indicator in goal.Indicators)
        {
            if (indicator.OriginalId.HasValue)
                deletedIndicatorIds.Add(indicator.OriginalId.Value);
        }

        foreach (var child in goal.Children)
        {
            CollectDeletedIds(child);
        }
    }

    // ---- Display Helpers ----

    private string GetModalTitle()
    {
        var entity = Mode == WizardMode.Template ? "modelo" : "missão";
        return IsEditMode ? $"Editar {entity}" : $"Criar {entity}";
    }

    private string GetNamePlaceholder() =>
        Mode == WizardMode.Template ? "Nome do template" : "Nome da missão";

    private string GetDescriptionPlaceholder() =>
        Mode == WizardMode.Template ? "Descrição do template" : "Adicionar breve descrição";

    private static string GetStatusLabel(GoalStatus status) => status switch
    {
        GoalStatus.Planned => "Planejada",
        GoalStatus.Active => "Ativa",
        GoalStatus.Completed => "Concluída",
        GoalStatus.Cancelled => "Cancelada",
        _ => status.ToString()
    };

    private static string BuildQuantitativeDetails(string? quantitativeType, decimal? minValue, decimal? maxValue, string? unit)
    {
        var unitLabel = unit switch
        {
            "Integer" or "Decimal" => "un",
            "Percentage" => "%",
            "Hours" => "h",
            "Points" => "pts",
            _ => ""
        };
        return quantitativeType switch
        {
            "KeepAbove" => $"Acima de {minValue} {unitLabel}",
            "KeepBelow" => $"Abaixo de {maxValue} {unitLabel}",
            "KeepBetween" => $"Entre {minValue} e {maxValue} {unitLabel}",
            "Achieve" => $"Atingir {maxValue} {unitLabel}",
            "Reduce" => $"Reduzir para {maxValue} {unitLabel}",
            _ => ""
        };
    }

    // ── Review helpers ──────────────────────────────────────────────────────

    private sealed record ReviewItem(int Depth, string Icon, string IconClass, string Name, string? Detail);

    private static IEnumerable<ReviewItem> FlattenGoalForReview(TempGoal goal, int depth)
    {
        yield return new ReviewItem(depth, "◎", "goal", goal.Name, null);
        foreach (var ind in goal.Indicators)
            yield return new ReviewItem(depth + 1, "◆", "indicator", ind.Name, string.IsNullOrEmpty(ind.Details) ? null : ind.Details);
        foreach (var tsk in goal.Tasks)
            yield return new ReviewItem(depth + 1, "✓", "task", tsk.Name, null);
        foreach (var child in goal.Children)
            foreach (var item in FlattenGoalForReview(child, depth + 1))
                yield return item;
    }
}
