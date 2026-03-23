using Bud.BlazorWasm.Api;
using Bud.BlazorWasm.State;
using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Features.Dashboard.Pages;

public partial class Dashboard : IDisposable
{
    [Inject] private ApiClient Api { get; set; } = default!;
    [Inject] private AuthState AuthState { get; set; } = default!;
    [Inject] private OrganizationContext OrgContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private MyDashboardResponse? dashboard;
    private List<CollaboratorTeamResponse>? userTeams;
    private Guid? selectedTeamId;

    protected override async Task OnInitializedAsync()
    {
        OrgContext.OnOrganizationChanged += OnOrganizationChanged;

        // Aguardar inicialização do OrgContext (MainLayout pode ainda estar carregando)
        var maxWait = 50; // 50 * 100ms = 5 segundos máximo
        while (!OrgContext.IsInitialized && maxWait > 0)
        {
            await Task.Delay(100);
            maxWait--;
        }

        await AuthState.EnsureInitializedAsync();
        await LoadUserTeams();
        await LoadDashboard();
    }

    private async Task LoadUserTeams()
    {
        if (AuthState.SessionResponse?.CollaboratorId is { } collaboratorId)
        {
            userTeams = await Api.GetCollaboratorTeamsAsync(collaboratorId);
        }
    }

    private async Task LoadDashboard()
    {
        dashboard = await Api.GetMyDashboardAsync(selectedTeamId) ?? new MyDashboardResponse();
    }

    private async Task OnTeamSelectionChanged(ChangeEventArgs e)
    {
        selectedTeamId = Guid.TryParse(e.Value?.ToString(), out var id) ? id : null;
        await LoadDashboard();
    }

    private void OnOrganizationChanged()
    {
        _ = InvokeAsync(async () =>
        {
            selectedTeamId = null;
            await LoadUserTeams();
            await LoadDashboard();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        OrgContext.OnOrganizationChanged -= OnOrganizationChanged;
        GC.SuppressFinalize(this);
    }
}
