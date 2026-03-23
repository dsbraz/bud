using Bud.Shared.Contracts.Features.Goals;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class TaskRow
{
    [Parameter, EditorRequired] public TaskResponse Task { get; set; } = default!;
    [Parameter] public bool ShowDeleteButton { get; set; } = true;
    [Parameter] public EventCallback<TaskState> OnStateChange { get; set; }
    [Parameter] public EventCallback<TaskResponse> OnDelete { get; set; }

    private string GetStateCssClass() => Task.State switch
    {
        TaskState.ToDo => "task-todo",
        TaskState.Doing => "task-doing",
        TaskState.Done => "task-done-state",
        TaskState.Archived => "task-archived",
        _ => "task-todo"
    };

    private string GetStateLabel() => Task.State switch
    {
        TaskState.ToDo => "A fazer",
        TaskState.Doing => "Fazendo",
        TaskState.Done => "Feito",
        TaskState.Archived => "Arquivado",
        _ => "A fazer"
    };

    private TaskState GetNextState() => Task.State switch
    {
        TaskState.ToDo => TaskState.Doing,
        TaskState.Doing => TaskState.Done,
        TaskState.Done => TaskState.ToDo,
        TaskState.Archived => TaskState.ToDo,
        _ => TaskState.ToDo
    };

    private async Task OnStateButtonClick()
    {
        await OnStateChange.InvokeAsync(GetNextState());
    }

    private bool IsDueDateActive =>
        Task.DueDate.HasValue &&
        Task.State != TaskState.Done &&
        Task.State != TaskState.Archived;

    private string GetDueDateRowCssClass()
    {
        if (!IsDueDateActive)
        {
            return "";
        }

        var days = (Task.DueDate!.Value.Date - DateTime.Today).TotalDays;
        if (days < 0)
        {
            return "task-row-overdue";
        }

        if (days <= 7)
        {
            return "task-row-warning";
        }

        return "";
    }

    private string GetDueDateChipCssClass()
    {
        if (!IsDueDateActive)
        {
            return "";
        }

        var days = (Task.DueDate!.Value.Date - DateTime.Today).TotalDays;
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

    private string FormatDueDate()
    {
        var days = (Task.DueDate!.Value.Date - DateTime.Today).TotalDays;
        if (days < 0)
        {
            return $"Venceu em {Task.DueDate.Value:dd/MM}";
        }

        if (days == 0)
        {
            return "Vence hoje";
        }

        return $"Vence em {Task.DueDate.Value:dd/MM}";
    }
}
