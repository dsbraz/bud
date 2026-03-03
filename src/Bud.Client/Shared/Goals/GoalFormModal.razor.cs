using Bud.Client.Shared;
using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Components;

#pragma warning disable IDE0011

namespace Bud.Client.Shared.Goals;

public partial class GoalFormModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public bool IsEditMode { get; set; }
    [Parameter] public WizardMode Mode { get; set; } = WizardMode.Goal;
    [Parameter] public string OrganizationName { get; set; } = "Organização";
    [Parameter] public GoalFormModel? InitialModel { get; set; }
    [Parameter] public IEnumerable<ScopeOption> CollaboratorOptions { get; set; } = [];
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<GoalFormResult> OnSave { get; set; }
    [Parameter] public EventCallback<GoalFormResult> OnSaveDraft { get; set; }

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
    private InlineFormMode _inlineFormMode;
    private IndicatorFormFields.IndicatorFormModel _inlineIndicatorModel = new();
    private int? _editingInlineIndicatorIndex;

    // Inline task form state
    private string _inlineTaskName = "";
    private string? _inlineTaskDescription;
    private TaskState _inlineTaskState = TaskState.ToDo;
    private DateTime? _inlineTaskDueDate;
    private int? _editingInlineTaskIndex;

    // Inline goal form state
    private string _inlineGoalName = "";
    private string? _inlineGoalDescription;
    private string? _inlineGoalDimension;
    private DateTime _inlineGoalStartDate = DateTime.Today;
    private DateTime _inlineGoalEndDate = DateTime.Today.AddDays(7);
    private string? _inlineGoalCollaboratorId;
    private string? _inlineGoalStatusValue;
    private int? _editingInlineGoalIndex;

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

    private void HandleInlineGoalCollaboratorChanged(ChangeEventArgs e)
    {
        var value = e.Value?.ToString();
        _inlineGoalCollaboratorId = string.IsNullOrEmpty(value) ? null : value;
    }

    // ---- Inline Indicator Form ----

    private void OpenInlineIndicatorForm()
    {
        _inlineFormMode = InlineFormMode.NewIndicator;
        _inlineIndicatorModel = new IndicatorFormFields.IndicatorFormModel();
        _editingInlineIndicatorIndex = null;
    }

    private void OpenEditInlineIndicator(int index)
    {
        var indicators = CurrentIndicators;
        if (index < 0 || index >= indicators.Count) return;

        var existing = indicators[index];
        _inlineFormMode = InlineFormMode.EditIndicator;
        _editingInlineIndicatorIndex = index;
        _inlineIndicatorModel = new IndicatorFormFields.IndicatorFormModel
        {
            Name = existing.Name,
            TypeValue = existing.Type,
            QuantitativeTypeValue = existing.QuantitativeType,
            UnitValue = existing.Unit,
            MinValue = existing.MinValue,
            MaxValue = existing.MaxValue,
            TargetText = existing.TargetText
        };
    }

    private void HandleInlineIndicatorSave()
    {
        if (string.IsNullOrWhiteSpace(_inlineIndicatorModel.Name))
        {
            ToastService.ShowError("Erro ao salvar indicador", "Informe o nome do indicador.");
            return;
        }
        if (string.IsNullOrWhiteSpace(_inlineIndicatorModel.TypeValue))
        {
            ToastService.ShowError("Erro ao salvar indicador", "Selecione o tipo do indicador.");
            return;
        }

        var details = _inlineIndicatorModel.TypeValue == "Quantitative"
            ? BuildQuantitativeDetails(_inlineIndicatorModel.QuantitativeTypeValue, _inlineIndicatorModel.MinValue, _inlineIndicatorModel.MaxValue, _inlineIndicatorModel.UnitValue)
            : _inlineIndicatorModel.TargetText ?? "";

        var indicators = CurrentIndicators;
        var originalId = _editingInlineIndicatorIndex.HasValue && _editingInlineIndicatorIndex.Value < indicators.Count
            ? indicators[_editingInlineIndicatorIndex.Value].OriginalId
            : null;

        var indicator = new TempIndicator(
            OriginalId: originalId,
            Name: _inlineIndicatorModel.Name,
            Type: _inlineIndicatorModel.TypeValue,
            Details: details,
            QuantitativeType: _inlineIndicatorModel.QuantitativeTypeValue,
            MinValue: _inlineIndicatorModel.MinValue,
            MaxValue: _inlineIndicatorModel.MaxValue,
            TargetText: _inlineIndicatorModel.TargetText,
            Unit: _inlineIndicatorModel.UnitValue);

        if (_editingInlineIndicatorIndex.HasValue && _editingInlineIndicatorIndex.Value < indicators.Count)
        {
            indicators[_editingInlineIndicatorIndex.Value] = indicator;
        }
        else
        {
            indicators.Add(indicator);
        }

        CloseInlineForm();
    }

    // ---- Inline Task Form ----

    private void OpenInlineTaskForm()
    {
        _inlineFormMode = InlineFormMode.NewTask;
        _inlineTaskName = "";
        _inlineTaskDescription = null;
        _inlineTaskState = TaskState.ToDo;
        _inlineTaskDueDate = null;
        _editingInlineTaskIndex = null;
    }

    private void OpenEditInlineTask(int index)
    {
        var tasks = CurrentTasks;
        if (index < 0 || index >= tasks.Count) return;

        var existing = tasks[index];
        _inlineFormMode = InlineFormMode.EditTask;
        _editingInlineTaskIndex = index;
        _inlineTaskName = existing.Name;
        _inlineTaskDescription = existing.Description;
        _inlineTaskState = existing.State;
        _inlineTaskDueDate = existing.DueDate;
    }

    private void HandleInlineTaskSave()
    {
        if (string.IsNullOrWhiteSpace(_inlineTaskName))
        {
            ToastService.ShowError("Erro ao salvar tarefa", "Informe o nome da tarefa.");
            return;
        }

        var tasks = CurrentTasks;
        var originalId = _editingInlineTaskIndex.HasValue && _editingInlineTaskIndex.Value < tasks.Count
            ? tasks[_editingInlineTaskIndex.Value].OriginalId
            : null;

        var task = new TempTask(
            OriginalId: originalId,
            Name: _inlineTaskName.Trim(),
            Description: string.IsNullOrWhiteSpace(_inlineTaskDescription) ? null : _inlineTaskDescription.Trim(),
            State: _inlineTaskState,
            DueDate: _inlineTaskDueDate);

        if (_editingInlineTaskIndex.HasValue && _editingInlineTaskIndex.Value < tasks.Count)
            tasks[_editingInlineTaskIndex.Value] = task;
        else
            tasks.Add(task);

        CloseInlineForm();
    }

    // ---- Inline Goal Form ----

    private void OpenInlineGoalForm()
    {
        _inlineFormMode = InlineFormMode.NewGoal;
        _inlineGoalName = "";
        _inlineGoalDescription = null;
        _inlineGoalDimension = null;
        // Inherit dates and scope from the root mission
        _inlineGoalStartDate = startDate;
        _inlineGoalEndDate = endDate;
        _inlineGoalCollaboratorId = collaboratorId;
        _inlineGoalStatusValue = null;
        _editingInlineGoalIndex = null;
    }

    private void OpenEditInlineGoal(int index)
    {
        var children = CurrentChildren;
        if (index < 0 || index >= children.Count) return;

        var existing = children[index];
        _inlineFormMode = InlineFormMode.EditGoal;
        _editingInlineGoalIndex = index;
        _inlineGoalName = existing.Name;
        _inlineGoalDescription = existing.Description;
        _inlineGoalDimension = existing.Dimension;
        _inlineGoalStartDate = existing.StartDate ?? DateTime.Today;
        _inlineGoalEndDate = existing.EndDate ?? DateTime.Today.AddDays(7);
        _inlineGoalCollaboratorId = existing.CollaboratorId;
        _inlineGoalStatusValue = existing.StatusValue;
    }

    private DateTime GetCurrentParentStartDate()
    {
        if (IsAtRoot) return startDate;
        var goal = ResolveGoalAtPath();
        return goal.StartDate ?? startDate;
    }

    private void HandleInlineGoalSave()
    {
        if (string.IsNullOrWhiteSpace(_inlineGoalName))
        {
            ToastService.ShowError("Erro ao salvar meta", "Informe o nome da meta.");
            return;
        }

        if (Mode == WizardMode.Goal)
        {
            var parentStart = GetCurrentParentStartDate();
            if (_inlineGoalStartDate < parentStart)
            {
                ToastService.ShowError("Erro ao salvar meta",
                    $"A data de início da meta não pode ser anterior à do pai ({parentStart:dd/MM/yyyy}).");
                return;
            }

            if (_inlineGoalEndDate < _inlineGoalStartDate)
            {
                ToastService.ShowError("Erro ao salvar meta",
                    "A data de fim precisa ser igual ou maior que a data de início.");
                return;
            }
        }

        var children = CurrentChildren;
        var isEditing = _inlineFormMode == InlineFormMode.EditGoal && _editingInlineGoalIndex.HasValue;

        // Preserve existing data when editing
        var originalId = isEditing && _editingInlineGoalIndex!.Value < children.Count
            ? children[_editingInlineGoalIndex.Value].OriginalId
            : null;
        var existingTempId = isEditing && _editingInlineGoalIndex!.Value < children.Count
            ? children[_editingInlineGoalIndex.Value].TempId
            : Guid.NewGuid().ToString();
        var existingIndicators = isEditing && _editingInlineGoalIndex!.Value < children.Count
            ? children[_editingInlineGoalIndex.Value].Indicators
            : [];
        var existingChildren = isEditing && _editingInlineGoalIndex!.Value < children.Count
            ? children[_editingInlineGoalIndex.Value].Children
            : [];

        var newGoal = new TempGoal(
            TempId: existingTempId,
            Name: _inlineGoalName.Trim(),
            Description: string.IsNullOrWhiteSpace(_inlineGoalDescription) ? null : _inlineGoalDescription.Trim(),
            OriginalId: originalId,
            Dimension: string.IsNullOrWhiteSpace(_inlineGoalDimension) ? null : _inlineGoalDimension.Trim(),
            StartDate: _inlineGoalStartDate,
            EndDate: _inlineGoalEndDate,
            CollaboratorId: _inlineGoalCollaboratorId,
            StatusValue: _inlineGoalStatusValue)
        {
            Indicators = existingIndicators,
            Children = existingChildren
        };

        if (isEditing && _editingInlineGoalIndex!.Value < children.Count)
        {
            children[_editingInlineGoalIndex.Value] = newGoal;
        }
        else
        {
            children.Add(newGoal);
        }

        CloseInlineForm();
    }

    // ---- Close Inline Form ----

    private void CloseInlineForm()
    {
        _inlineFormMode = InlineFormMode.None;
        _inlineIndicatorModel = new IndicatorFormFields.IndicatorFormModel();
        _editingInlineIndicatorIndex = null;
        _inlineTaskName = "";
        _inlineTaskDescription = null;
        _inlineTaskState = TaskState.ToDo;
        _inlineTaskDueDate = null;
        _editingInlineTaskIndex = null;
        _inlineGoalName = "";
        _inlineGoalDescription = null;
        _inlineGoalDimension = null;
        _inlineGoalStartDate = DateTime.Today;
        _inlineGoalEndDate = DateTime.Today.AddDays(7);
        _inlineGoalCollaboratorId = null;
        _inlineGoalStatusValue = null;
        _editingInlineGoalIndex = null;
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
}
