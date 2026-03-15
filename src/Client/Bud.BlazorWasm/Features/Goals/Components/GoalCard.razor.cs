using Bud.Shared.Contracts.Features.Goals;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class GoalCard
{
    [Parameter, EditorRequired] public GoalResponse Goal { get; set; } = default!;
    [Parameter] public GoalProgressResponse? Progress { get; set; }
    [Parameter] public bool IsExpanded { get; set; }
    [Parameter] public bool IsDeleting { get; set; }
    [Parameter] public List<IndicatorResponse>? Indicators { get; set; }
    [Parameter] public List<TaskResponse>? Tasks { get; set; }
    [Parameter] public List<GoalResponse>? ChildGoals { get; set; }
    [Parameter] public Dictionary<Guid, IndicatorProgressResponse> IndicatorProgressCache { get; set; } = new();
    [Parameter] public Dictionary<Guid, GoalProgressResponse> GoalProgressCache { get; set; } = new();
    [Parameter] public HashSet<Guid> ExpandedGoals { get; set; } = new();
    [Parameter] public EventCallback OnToggleExpand { get; set; }
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback<GoalResponse> OnExpandModal { get; set; }
    [Parameter] public EventCallback<(TaskResponse task, TaskState newState)> OnTaskStateChange { get; set; }
    [Parameter] public EventCallback OnDeleteClick { get; set; }
    [Parameter] public EventCallback<Guid> OnToggleGoalExpand { get; set; }
    [Parameter] public EventCallback<IndicatorResponse> OnCheckinClick { get; set; }
    [Parameter] public EventCallback<IndicatorResponse> OnHistoryClick { get; set; }
}
