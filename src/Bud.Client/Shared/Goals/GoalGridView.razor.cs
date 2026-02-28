using System.Globalization;
using Bud.Client.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Components;

#pragma warning disable IDE0011, IDE0044

namespace Bud.Client.Shared.Goals;

public partial class GoalGridView
{
    [Inject] private ApiClient Api { get; set; } = default!;

    [Parameter] public PagedResult<GoalResponse>? RootGoals { get; set; }
    [Parameter] public Dictionary<Guid, GoalProgressResponse> RootGoalProgress { get; set; } = new();
    [Parameter] public EventCallback<GoalResponse> OnEdit { get; set; }
    [Parameter] public EventCallback<Guid> OnDeleteClick { get; set; }
    [Parameter] public Guid? DeletingGoalId { get; set; }
    [Parameter] public EventCallback<(GoalResponse goal, IndicatorResponse indicator)> OnCheckinClick { get; set; }
    [Parameter] public EventCallback<(GoalResponse goal, IndicatorResponse indicator)> OnHistoryClick { get; set; }
    [Parameter] public EventCallback<(GoalResponse goal, IndicatorResponse indicator)> OnEditIndicator { get; set; }

    private Guid? _openRootGoalId;
    private List<GoalResponse> _breadcrumb = [];
    private List<GoalResponse>? _currentGoals;
    private List<IndicatorResponse>? _currentIndicators;
    private Dictionary<Guid, GoalProgressResponse> _currentGoalProgress = new();
    private Dictionary<Guid, IndicatorProgressResponse> _currentIndicatorProgress = new();
    private bool _isLoading;
    private bool _isExpanded;

    private IReadOnlyList<GoalResponse> DisplayGoals => _currentGoals ?? [];

    private IReadOnlyList<IndicatorResponse> DisplayIndicators => _currentIndicators ?? [];

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
        _currentGoalProgress.Clear();
        _currentIndicatorProgress.Clear();
        StateHasChanged();
    }

    private void ToggleExpand()
    {
        _isExpanded = !_isExpanded;
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
        _currentGoalProgress.Clear();
        _currentIndicatorProgress.Clear();
        StateHasChanged();

        try
        {
            var childrenTask = Api.GetGoalChildrenAsync(goalId);
            var indicatorsTask = Api.GetGoalIndicatorsByGoalIdAsync(goalId);

            await Task.WhenAll(childrenTask, indicatorsTask);

            var children = childrenTask.Result;
            var indicators = indicatorsTask.Result;

            _currentGoals = children?.Items.ToList() ?? [];
            _currentIndicators = indicators?.Items.ToList() ?? [];

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

    private async Task HandleEditIndicatorClick(IndicatorResponse indicator)
    {
        if (CurrentParent is not null)
        {
            await OnEditIndicator.InvokeAsync((CurrentParent, indicator));
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
}
