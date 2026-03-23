using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bud.BlazorWasm.Components.Common;

public partial class CollaboratorFormFields
{
    [Parameter] public string FullName { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> FullNameChanged { get; set; }

    [Parameter] public string Email { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> EmailChanged { get; set; }

    [Parameter] public CollaboratorRole Role { get; set; }
    [Parameter] public EventCallback<CollaboratorRole> RoleChanged { get; set; }

    [Parameter] public string? LeaderId { get; set; }
    [Parameter] public EventCallback<string?> LeaderIdChanged { get; set; }

    [Parameter] public List<CollaboratorLeaderResponse> Leaders { get; set; } = new();
    [Parameter] public Guid? ExcludeLeaderId { get; set; }

    [Parameter] public List<CollaboratorTeamResponse> AvailableTeams { get; set; } = new();
    [Parameter] public List<CollaboratorTeamResponse> AssignedTeams { get; set; } = new();
    [Parameter] public EventCallback<List<CollaboratorTeamResponse>> AssignedTeamsChanged { get; set; }
    [Parameter] public EventCallback<string> OnAvailableTeamsSearch { get; set; }

    [Parameter] public bool ShowTeams { get; set; } = true;

    private async Task OnFullNameChanged(string value)
    {
        FullName = value;
        await FullNameChanged.InvokeAsync(value);
    }

    private async Task OnEmailChanged(string value)
    {
        Email = value;
        await EmailChanged.InvokeAsync(value);
    }

    private async Task OnRoleChanged(CollaboratorRole value)
    {
        Role = value;
        await RoleChanged.InvokeAsync(value);
    }

    private async Task OnLeaderChanged(ChangeEventArgs e)
    {
        var value = e.Value?.ToString();
        LeaderId = string.IsNullOrEmpty(value) ? null : value;
        await LeaderIdChanged.InvokeAsync(LeaderId);
    }

    private async Task OnAssignedTeamsChangedAsync(List<CollaboratorTeamResponse> teams)
    {
        AssignedTeams = teams;
        await AssignedTeamsChanged.InvokeAsync(teams);
    }

    private async Task OnAvailableTeamsSearchAsync(string search)
    {
        await OnAvailableTeamsSearch.InvokeAsync(search);
    }
}
