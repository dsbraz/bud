using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bud.BlazorWasm.Components.Common;

public partial class TeamCollaboratorSelector
{
    [Parameter] public List<CollaboratorLeaderResponse> Leaders { get; set; } = new();
    [Parameter] public string LeaderId { get; set; } = "";
    [Parameter] public EventCallback<string> LeaderIdChanged { get; set; }

    [Parameter] public List<CollaboratorLookupResponse> AvailableCollaborators { get; set; } = new();
    [Parameter] public List<CollaboratorLookupResponse> AssignedCollaborators { get; set; } = new();
    [Parameter] public EventCallback<List<CollaboratorLookupResponse>> AssignedCollaboratorsChanged { get; set; }
    [Parameter] public EventCallback<string> OnAvailableSearch { get; set; }

    private async Task OnLeaderChanged(ChangeEventArgs e)
    {
        var value = e.Value?.ToString() ?? "";
        await LeaderIdChanged.InvokeAsync(value);
    }

    private async Task OnAssignedChanged(List<CollaboratorLookupResponse> collaborators)
    {
        await AssignedCollaboratorsChanged.InvokeAsync(collaborators);
    }
}
