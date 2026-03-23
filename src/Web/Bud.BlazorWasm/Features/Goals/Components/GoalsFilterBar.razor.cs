using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class GoalsFilterBar
{
    [Parameter] public GoalFilter Filter { get; set; } = GoalFilter.Mine;
    [Parameter] public bool FilterActiveOnly { get; set; }
    [Parameter] public DateTime? FilterStartDate { get; set; }
    [Parameter] public DateTime? FilterEndDate { get; set; }
    [Parameter] public string? Search { get; set; }

    [Parameter] public EventCallback<GoalFilter> OnSetFilter { get; set; }
    [Parameter] public EventCallback OnToggleActiveFilter { get; set; }
    [Parameter] public EventCallback<(DateTime?, DateTime?)> OnDateFilterApplied { get; set; }
    [Parameter] public EventCallback<string?> OnSearchSubmit { get; set; }
    [Parameter] public EventCallback OnClearFilters { get; set; }

    private bool showDatePicker;
    private bool showScopeDropdown;
    private bool showStatusDropdown;
    private string? localSearch;
    private DateTime? localStartDate;
    private DateTime? localEndDate;
    private string? previousSearch;
    private DateTime? previousStartDate;
    private DateTime? previousEndDate;

    protected override void OnParametersSet()
    {
        if (Search != previousSearch)
        {
            localSearch = Search;
            previousSearch = Search;
        }
        if (FilterStartDate != previousStartDate)
        {
            localStartDate = FilterStartDate;
            previousStartDate = FilterStartDate;
        }
        if (FilterEndDate != previousEndDate)
        {
            localEndDate = FilterEndDate;
            previousEndDate = FilterEndDate;
        }
    }

    private void ToggleDatePicker()
    {
        showDatePicker = !showDatePicker;
        showScopeDropdown = false;
        showStatusDropdown = false;
    }

    private void ToggleScopeDropdown()
    {
        showScopeDropdown = !showScopeDropdown;
        showDatePicker = false;
        showStatusDropdown = false;
    }

    private void ToggleStatusDropdown()
    {
        showStatusDropdown = !showStatusDropdown;
        showDatePicker = false;
        showScopeDropdown = false;
    }

    private async Task SelectScope(GoalFilter filter)
    {
        showScopeDropdown = false;
        await OnSetFilter.InvokeAsync(filter);
    }

    private async Task HandleToggleActive()
    {
        showStatusDropdown = false;
        await OnToggleActiveFilter.InvokeAsync();
    }

    private void OnStartDateChanged(ChangeEventArgs e)
    {
        localStartDate = DateTime.TryParse(e.Value?.ToString(), out var d) ? d : null;
    }

    private void OnEndDateChanged(ChangeEventArgs e)
    {
        localEndDate = DateTime.TryParse(e.Value?.ToString(), out var d) ? d : null;
    }

    private async Task ApplyDateFilter()
    {
        showDatePicker = false;
        await OnDateFilterApplied.InvokeAsync((localStartDate, localEndDate));
    }

    private async Task HandleClearFilters()
    {
        showDatePicker = false;
        showScopeDropdown = false;
        showStatusDropdown = false;
        localSearch = null;
        localStartDate = null;
        localEndDate = null;
        await OnClearFilters.InvokeAsync();
    }

    private string GetScopeLabel() => Filter switch
    {
        GoalFilter.Mine => "Minhas metas",
        GoalFilter.MyTeam => "Metas do time",
        GoalFilter.All => "Todas as metas",
        _ => "Minhas metas"
    };

    private string GetDateRangeLabel()
    {
        if (FilterStartDate.HasValue && FilterEndDate.HasValue)
        {
            return $"{FilterStartDate:dd/MM} - {FilterEndDate:dd/MM/yyyy}";
        }

        if (FilterStartDate.HasValue)
        {
            return $"A partir de {FilterStartDate:dd/MM/yyyy}";
        }

        if (FilterEndDate.HasValue)
        {
            return $"Até {FilterEndDate:dd/MM/yyyy}";
        }

        return "Selecionar período";
    }

    private bool HasActiveFilters() =>
        !string.IsNullOrEmpty(Search)
        || FilterStartDate.HasValue
        || FilterEndDate.HasValue
        || !FilterActiveOnly;
}
