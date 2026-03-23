using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Components.Common;

public partial class TransferList<TItem>
{
    [Parameter] public List<TItem> AvailableItems { get; set; } = new();
    [Parameter] public List<TItem> AssignedItems { get; set; } = new();
    [Parameter] public EventCallback<List<TItem>> AssignedItemsChanged { get; set; }
    [Parameter] public EventCallback<string> OnAvailableSearch { get; set; }

    [Parameter] public Func<TItem, Guid> GetKey { get; set; } = _ => Guid.Empty;
    [Parameter] public Func<TItem, string> GetDisplayText { get; set; } = item => item?.ToString() ?? "";
    [Parameter] public Func<TItem, string>? GetSecondaryText { get; set; }

    [Parameter] public string AvailableTitle { get; set; } = "Disponíveis";
    [Parameter] public string AssignedTitle { get; set; } = "Atribuídos";
    [Parameter] public string SearchPlaceholder { get; set; } = "Buscar...";
    [Parameter] public string EmptyAvailableMessage { get; set; } = "Nenhum item disponível.";
    [Parameter] public string EmptyAssignedMessage { get; set; } = "Nenhum item atribuído.";

    private string availableSearch = "";
    private string assignedSearch = "";
    private readonly HashSet<Guid> selectedAvailable = new();
    private readonly HashSet<Guid> selectedAssigned = new();

    private List<TItem> filteredAvailableItems => string.IsNullOrWhiteSpace(availableSearch)
        ? AvailableItems
        : AvailableItems.Where(i => GetDisplayText(i).Contains(availableSearch, StringComparison.OrdinalIgnoreCase) ||
            (GetSecondaryText != null && GetSecondaryText(i).Contains(availableSearch, StringComparison.OrdinalIgnoreCase))).ToList();

    private List<TItem> filteredAssignedItems => string.IsNullOrWhiteSpace(assignedSearch)
        ? AssignedItems
        : AssignedItems.Where(i => GetDisplayText(i).Contains(assignedSearch, StringComparison.OrdinalIgnoreCase) ||
            (GetSecondaryText != null && GetSecondaryText(i).Contains(assignedSearch, StringComparison.OrdinalIgnoreCase))).ToList();

    private void ToggleAvailableSelection(TItem item)
    {
        var key = GetKey(item);
        if (!selectedAvailable.Remove(key))
        {
            selectedAvailable.Add(key);
        }
    }

    private void ToggleAssignedSelection(TItem item)
    {
        var key = GetKey(item);
        if (!selectedAssigned.Remove(key))
        {
            selectedAssigned.Add(key);
        }
    }

    private async Task MoveToAssignedAsync()
    {
        var itemsToMove = AvailableItems.Where(i => selectedAvailable.Contains(GetKey(i))).ToList();
        foreach (var item in itemsToMove)
        {
            AvailableItems.Remove(item);
            AssignedItems.Add(item);
        }
        selectedAvailable.Clear();
        await AssignedItemsChanged.InvokeAsync(AssignedItems);
    }

    private async Task MoveToAvailableAsync()
    {
        var itemsToMove = AssignedItems.Where(i => selectedAssigned.Contains(GetKey(i))).ToList();
        foreach (var item in itemsToMove)
        {
            AssignedItems.Remove(item);
            AvailableItems.Add(item);
        }
        selectedAssigned.Clear();
        await AssignedItemsChanged.InvokeAsync(AssignedItems);
    }

    private async Task OnAvailableSearchChangedAsync()
    {
        if (OnAvailableSearch.HasDelegate)
        {
            await OnAvailableSearch.InvokeAsync(availableSearch);
        }
    }
}
