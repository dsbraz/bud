using Bud.BlazorWasm.Api;
using Bud.BlazorWasm.Services;
using Bud.BlazorWasm.State;
using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bud.BlazorWasm.Features.People.Workspaces.Pages;

public partial class Workspaces : IDisposable
{
    [Inject] private ApiClient Api { get; set; } = default!;
    [Inject] private ToastService ToastService { get; set; } = default!;
    [Inject] private OrganizationContext OrgContext { get; set; } = default!;
    [Inject] private UiOperationService UiOps { get; set; } = default!;

    private CreateWorkspaceRequest newWorkspace = new();
    private List<OrganizationResponse> organizations = new();
    private List<TeamResponse> teams = new();
    private PagedResult<WorkspaceResponse>? workspaces;
    private string? search;
    private bool isModalOpen;
    private bool showFilterPanel;

    // Estado do modal de edição
    private bool isEditModalOpen;
    private WorkspaceResponse? selectedWorkspace;
    private WorkspaceEditModel editWorkspace = new();

    // Estado do modal de detalhes
    private bool isDetailsModalOpen;
    private WorkspaceResponse? detailsWorkspace;

    // Estado de confirmação de exclusão
    private Guid? deletingWorkspaceId;
    private System.Threading.Timer? deleteConfirmTimer;

    protected override async Task OnInitializedAsync()
    {
        await LoadOrganizations();
        await LoadWorkspaces();
        await LoadTeams();
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
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao atualizar espaços de trabalho por troca de organização: {ex.Message}");
            ToastService.ShowError("Erro ao atualizar espaços de trabalho", "Não foi possível atualizar os dados da organização selecionada.");
        }
    }

    public void Dispose()
    {
        OrgContext.OnOrganizationChanged -= HandleOrganizationChanged;
        deleteConfirmTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task LoadOrganizations()
    {
        var result = await Api.GetOrganizationsAsync(null, 1, 100);
        organizations = result?.Items.ToList() ?? new List<OrganizationResponse>();
    }

    private async Task LoadWorkspaces()
    {
        workspaces = await Api.GetWorkspacesAsync(OrgContext.SelectedOrganizationId, search, 1, 20) ?? new PagedResult<WorkspaceResponse>();
    }

    private async Task LoadTeams()
    {
        var result = await Api.GetTeamsAsync(null, null, null, 1, 100);
        teams = result?.Items.ToList() ?? new List<TeamResponse>();
    }

    // Filter methods
    private void ToggleFilterPanel() => showFilterPanel = !showFilterPanel;

    private bool HasActiveFilters() => !string.IsNullOrEmpty(search);

    private async Task ClearFilters()
    {
        search = null;
        await LoadWorkspaces();
    }

    private async Task OnSearchKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await LoadWorkspaces();
        }
    }

    // Summary methods
    private int GetTotalWorkspacesCount() => workspaces?.Total ?? 0;
    private int GetTotalTeamsCount() => teams.Count;

    private void OpenDetailsModal(WorkspaceResponse workspace)
    {
        detailsWorkspace = workspace;
        isDetailsModalOpen = true;
    }

    private void CloseDetailsModal()
    {
        isDetailsModalOpen = false;
        detailsWorkspace = null;
    }

    private void OpenCreateModal()
    {
        newWorkspace = new CreateWorkspaceRequest();
        isModalOpen = true;
    }

    private void CloseModal()
    {
        isModalOpen = false;
    }

    private async Task CreateWorkspace()
    {
        if (!OrgContext.SelectedOrganizationId.HasValue)
        {
            ToastService.ShowError("Erro ao criar espaço de trabalho", "Selecione uma organização específica no menu lateral. Não é possível criar espaços de trabalho com 'TODOS' selecionado.");
            return;
        }

        if (string.IsNullOrWhiteSpace(newWorkspace.Name))
        {
            ToastService.ShowError("Erro ao criar espaço de trabalho", "Informe o nome do espaço de trabalho.");
            return;
        }

        newWorkspace.OrganizationId = OrgContext.SelectedOrganizationId.Value;

        await UiOps.RunAsync(
            async () =>
            {
                await Api.CreateWorkspaceAsync(newWorkspace);
                var workspaceName = newWorkspace.Name;
                newWorkspace = new CreateWorkspaceRequest();
                await LoadWorkspaces();

                ToastService.ShowSuccess("Espaço de trabalho criado com sucesso!", $"O espaço de trabalho '{workspaceName}' foi criado.");
                CloseModal();
            },
            "Erro ao criar espaço de trabalho",
            "Não foi possível criar o espaço de trabalho. Verifique os dados e tente novamente.");
    }

    private void OpenEditModal(WorkspaceResponse workspace)
    {
        selectedWorkspace = workspace;
        editWorkspace = new WorkspaceEditModel
        {
            Name = workspace.Name
        };
        isEditModalOpen = true;
    }

    private void CloseEditModal()
    {
        isEditModalOpen = false;
        selectedWorkspace = null;
        editWorkspace = new WorkspaceEditModel();
    }

    private async Task UpdateWorkspace()
    {
        if (selectedWorkspace == null)
        {
            return;
        }

        await UiOps.RunAsync(
            async () =>
            {
                var request = new PatchWorkspaceRequest
                {
                    Name = editWorkspace.Name
                };
                var result = await Api.UpdateWorkspaceAsync(selectedWorkspace.Id, request);
                if (result != null)
                {
                    ToastService.ShowSuccess("Espaço de trabalho atualizado", "As alterações foram salvas com sucesso.");
                    CloseEditModal();
                    await LoadWorkspaces();
                }
            },
            "Erro ao atualizar",
            "Não foi possível atualizar o espaço de trabalho. Verifique os dados e tente novamente.");
    }

    private void HandleDeleteClick(Guid workspaceId)
    {
        if (deletingWorkspaceId == workspaceId)
        {
            // Segundo clique - executar exclusão
            _ = DeleteWorkspace(workspaceId);
        }
        else
        {
            // Primeiro clique - mostrar confirmação
            deletingWorkspaceId = workspaceId;

            // Reset após 3 segundos
            deleteConfirmTimer?.Dispose();
            deleteConfirmTimer = new System.Threading.Timer(
                _ => InvokeAsync(() =>
                {
                    deletingWorkspaceId = null;
                    StateHasChanged();
                }),
                null,
                3000,
                Timeout.Infinite
            );
        }
    }

    private async Task DeleteWorkspace(Guid workspaceId)
    {
        try
        {
            await UiOps.RunAsync(
                async () =>
                {
                    await Api.DeleteWorkspaceAsync(workspaceId);
                    ToastService.ShowSuccess("Espaço de trabalho excluído", "O espaço de trabalho foi removido com sucesso.");
                    await LoadWorkspaces();
                },
                "Erro ao excluir",
                "Não foi possível excluir o espaço de trabalho. Tente novamente.");
        }
        finally
        {
            deletingWorkspaceId = null;
            deleteConfirmTimer?.Dispose();
            deleteConfirmTimer = null;
        }
    }

    private sealed class WorkspaceEditModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
