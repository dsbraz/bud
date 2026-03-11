using Bud.Shared.Contracts.Goals;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class GoalFormTree
{
    [Parameter] public List<TempIndicator> Indicators { get; set; } = [];
    [Parameter] public List<TempTask> Tasks { get; set; } = [];
    [Parameter] public List<TempGoal> Children { get; set; } = [];
    [Parameter] public bool Expandable { get; set; }
    [Parameter] public EventCallback<int> OnEditIndicator { get; set; }
    [Parameter] public EventCallback<int> OnDeleteIndicator { get; set; }
    [Parameter] public EventCallback<int> OnEditTask { get; set; }
    [Parameter] public EventCallback<int> OnDeleteTask { get; set; }
    [Parameter] public EventCallback<int> OnNavigateInto { get; set; }
    [Parameter] public EventCallback<int> OnEditGoal { get; set; }
    [Parameter] public EventCallback<int> OnDeleteGoal { get; set; }
    [Parameter] public EventCallback<string> OnAddItemToChild { get; set; }
    [Parameter] public EventCallback OnAddItem { get; set; }
    [Parameter] public RenderFragment<string>? AddItemFormTemplate { get; set; }

    // Child-level edit/delete callbacks (tuple: parentChildIndex, itemIndex)
    [Parameter] public EventCallback<(int, int)> OnEditChildIndicator { get; set; }
    [Parameter] public EventCallback<(int, int)> OnDeleteChildIndicator { get; set; }
    [Parameter] public EventCallback<(int, int)> OnEditChildTask { get; set; }
    [Parameter] public EventCallback<(int, int)> OnDeleteChildTask { get; set; }
    [Parameter] public EventCallback<(int, int)> OnEditChildGoal { get; set; }
    [Parameter] public EventCallback<(int, int)> OnDeleteChildGoal { get; set; }

    private readonly HashSet<int> _expandedChildren = [];
    private string? _confirmingDeleteKey;

    private bool _hasExpandedChild => _expandedChildren.Count > 0;

    private void HandleChildClick(int childIndex)
    {
        if (Expandable)
        {
            if (!_expandedChildren.Add(childIndex))
            {
                _expandedChildren.Remove(childIndex);
            }
        }
        else
        {
            OnNavigateInto.InvokeAsync(childIndex);
        }
    }

    private void HandleDeleteClick(string key, Func<Task> deleteAction)
    {
        if (_confirmingDeleteKey == key)
        {
            _confirmingDeleteKey = null;
            deleteAction();
        }
        else
        {
            _confirmingDeleteKey = key;
        }
    }
}
