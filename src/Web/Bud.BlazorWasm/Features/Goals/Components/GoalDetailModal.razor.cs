using Bud.Shared.Contracts.Features.Goals;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class GoalDetailModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public GoalResponse? Goal { get; set; }
    [Parameter] public GoalProgressResponse? Progress { get; set; }
    [Parameter] public string? ParentName { get; set; }
    [Parameter] public List<IndicatorResponse>? Indicators { get; set; }
    [Parameter] public List<TaskResponse>? Tasks { get; set; }
    [Parameter] public List<GoalResponse>? ChildGoals { get; set; }
    [Parameter] public Dictionary<Guid, IndicatorProgressResponse> IndicatorProgressCache { get; set; } = new();
    [Parameter] public Dictionary<Guid, GoalProgressResponse> GoalProgressCache { get; set; } = new();
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback<IndicatorResponse> OnCheckinClick { get; set; }
    [Parameter] public EventCallback<IndicatorResponse> OnHistoryClick { get; set; }
    [Parameter] public EventCallback<(TaskResponse task, TaskState newState)> OnTaskStateChange { get; set; }

    private readonly HashSet<Guid> _expandedGoals = new();

    private void ToggleGoalExpand(Guid goalId)
    {
        if (!_expandedGoals.Remove(goalId))
        {
            _expandedGoals.Add(goalId);
        }
    }

    private static async Task HandleChildExpandModal(GoalResponse childGoal)
    {
        // For now, just log or ignore - the parent Goals.razor will handle opening a new modal for the child
        // This could be enhanced to navigate the breadcrumb
        await Task.CompletedTask;
    }
}
