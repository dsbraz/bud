using Bud.Client.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

#pragma warning disable IDE0011, CA1805

namespace Bud.Client.Pages;

public partial class Teams
{
    private CreateTeamRequest newTeam = new();
    private List<Workspace> workspaces = new();
    private List<Team> parentTeams = new();
    private List<Team> allTeams = new();
    private List<Collaborator> collaborators = new();
    private PagedResult<Team>? teams;
    private string? search;
    private string? selectedWorkspaceId;
    private string? createWorkspaceId;
    private string? createParentTeamId;
    private bool isModalOpen = false;
    private bool showFilterPanel = false;
    private bool showWorkspaceDropdown = false;

    // Estado do modal de criação — líder + colaboradores
    private List<LeaderCollaboratorResponse> createLeaders = new();
    private string createLeaderId = "";
    private List<CollaboratorSummaryDto> availableCollaboratorsForCreate = new();
    private List<CollaboratorSummaryDto> assignedCollaboratorsForCreate = new();

    // Estado do modal de edição
    private bool isEditModalOpen = false;
    private Team? selectedTeam = null;
    private UpdateTeamRequest editTeam = new();
    private string? editParentTeamId;
    private List<Team> editParentTeams = new();
    private List<LeaderCollaboratorResponse> editLeaders = new();
    private string editLeaderId = "";
    private List<CollaboratorSummaryDto> availableCollaboratorsForEdit = new();
    private List<CollaboratorSummaryDto> assignedCollaboratorsForEdit = new();
    private bool collaboratorsModified = false;

    // Estado do modal de detalhes
    private bool isDetailsModalOpen = false;
    private Team? detailsTeam = null;
    private List<CollaboratorSummaryDto>? detailsTeamCollaborators = null;

    // Estado de confirmação de exclusão
    private Guid? deletingTeamId = null;
    private System.Threading.Timer? deleteConfirmTimer;

    protected override async Task OnInitializedAsync()
    {
        await LoadWorkspaces();
        await LoadTeams();
        await LoadCollaborators();
        OrgContext.OnOrganizationChanged += HandleOrganizationChanged;
    }

    private void HandleOrganizationChanged()
    {
        _ = HandleOrganizationChangedAsync();
    }

    private async Task HandleOrganizationChangedAsync()
    {
        try
        {
            await LoadWorkspaces();
            await LoadTeams();
            await LoadCollaborators();
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao atualizar equipes por troca de organização: {ex.Message}");
            ToastService.ShowError("Erro ao atualizar equipes", "Não foi possível atualizar os dados da organização selecionada.");
        }
    }

    public void Dispose()
    {
        OrgContext.OnOrganizationChanged -= HandleOrganizationChanged;
        deleteConfirmTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task LoadWorkspaces()
    {
        var result = await Api.GetWorkspacesAsync(OrgContext.SelectedOrganizationId, null, 1, 100);
        workspaces = result?.Items.ToList() ?? new List<Workspace>();
    }

    private async Task LoadTeams()
    {
        var filterWorkspaceId = Guid.TryParse(selectedWorkspaceId, out var parsedWorkspaceId)
            ? parsedWorkspaceId
            : (Guid?)null;
        teams = await Api.GetTeamsAsync(filterWorkspaceId, null, search, 1, 20) ?? new PagedResult<Team>();

        var allTeamsResult = await Api.GetTeamsAsync(null, null, null, 1, 100);
        allTeams = allTeamsResult?.Items.ToList() ?? new List<Team>();
    }

    private async Task LoadCollaborators()
    {
        var result = await Api.GetCollaboratorsAsync(null, null, 1, 100);
        collaborators = result?.Items.ToList() ?? new List<Collaborator>();
    }

    // Filter methods
    private void ToggleFilterPanel() => showFilterPanel = !showFilterPanel;
    private void ToggleWorkspaceDropdown() => showWorkspaceDropdown = !showWorkspaceDropdown;

    private async Task ApplyWorkspaceFilter()
    {
        showWorkspaceDropdown = false;
        await LoadTeams();
    }

    private bool HasActiveFilters() => !string.IsNullOrEmpty(search) || !string.IsNullOrEmpty(selectedWorkspaceId);

    private async Task ClearFilters()
    {
        search = null;
        selectedWorkspaceId = null;
        await LoadTeams();
    }

    private async Task OnSearchKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await LoadTeams();
    }

