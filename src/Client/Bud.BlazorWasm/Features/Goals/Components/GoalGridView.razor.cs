using System.Globalization;
using Bud.BlazorWasm.Api;
using Bud.BlazorWasm.Services;
using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Components;

#pragma warning disable IDE0011, IDE0044

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class GoalGridView
{
    [Inject] private ApiClient Api { get; set; } = default!;

    [Parameter] public PagedResult<GoalResponse>? RootGoals { get; set; }
    [Parameter] public Dictionary<Guid, GoalProgressResponse> RootGoalProgress { get; set; } = new();
    [Parameter] public List<CollaboratorResponse> Collaborators { get; set; } = [];
    [Parameter] public EventCallback<GoalResponse> OnEdit { get; set; }
    [Parameter] public EventCallback<Guid> OnDeleteClick { get; set; }
    [Parameter] public Guid? DeletingGoalId { get; set; }
    [Parameter] public EventCallback<(GoalResponse goal, IndicatorResponse indicator)> OnCheckinClick { get; set; }
    [Parameter] public EventCallback<(GoalResponse goal, IndicatorResponse indicator)> OnHistoryClick { get; set; }

    private Guid? _openRootGoalId;
    private List<GoalResponse> _breadcrumb = [];
    private List<GoalResponse>? _currentGoals;
    private List<IndicatorResponse>? _currentIndicators;
    private List<TaskResponse>? _currentTasks;
    private Dictionary<Guid, GoalProgressResponse> _currentGoalProgress = new();
    private Dictionary<Guid, IndicatorProgressResponse> _currentIndicatorProgress = new();
    private bool _isLoading;
    private bool _isExpanded;

    private IReadOnlyList<GoalResponse> DisplayGoals => _currentGoals ?? [];

    private IReadOnlyList<IndicatorResponse> DisplayIndicators => _currentIndicators ?? [];

    private IReadOnlyList<TaskResponse> DisplayTasks => _currentTasks ?? [];

    private GoalResponse? CurrentParent =>
        _breadcrumb.Count > 0 ? _breadcrumb[^1] : null;

    private async Task OpenGoal(GoalResponse goal)
    {
        _openRootGoalId = goal.Id;
        _breadcrumb = [goal];
        await LoadCurrentLevel(goal.Id);
    }

    private void CloseContainer()
    {
        _openRootGoalId = null;
        _isExpanded = false;
        _breadcrumb.Clear();
        _currentGoals = null;
        _currentIndicators = null;
        _currentTasks = null;
        _currentGoalProgress.Clear();
        _currentIndicatorProgress.Clear();
        StateHasChanged();
    }

    private void ToggleExpand()
    {
        _isExpanded = !_isExpanded;
    }

    private void HandleModalBackdropClick()
    {
        if (_isExpanded)
            ToggleExpand();
        else
            CloseContainer();
    }

    private async Task NavigateInto(GoalResponse goal)
    {
        _breadcrumb.Add(goal);
        await LoadCurrentLevel(goal.Id);
    }

    private async Task NavigateTo(int index)
    {
        _breadcrumb = _breadcrumb.Take(index + 1).ToList();
        await LoadCurrentLevel(_breadcrumb[index].Id);
    }

    private async Task LoadCurrentLevel(Guid goalId)
    {
        _isLoading = true;
        _currentGoals = null;
        _currentIndicators = null;
        _currentTasks = null;
        _currentGoalProgress.Clear();
        _currentIndicatorProgress.Clear();
        StateHasChanged();

        try
        {
            var childrenTask = Api.GetGoalChildrenAsync(goalId);
            var indicatorsTask = Api.GetGoalIndicatorsByGoalIdAsync(goalId);
            var tasksTask = Api.GetTasksAsync(goalId);

            await Task.WhenAll(childrenTask, indicatorsTask, tasksTask);

            var children = childrenTask.Result;
            var indicators = indicatorsTask.Result;

            _currentGoals = children?.Items.ToList() ?? [];
            _currentIndicators = indicators?.Items.ToList() ?? [];
            _currentTasks = tasksTask.Result?.Items.ToList() ?? [];

            var goalIds = _currentGoals.Select(g => g.Id).ToList();
            var indicatorIds = _currentIndicators.Select(i => i.Id).ToList();

            var goalProgressTask = goalIds.Count > 0
                ? Api.GetGoalProgressAsync(goalIds)
                : Task.FromResult<List<GoalProgressResponse>?>(null);
            var indicatorProgressTask = indicatorIds.Count > 0
                ? Api.GetIndicatorProgressAsync(indicatorIds)
                : Task.FromResult<List<IndicatorProgressResponse>?>(null);

            await Task.WhenAll(goalProgressTask, indicatorProgressTask);

            if (goalProgressTask.Result is { } goalProgressList)
            {
                foreach (var p in goalProgressList)
                {
                    _currentGoalProgress[p.GoalId] = p;
                }
            }

            if (indicatorProgressTask.Result is { } indicatorProgressList)
            {
                foreach (var p in indicatorProgressList)
                {
                    _currentIndicatorProgress[p.IndicatorId] = p;
                }
            }
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private CollaboratorResponse? GetCollaborator(Guid id) =>
        Collaborators.FirstOrDefault(c => c.Id == id);

    private static string GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "?";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0][0].ToString().ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static string FormatLastUpdated(DateTime? lastCheckinDate)
    {
        if (lastCheckinDate is null) return "Sem check-ins";
        var days = (int)(DateTime.UtcNow - lastCheckinDate.Value).TotalDays;
        return days switch
        {
            0 => "Atualizado hoje",
            1 => "Atualizado há 1 dia",
            _ => $"Atualizado há {days} dias"
        };
    }

    public async Task ReloadCurrentLevel()
    {
        if (CurrentParent is not null)
        {
            await LoadCurrentLevel(CurrentParent.Id);
        }
    }

    private async Task HandleEditClick(GoalResponse goal)
    {
        await OnEdit.InvokeAsync(goal);
    }

    private async Task HandleDeleteClick(Guid goalId)
    {
        await OnDeleteClick.InvokeAsync(goalId);
    }

    private async Task HandleCheckinClick(IndicatorResponse indicator)
    {
        if (CurrentParent is not null)
        {
            await OnCheckinClick.InvokeAsync((CurrentParent, indicator));
        }
    }

    private async Task HandleHistoryClick(IndicatorResponse indicator)
    {
        if (CurrentParent is not null)
        {
            await OnHistoryClick.InvokeAsync((CurrentParent, indicator));
        }
    }

    private async Task HandleTaskStateChange(TaskResponse task, TaskState newState)
    {
        try
        {
            await Api.UpdateTaskAsync(task.Id, new PatchTaskRequest { State = newState });
            if (_currentTasks is not null)
            {
                var idx = _currentTasks.FindIndex(t => t.Id == task.Id);
                if (idx >= 0)
                {
                    _currentTasks[idx] = new TaskResponse
                    {
                        Id = task.Id,
                        OrganizationId = task.OrganizationId,
                        GoalId = task.GoalId,
                        Name = task.Name,
                        Description = task.Description,
                        State = newState,
                        DueDate = task.DueDate
                    };
                    _currentTasks = [.._currentTasks];
                }
            }
            StateHasChanged();
        }
        catch (Exception)
        {
            // silent failure
        }
    }

    private static string GetGoalProgressPercent(GoalProgressResponse? progress)
    {
        if (progress is null || progress.IndicatorsWithCheckins == 0)
        {
            return "0";
        }

        return ((int)progress.OverallProgress).ToString(CultureInfo.InvariantCulture);
    }

    private static string GetIndicatorProgressPercent(IndicatorProgressResponse? progress)
    {
        if (progress is null || !progress.HasCheckins)
        {
            return "0";
        }

        return ((int)progress.Progress).ToString(CultureInfo.InvariantCulture);
    }

    private static string GetIndicatorStatusLabel(IndicatorProgressResponse? progress)
    {
        if (progress is null || !progress.HasCheckins)
        {
            return "Sem dados";
        }

        var statusClass = GoalProgressDisplayHelper.GetIndicatorProgressStatusClass(progress);
        return statusClass switch
        {
            "on-track" => "Dentro do previsto",
            "at-risk" => "Atenção necessária",
            "off-track" => "Fora do previsto",
            _ => "Sem dados"
        };
    }

    private static int GetGoalItemCount(GoalProgressResponse? progress)
    {
        return progress?.TotalIndicators ?? 0;
    }

    private static bool IsDueDateActiveForTask(TaskResponse t) =>
        t.DueDate.HasValue && t.State != TaskState.Done && t.State != TaskState.Archived;

    private static string GetTaskGridStatusClass(TaskResponse t) => t.State switch
    {
        TaskState.ToDo => "task-todo",
        TaskState.Doing => "task-doing",
        TaskState.Done => "task-done-state",
        TaskState.Archived => "task-archived",
        _ => "task-todo"
    };

    private static string GetTaskGridStatusLabel(TaskResponse t) => t.State switch
    {
        TaskState.ToDo => "A fazer",
        TaskState.Doing => "Fazendo",
        TaskState.Done => "Feito",
        TaskState.Archived => "Arquivado",
        _ => "A fazer"
    };

    private static string GetNextTaskStateLabel(TaskResponse t) => t.State switch
    {
        TaskState.ToDo => "Iniciar",
        TaskState.Doing => "Concluir",
        TaskState.Done => "Reabrir",
        TaskState.Archived => "Reabrir",
        _ => "Avançar"
    };

    private static TaskState GetNextTaskState(TaskResponse t) => t.State switch
    {
        TaskState.ToDo => TaskState.Doing,
        TaskState.Doing => TaskState.Done,
        TaskState.Done => TaskState.ToDo,
        TaskState.Archived => TaskState.ToDo,
        _ => TaskState.ToDo
    };

    private static string GetTaskDueDateRowClass(TaskResponse t)
    {
        if (!IsDueDateActiveForTask(t)) return "";
        var days = (t.DueDate!.Value.Date - DateTime.Today).TotalDays;
        if (days < 0)  return "task-row-overdue";
        if (days <= 7) return "task-row-warning";
        return "";
    }

    private static string GetTaskDueDateChipClass(TaskResponse t)
    {
        if (!IsDueDateActiveForTask(t)) return "";
        var days = (t.DueDate!.Value.Date - DateTime.Today).TotalDays;
        if (days < 0)  return "task-due-overdue";
        if (days <= 7) return "task-due-warning";
        return "";
    }

    private static string FormatGridTaskDueDate(TaskResponse t)
    {
        var days = (t.DueDate!.Value.Date - DateTime.Today).TotalDays;
        if (days < 0)  return $"Venceu em {t.DueDate.Value:dd/MM}";
        if (days == 0) return "Vence hoje";
        return $"Vence em {t.DueDate.Value:dd/MM}";
    }
}
