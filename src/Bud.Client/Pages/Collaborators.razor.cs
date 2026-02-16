using Bud.Client.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Rendering;

#pragma warning disable IDE0011, CA1805

namespace Bud.Client.Pages;

public partial class Collaborators
{
    private CreateCollaboratorRequest newCollaborator = new();
    private List<Team> teams = new();
    private List<Workspace> workspaces = new();
    private List<LeaderCollaboratorResponse> leaders = new();
    private List<Collaborator> allCollaborators = new();
    private PagedResult<Collaborator>? collaborators;
    private string? search;
    private string? selectedTeamId;
    private string? createLeaderId;
    private bool isModalOpen = false;

    // Filter panel state
    private bool showFilterPanel = false;
    private bool showTeamDropdown = false;

    // Estado de equipes no modal de criação
    private List<TeamSummaryDto> availableTeamsForCreate = new();
    private List<TeamSummaryDto> assignedTeamsForCreate = new();
    private bool createTeamsModified = false;

    // Estado do modal de edição
    private bool isEditModalOpen = false;
    private Collaborator? selectedCollaborator = null;
    private UpdateCollaboratorRequest editCollaborator = new();
    private string? editLeaderId;
    private List<TeamSummaryDto> availableTeamsForEdit = new();
    private List<TeamSummaryDto> assignedTeamsForEdit = new();
    private bool teamsModified = false;

    // Estado do modal de detalhes
    private bool isDetailsModalOpen = false;
    private Collaborator? detailsCollaborator = null;
    private List<TeamSummaryDto>? detailsCollaboratorTeams = null;
    private List<CollaboratorHierarchyNodeDto>? detailsSubordinates = null;

    // Estado de confirmação de exclusão
    private Guid? deletingCollaboratorId = null;
    private System.Threading.Timer? deleteConfirmTimer;

