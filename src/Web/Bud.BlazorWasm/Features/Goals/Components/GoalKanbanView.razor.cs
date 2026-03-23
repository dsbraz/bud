using Bud.Shared.Contracts;
using Bud.Shared.Contracts.Features.Goals;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class GoalKanbanView
{
    [Parameter, EditorRequired] public List<TaskResponse> Tasks { get; set; } = new();
    [Parameter, EditorRequired] public List<GoalResponse> Goals { get; set; } = new();
    [Parameter] public bool ShowArchived { get; set; }
    [Parameter] public EventCallback<(TaskResponse task, TaskState newState)> OnTaskStateChange { get; set; }

    private TaskResponse? _draggingTask;
    private TaskState? _dropTargetColumn;

    private sealed record ColumnDef(TaskState State, string Label, string CssClass);

    private static readonly ColumnDef[] _columns =
    [
        new(TaskState.ToDo,     "A fazer",   "kanban-col-todo"),
        new(TaskState.Doing,    "Fazendo",   "kanban-col-doing"),
        new(TaskState.Done,     "Feito",     "kanban-col-done"),
        new(TaskState.Archived, "Arquivado", "kanban-col-archived"),
    ];

    private void HandleDragStart(TaskResponse task)
    {
        _draggingTask = task;
        _dropTargetColumn = null;
    }

    private void HandleDragEnd()
    {
        _draggingTask = null;
        _dropTargetColumn = null;
    }

    private void HandleDragEnter(TaskState state)
    {
        _dropTargetColumn = state;
    }

    private async Task HandleDrop(TaskState targetState)
    {
        var task = _draggingTask;
        _draggingTask = null;
        _dropTargetColumn = null;

        if (task is null || task.State == targetState)
        {
            return;
        }

        await OnTaskStateChange.InvokeAsync((task, targetState));
    }

    private static string GetDueDateClass(TaskResponse task)
    {
        var days = (task.DueDate!.Value.Date - DateTime.Today).TotalDays;
        if (days < 0)
        {
            return "task-due-overdue";
        }

        if (days <= 7)
        {
            return "task-due-warning";
        }

        return "";
    }

    private static string FormatDueDate(TaskResponse task)
    {
        var days = (task.DueDate!.Value.Date - DateTime.Today).TotalDays;
        if (days < 0)
        {
            return $"Venceu em {task.DueDate.Value:dd/MM}";
        }

        if (days == 0)
        {
            return "Vence hoje";
        }

        return $"Vence em {task.DueDate.Value:dd/MM}";
    }
}