    private string GetWorkspaceFilterLabel()
    {
        if (string.IsNullOrEmpty(selectedWorkspaceId))
            return "Todos os espaços de trabalho";

        var workspace = workspaces.FirstOrDefault(w => w.Id.ToString() == selectedWorkspaceId);
        return workspace?.Name ?? "Todos os espaços de trabalho";
    }

    // Summary methods
    private int GetTotalTeamsCount() => teams?.Total ?? 0;
    private int GetRootTeamsCount() => allTeams.Count(t => t.ParentTeamId == null);
    private int GetSubTeamsCount() => allTeams.Count(t => t.ParentTeamId != null);

    private async Task OpenDetailsModal(Team team)
    {
        detailsTeam = team;
        detailsTeamCollaborators = null;
        isDetailsModalOpen = true;

        // Carregar colaboradores da equipe via API
        detailsTeamCollaborators = await Api.GetTeamCollaboratorSummariesAsync(team.Id) ?? new();
        StateHasChanged();
    }

    private void CloseDetailsModal()
    {
        isDetailsModalOpen = false;
        detailsTeam = null;
        detailsTeamCollaborators = null;
    }

    private async Task OnCreateWorkspaceChanged(ChangeEventArgs e)
    {
        createWorkspaceId = e.Value?.ToString();
        createParentTeamId = null;
        parentTeams.Clear();

        if (Guid.TryParse(createWorkspaceId, out var workspaceId))
        {
            var result = await Api.GetTeamsAsync(workspaceId, null, null, 1, 100);
            parentTeams = result?.Items.ToList() ?? new List<Team>();
        }
    }

    private async Task OpenCreateModal()
    {
        newTeam = new CreateTeamRequest();
        createWorkspaceId = null;
        createParentTeamId = null;
        createLeaderId = "";
        parentTeams.Clear();
        assignedCollaboratorsForCreate = new();

        createLeaders = await Api.GetLeadersAsync() ?? new();
        availableCollaboratorsForCreate = await Api.GetCollaboratorSummariesAsync() ?? new();

        isModalOpen = true;
    }

    private void CloseModal()
    {
        isModalOpen = false;
    }

    private async Task CreateTeam()
    {
        if (!Guid.TryParse(createWorkspaceId, out var workspaceId))
        {
            ToastService.ShowError("Erro ao criar equipe", "Selecione um espaço de trabalho.");
            return;
        }

        if (string.IsNullOrWhiteSpace(newTeam.Name))
        {
            ToastService.ShowError("Erro ao criar equipe", "Informe o nome da equipe.");
            return;
        }

        if (!Guid.TryParse(createLeaderId, out var leaderId))
        {
            ToastService.ShowError("Erro ao criar equipe", "Selecione um líder para a equipe.");
            return;
        }

        newTeam.WorkspaceId = workspaceId;
        newTeam.LeaderId = leaderId;
        newTeam.ParentTeamId = Guid.TryParse(createParentTeamId, out var parentId) ? parentId : null;

        if (!assignedCollaboratorsForCreate.Any(c => c.Id == leaderId))
        {
            ToastService.ShowError("Erro ao criar equipe", "O líder da equipe deve estar incluído na lista de membros.");
            return;
        }

        await UiOps.RunAsync(
            async () =>
            {
                var createdTeam = await Api.CreateTeamAsync(newTeam);
                var createdTeamName = newTeam.Name;

                if (createdTeam != null && assignedCollaboratorsForCreate.Count > 0)
                {
                    await Api.UpdateTeamCollaboratorsAsync(createdTeam.Id, new UpdateTeamCollaboratorsRequest
                    {
                        CollaboratorIds = assignedCollaboratorsForCreate.Select(c => c.Id).ToList()
                    });
                }

                newTeam = new CreateTeamRequest();
                createWorkspaceId = null;
                createParentTeamId = null;
                createLeaderId = "";
                parentTeams.Clear();
                assignedCollaboratorsForCreate = new();
                await LoadTeams();

                ToastService.ShowSuccess("Equipe criada com sucesso!", $"A equipe '{createdTeamName}' foi criada.");
                CloseModal();
            },
            "Erro ao criar equipe",
            "Não foi possível criar a equipe. Verifique os dados e tente novamente.");
    }

