using Bud.BlazorWasm.Api;
using Bud.Shared.Contracts.Goals;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class GoalChildSection
{
    [Inject] private ApiClient Api { get; set; } = default!;

    [Parameter, EditorRequired] public GoalResponse ChildGoal { get; set; } = default!;
    [Parameter] public GoalProgressResponse? GoalProgressResponse { get; set; }
    [Parameter] public bool IsExpanded { get; set; }
    [Parameter] public EventCallback OnToggleExpand { get; set; }
    [Parameter] public EventCallback<IndicatorResponse> OnCheckinClick { get; set; }
    [Parameter] public EventCallback<IndicatorResponse> OnHistoryClick { get; set; }
    [Parameter] public EventCallback<GoalResponse> OnExpandModal { get; set; }
    [Parameter] public int Depth { get; set; }

    private List<GoalResponse>? _children;
    private List<IndicatorResponse>? _indicators;
    private List<TaskResponse>? _tasks;
    private Dictionary<Guid, GoalProgressResponse> _childProgressCache = new();
    private Dictionary<Guid, IndicatorProgressResponse> _indicatorProgressCache = new();
    private readonly HashSet<Guid> _expandedChildren = new();
    private bool _isLoading;
    private bool _dataLoaded;

    private int ProgressPercent => (int)(GoalProgressResponse?.OverallProgress ?? 0);
    private decimal ExpectedProgress => GoalProgressResponse?.ExpectedProgress ?? 0m;

    protected override async Task OnParametersSetAsync()
    {
        if (IsExpanded && !_dataLoaded && !_isLoading)
        {
            await LoadDataAsync();
        }
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        StateHasChanged();
        try
        {
            var childrenTask = Api.GetGoalChildrenAsync(ChildGoal.Id);
            var indicatorsTask = Api.GetGoalIndicatorsByGoalIdAsync(ChildGoal.Id);
            var tasksTask = Api.GetTasksAsync(ChildGoal.Id);
            await Task.WhenAll(childrenTask, indicatorsTask, tasksTask);

            _children = childrenTask.Result?.Items.ToList() ?? [];
            _indicators = indicatorsTask.Result?.Items.ToList() ?? [];
            _tasks = tasksTask.Result?.Items.ToList() ?? [];

            if (_children.Count > 0)
            {
                var progressList = await Api.GetGoalProgressAsync(_children.Select(c => c.Id).ToList());
                _childProgressCache = progressList?.ToDictionary(p => p.GoalId) ?? new();
            }

            if (_indicators.Count > 0)
            {
                var indicatorProgress = await Api.GetIndicatorProgressAsync(_indicators.Select(i => i.Id).ToList());
                _indicatorProgressCache = indicatorProgress?.ToDictionary(p => p.IndicatorId) ?? new();
            }

            _dataLoaded = true;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private void ToggleChild(Guid childId)
    {
        if (!_expandedChildren.Remove(childId))
        {
            _expandedChildren.Add(childId);
        }
    }

    private async Task HandleTaskStateChange(TaskResponse task, TaskState newState)
    {
        try
        {
            await Api.UpdateTaskAsync(task.Id, new PatchTaskRequest { State = newState });
            if (_tasks is not null)
            {
                var idx = _tasks.FindIndex(t => t.Id == task.Id);
                if (idx >= 0)
                {
                    _tasks[idx] = new TaskResponse
                    {
                        Id = task.Id,
                        OrganizationId = task.OrganizationId,
                        GoalId = task.GoalId,
                        Name = task.Name,
                        Description = task.Description,
                        State = newState,
                        DueDate = task.DueDate
                    };
                    _tasks = [.._tasks];
                }
            }
        }
        catch (Exception)
        {
            // silent failure; task will show stale state until next expand
        }
    }

    private string GetProgressStatusClass()
    {
        if (ProgressPercent >= (int)ExpectedProgress)
        {
            return "on-track";
        }

        if (ProgressPercent >= (int)(ExpectedProgress * 0.7m))
        {
            return "at-risk";
        }

        return "off-track";
    }
}