    protected override async Task OnInitializedAsync()
    {
        await LoadTeams();
        await LoadWorkspaces();
        await LoadLeaders();
        await LoadCollaborators();
        await LoadAllCollaborators();
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
            await LoadTeams();
            await LoadWorkspaces();
            await LoadLeaders();
            await LoadCollaborators();
            await LoadAllCollaborators();
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao atualizar colaboradores por troca de organização: {ex.Message}");
            ToastService.ShowError("Erro ao atualizar colaboradores", "Não foi possível atualizar os dados da organização selecionada.");
        }
    }

    public void Dispose()
    {
        OrgContext.OnOrganizationChanged -= HandleOrganizationChanged;
        deleteConfirmTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task LoadTeams()
    {
        var result = await Api.GetTeamsAsync(null, null, null, 1, 100);
        teams = result?.Items.ToList() ?? new List<Team>();
    }

    private async Task LoadWorkspaces()
    {
        var result = await Api.GetWorkspacesAsync(OrgContext.SelectedOrganizationId, null, 1, 100);
        workspaces = result?.Items.ToList() ?? new List<Workspace>();
    }

    private async Task LoadLeaders()
    {
        leaders = await Api.GetLeadersAsync(OrgContext.SelectedOrganizationId) ?? new List<LeaderCollaboratorResponse>();
    }

    private async Task LoadAllCollaborators()
    {
        var result = await Api.GetCollaboratorsAsync(null, null, 1, 100);
        allCollaborators = result?.Items.ToList() ?? new List<Collaborator>();
    }

    private async Task LoadCollaborators()
    {
        var filterTeamId = Guid.TryParse(selectedTeamId, out var parsedTeamId)
            ? parsedTeamId
            : (Guid?)null;
        collaborators = await Api.GetCollaboratorsAsync(filterTeamId, search, 1, 20) ?? new PagedResult<Collaborator>();
    }

    // Filter methods
    private void ToggleFilterPanel()
    {
        showFilterPanel = !showFilterPanel;
    }

    private void ToggleTeamDropdown()
    {
        showTeamDropdown = !showTeamDropdown;
    }

    private async Task ApplyTeamFilter()
    {
        showTeamDropdown = false;
        await LoadCollaborators();
    }

    private bool HasActiveFilters()
    {
        return !string.IsNullOrEmpty(selectedTeamId) || !string.IsNullOrEmpty(search);
    }

    private async Task ClearFilters()
    {
        selectedTeamId = null;
        search = null;
        await LoadCollaborators();
    }

    private string GetTeamFilterLabel()
    {
        if (Guid.TryParse(selectedTeamId, out var teamId))
        {
            var team = teams.FirstOrDefault(t => t.Id == teamId);
            if (team != null)
            {
                return team.Name;
            }
        }
        return "Todas as equipes";
    }

    private async Task OnSearchKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await LoadCollaborators();
        }
    }

    // Summary card methods
    private int GetTotalCollaboratorsCount()
    {
        return allCollaborators.Count;
    }

    private int GetLeadersCount()
    {
        return allCollaborators.Count(c => c.Role == CollaboratorRole.Leader);
    }

    private int GetContributorsCount()
    {
        return allCollaborators.Count(c => c.Role == CollaboratorRole.IndividualContributor);
    }

    private void OpenCreateModal()
    {
        newCollaborator = new CreateCollaboratorRequest();
        createLeaderId = null;
        assignedTeamsForCreate = new();
        createTeamsModified = false;
        availableTeamsForCreate = teams.Select(t => new TeamSummaryDto
        {
            Id = t.Id,
            Name = t.Name,
            WorkspaceName = workspaces.FirstOrDefault(w => w.Id == t.WorkspaceId)?.Name ?? ""
        }).ToList();
        isModalOpen = true;
    }

    private void CloseModal()
    {
        isModalOpen = false;
        availableTeamsForCreate = new();
        assignedTeamsForCreate = new();
        createTeamsModified = false;
    }

    private async Task CreateCollaborator()
    {
        if (string.IsNullOrWhiteSpace(newCollaborator.FullName))
        {
            ToastService.ShowError("Erro ao criar colaborador", "Informe o nome completo.");
            return;
        }

        if (string.IsNullOrWhiteSpace(newCollaborator.Email))
        {
            ToastService.ShowError("Erro ao criar colaborador", "Informe o e-mail.");
            return;
        }

        // TeamId sempre null (colaboradores são criados sem equipe)
        newCollaborator.TeamId = null;
        newCollaborator.LeaderId = Guid.TryParse(createLeaderId, out var leaderId) ? leaderId : null;

        await UiOps.RunAsync(
            async () =>
            {
                var created = await Api.CreateCollaboratorAsync(newCollaborator);
                var createdCollaboratorName = newCollaborator.FullName;

                // Assign teams if any were selected
                if (created != null && createTeamsModified && assignedTeamsForCreate.Count > 0)
                {
                    await Api.UpdateCollaboratorTeamsAsync(created.Id, new UpdateCollaboratorTeamsRequest
                    {
                        TeamIds = assignedTeamsForCreate.Select(t => t.Id).ToList()
                    });
                }

                newCollaborator = new CreateCollaboratorRequest();
                createLeaderId = null;
                await LoadCollaborators();
                await LoadAllCollaborators();

                ToastService.ShowSuccess("Colaborador criado com sucesso!", $"O colaborador '{createdCollaboratorName}' foi criado.");
                CloseModal();
            },
            "Erro ao criar colaborador",
            "Não foi possível criar o colaborador. Verifique os dados e tente novamente.");
    }

    private async Task OpenEditModal(Collaborator collaborator)
    {
        selectedCollaborator = collaborator;
        editCollaborator = new UpdateCollaboratorRequest
        {
            FullName = collaborator.FullName,
            Email = collaborator.Email,
            Role = collaborator.Role,
            LeaderId = collaborator.LeaderId
        };
        editLeaderId = collaborator.LeaderId?.ToString() ?? "";

        // Load teams for TransferList
        assignedTeamsForEdit = await Api.GetCollaboratorTeamsAsync(collaborator.Id) ?? new();
        availableTeamsForEdit = await Api.GetAvailableTeamsForCollaboratorAsync(collaborator.Id) ?? new();
        teamsModified = false;

        isEditModalOpen = true;
    }

    private void CloseEditModal()
    {
        isEditModalOpen = false;
        selectedCollaborator = null;
        editCollaborator = new();
        editLeaderId = null;
        availableTeamsForEdit = new();
        assignedTeamsForEdit = new();
        teamsModified = false;
    }

    private async Task OpenDetailsModal(Collaborator collaborator)
    {
        detailsCollaborator = collaborator;
        detailsCollaboratorTeams = null;
        detailsSubordinates = null;
        isDetailsModalOpen = true;

        // Carregar equipes e liderados em paralelo
        var teamsTask = Api.GetCollaboratorTeamsAsync(collaborator.Id);
        var subordinatesTask = Api.GetCollaboratorSubordinatesAsync(collaborator.Id);
        detailsCollaboratorTeams = await teamsTask ?? new();
        detailsSubordinates = await subordinatesTask ?? new();
        StateHasChanged();
    }

    private void CloseDetailsModal()
    {
        isDetailsModalOpen = false;
        detailsCollaborator = null;
        detailsCollaboratorTeams = null;
        detailsSubordinates = null;
    }

    private async Task UpdateCollaborator()
    {
        if (selectedCollaborator == null) return;

        editCollaborator.LeaderId = Guid.TryParse(editLeaderId, out var leaderId) ? leaderId : null;

        await UiOps.RunAsync(
            async () =>
            {
                var result = await Api.UpdateCollaboratorAsync(selectedCollaborator.Id, editCollaborator);
                if (result != null)
                {
                    // Update teams if modified
                    if (teamsModified)
                    {
                        await Api.UpdateCollaboratorTeamsAsync(selectedCollaborator.Id, new UpdateCollaboratorTeamsRequest
                        {
                            TeamIds = assignedTeamsForEdit.Select(t => t.Id).ToList()
                        });
                    }

                    ToastService.ShowSuccess("Colaborador atualizado", "As alterações foram salvas com sucesso.");
                    CloseEditModal();
                    await LoadCollaborators();
                    await LoadAllCollaborators();
                }
            },
            "Erro ao atualizar",
            "Não foi possível atualizar o colaborador. Verifique os dados e tente novamente.");
    }

    private async Task HandleDeleteClick(Guid collaboratorId)
    {
        if (deletingCollaboratorId == collaboratorId)
        {
            // Segundo clique - executar exclusão
            await DeleteCollaborator(collaboratorId);
        }
        else
        {
            // Primeiro clique - mostrar confirmação
            deletingCollaboratorId = collaboratorId;

            // Reset após 3 segundos
            deleteConfirmTimer?.Dispose();
            deleteConfirmTimer = new System.Threading.Timer(
                _ => InvokeAsync(() =>
                {
                    deletingCollaboratorId = null;
                    StateHasChanged();
                }),
                null,
                3000,
                Timeout.Infinite
            );
        }
    }

    private async Task DeleteCollaborator(Guid collaboratorId)
    {
        try
        {
            await UiOps.RunAsync(
                async () =>
                {
                    await Api.DeleteCollaboratorAsync(collaboratorId);
                    ToastService.ShowSuccess("Colaborador excluído", "O colaborador foi removido com sucesso.");
                    await LoadCollaborators();
                    await LoadAllCollaborators();
                },
                "Erro ao excluir",
                "Não foi possível excluir o colaborador. Tente novamente.");
        }
        finally
        {
            deletingCollaboratorId = null;
            deleteConfirmTimer?.Dispose();
            deleteConfirmTimer = null;
        }
    }

    private static string GetRoleLabel(CollaboratorRole role) => role switch
    {
        CollaboratorRole.Leader => "Lider",
        _ => "Contribuidor individual"
    };

    private void OnTeamsChanged(List<TeamSummaryDto> teams)
    {
        assignedTeamsForEdit = teams;
        teamsModified = true;
    }

    private async Task SearchAvailableTeamsAsync(string search)
    {
        if (selectedCollaborator == null) return;
        availableTeamsForEdit = await Api.GetAvailableTeamsForCollaboratorAsync(selectedCollaborator.Id, search) ?? new();
    }

    private void OnCreateTeamsChanged(List<TeamSummaryDto> updatedTeams)
    {
        assignedTeamsForCreate = updatedTeams;
        createTeamsModified = true;
    }

    private Task SearchAvailableTeamsForCreateAsync(string search)
    {
        var assignedIds = assignedTeamsForCreate.Select(t => t.Id).ToHashSet();
        availableTeamsForCreate = teams
            .Where(t => !assignedIds.Contains(t.Id))
            .Where(t => string.IsNullOrWhiteSpace(search) ||
                t.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Select(t => new TeamSummaryDto
            {
                Id = t.Id,
                Name = t.Name,
                WorkspaceName = workspaces.FirstOrDefault(w => w.Id == t.WorkspaceId)?.Name ?? ""
            }).ToList();
        return Task.CompletedTask;
    }

    private static RenderFragment RenderOrgChartNode(CollaboratorHierarchyNodeDto node) => builder =>
    {
        var seq = 0;

        builder.OpenElement(seq++, "li");
        builder.AddAttribute(seq++, "class", "org-chart-node");

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "org-chart-card");

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "org-chart-avatar");
        builder.AddContent(seq++, node.Initials);
        builder.CloseElement();

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "org-chart-info");

        builder.OpenElement(seq++, "span");
        builder.AddAttribute(seq++, "class", "org-chart-name");
        builder.AddContent(seq++, node.FullName);
        builder.CloseElement();

        builder.OpenElement(seq++, "span");
        builder.AddAttribute(seq++, "class", "org-chart-role");
        builder.AddContent(seq++, node.Role);
        builder.CloseElement();

        builder.CloseElement();
        builder.CloseElement();

        if (node.Children.Count > 0)
        {
            builder.OpenElement(seq++, "ul");
            builder.AddAttribute(seq++, "class", "org-chart-list");

            foreach (var child in node.Children)
            {
                builder.AddContent(seq++, RenderOrgChartNode(child));
            }

            builder.CloseElement();
        }

        builder.CloseElement();
    };
}