    private async Task OpenEditModal(Team team)
    {
        selectedTeam = team;
        editTeam = new UpdateTeamRequest
        {
            Name = team.Name,
            ParentTeamId = team.ParentTeamId
        };
        editParentTeamId = team.ParentTeamId?.ToString() ?? "";
        editLeaderId = team.LeaderId.ToString();

        // Carregar equipes do mesmo workspace para seleção de equipe pai
        var result = await Api.GetTeamsAsync(team.WorkspaceId, null, null, 1, 100);
        editParentTeams = result?.Items.ToList() ?? new List<Team>();

        // Load leaders and collaborators
        editLeaders = await Api.GetLeadersAsync() ?? new();
        assignedCollaboratorsForEdit = await Api.GetTeamCollaboratorSummariesAsync(team.Id) ?? new();
        availableCollaboratorsForEdit = await Api.GetAvailableCollaboratorsForTeamAsync(team.Id) ?? new();
        collaboratorsModified = false;

        isEditModalOpen = true;
    }

    private void CloseEditModal()
    {
        isEditModalOpen = false;
        selectedTeam = null;
        editTeam = new();
        editParentTeamId = null;
        editLeaderId = "";
        editParentTeams.Clear();
        editLeaders = new();
        availableCollaboratorsForEdit = new();
        assignedCollaboratorsForEdit = new();
        collaboratorsModified = false;
    }

    private async Task UpdateTeam()
    {
        if (selectedTeam == null) return;

        if (!Guid.TryParse(editLeaderId, out var leaderId))
        {
            ToastService.ShowError("Erro ao atualizar", "Selecione um líder para a equipe.");
            return;
        }

        editTeam.ParentTeamId = Guid.TryParse(editParentTeamId, out var parentId) ? parentId : null;
        editTeam.LeaderId = leaderId;

        if (collaboratorsModified && !assignedCollaboratorsForEdit.Any(c => c.Id == leaderId))
        {
            ToastService.ShowError("Erro ao atualizar", "O líder da equipe deve estar incluído na lista de membros.");
            return;
        }

        await UiOps.RunAsync(
            async () =>
            {
                var result = await Api.UpdateTeamAsync(selectedTeam.Id, editTeam);
                if (result != null)
                {
                    if (collaboratorsModified)
                    {
                        await Api.UpdateTeamCollaboratorsAsync(selectedTeam.Id, new UpdateTeamCollaboratorsRequest
                        {
                            CollaboratorIds = assignedCollaboratorsForEdit.Select(c => c.Id).ToList()
                        });
                    }

                    ToastService.ShowSuccess("Equipe atualizada", "As alterações foram salvas com sucesso.");
                    CloseEditModal();
                    await LoadTeams();
                    await LoadCollaborators();
                }
            },
            "Erro ao atualizar",
            "Não foi possível atualizar a equipe. Verifique os dados e tente novamente.");
    }

    private void OnCreateCollaboratorsChanged(List<CollaboratorSummaryDto> collaborators)
    {
        assignedCollaboratorsForCreate = collaborators;
    }

    private async Task SearchAvailableCollaboratorsForCreateAsync(string search)
    {
        availableCollaboratorsForCreate = await Api.GetCollaboratorSummariesAsync(search) ?? new();
    }

    private void OnCollaboratorsChanged(List<CollaboratorSummaryDto> collaborators)
    {
        assignedCollaboratorsForEdit = collaborators;
        collaboratorsModified = true;
    }

    private async Task SearchAvailableCollaboratorsAsync(string search)
    {
        if (selectedTeam == null) return;
        availableCollaboratorsForEdit = await Api.GetAvailableCollaboratorsForTeamAsync(selectedTeam.Id, search) ?? new();
    }

    private void HandleDeleteClick(Guid teamId)
    {
        if (deletingTeamId == teamId)
        {
            // Segundo clique - executar exclusão
            _ = DeleteTeam(teamId);
        }
        else
        {
            // Primeiro clique - mostrar confirmação
            deletingTeamId = teamId;

            // Reset após 3 segundos
            deleteConfirmTimer?.Dispose();
            deleteConfirmTimer = new System.Threading.Timer(
                _ => InvokeAsync(() =>
                {
                    deletingTeamId = null;
                    StateHasChanged();
                }),
                null,
                3000,
                Timeout.Infinite
            );
        }
    }

    private async Task DeleteTeam(Guid teamId)
    {
        try
        {
            await UiOps.RunAsync(
                async () =>
                {
                    await Api.DeleteTeamAsync(teamId);
                    ToastService.ShowSuccess("Equipe excluída", "A equipe foi removida com sucesso.");
                    await LoadTeams();
                },
                "Erro ao excluir",
                "Não foi possível excluir a equipe. Tente novamente.");
        }
        finally
        {
            deletingTeamId = null;
            deleteConfirmTimer?.Dispose();
            deleteConfirmTimer = null;
        }
    }
}
