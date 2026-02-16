using Bud.Client.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

#pragma warning disable IDE0011, CA1805

namespace Bud.Client.Pages;

public partial class Organizations
{
    private CreateOrganizationRequest newOrganization = new();
    private PagedResult<Organization>? organizations;
    private List<LeaderCollaboratorResponse> leaders = new();
    private string? search;
    private bool isSubmitting = false;
    private bool isModalOpen = false;
    private string modalMode = "create"; // "create" ou "edit"
    private Guid? editingOrganizationId = null;
    private UpdateOrganizationRequest editOrganization = new();
    private Guid? deletingOrganizationId = null;
    private System.Threading.Timer? deleteConfirmTimer;
    private const string GlobalAdminOrgName = "getbud.co"; // Organização do admin global

    // Estado do modal de detalhes
    private bool isDetailsModalOpen = false;
    private Organization? selectedOrganization = null;
    private PagedResult<Workspace>? orgWorkspaces = null;
    private PagedResult<Collaborator>? orgCollaborators = null;

    protected override async Task OnInitializedAsync()
    {
        await AuthState.EnsureInitializedAsync();
        await LoadOrganizations();
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
            await LoadOrganizations();
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao atualizar organizações por troca de contexto: {ex.Message}");
            ToastService.ShowError("Erro ao atualizar organizações", "Não foi possível atualizar a lista de organizações.");
        }
    }

    private async Task LoadLeaders(Guid? organizationId = null)
    {
        try
        {
            leaders = await Api.GetLeadersAsync(organizationId) ?? new List<LeaderCollaboratorResponse>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao carregar líderes: {ex.Message}");
            leaders = new List<LeaderCollaboratorResponse>();
        }
    }

    private async Task LoadOrganizations()
    {
        organizations = await Api.GetOrganizationsAsync(search, 1, 20) ?? new PagedResult<Organization>();
    }

    private async Task OpenCreateModal()
    {
        modalMode = "create";
        editingOrganizationId = null;
        newOrganization = new CreateOrganizationRequest();
        // Load leaders based on selected tenant context
        // If a specific org is selected, show only its leaders
        // If "TODOS" is selected (null), show all leaders
        await LoadLeaders(OrgContext.SelectedOrganizationId);
        isModalOpen = true;
    }

    private async Task OpenEditModal(Organization org)
    {
        modalMode = "edit";
        editingOrganizationId = org.Id;
        editOrganization = new UpdateOrganizationRequest
        {
            Name = org.Name,
            OwnerId = org.OwnerId ?? Guid.Empty
        };
        // Load leaders based on selected tenant context
        // If a specific org is selected, show only its leaders
        // If "TODOS" is selected (null), show all leaders
        await LoadLeaders(OrgContext.SelectedOrganizationId);
        isModalOpen = true;
    }

    private bool IsProtectedOrganization()
    {
        if (editingOrganizationId == null) return false;

        var org = organizations?.Items.FirstOrDefault(o => o.Id == editingOrganizationId);
        return org != null &&
               !string.IsNullOrEmpty(org.Name) &&
               org.Name.Equals(GlobalAdminOrgName, StringComparison.OrdinalIgnoreCase);
    }

    private void CloseModal()
    {
        isModalOpen = false;
        modalMode = "create";
        editingOrganizationId = null;
        editOrganization = new UpdateOrganizationRequest();
    }

    private async Task CreateOrganization()
    {
        // Frontend validation
        if (string.IsNullOrWhiteSpace(newOrganization.Name))
        {
            ToastService.ShowError("Erro ao criar organização", "Por favor, informe o domínio da organização.");
            return;
        }

        if (newOrganization.OwnerId == Guid.Empty)
        {
            ToastService.ShowError("Erro ao criar organização", "Por favor, selecione um líder para a organização.");
            return;
        }

        isSubmitting = true;

        try
        {
            await UiOps.RunAsync(
                async () =>
                {
                    await Api.CreateOrganizationAsync(newOrganization);
                    newOrganization = new CreateOrganizationRequest();
                    await LoadOrganizations();
                    await RefreshAvailableOrganizations();

                    ToastService.ShowSuccess("Organização criada com sucesso!", "A organização foi criada e está disponível.");
                    CloseModal();
                },
                "Erro ao criar organização",
                "Não foi possível criar a organização. Verifique os dados e tente novamente.");
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private async Task UpdateOrganization()
    {
        if (editingOrganizationId == null) return;

        if (string.IsNullOrWhiteSpace(editOrganization.Name))
        {
            ToastService.ShowError("Erro ao atualizar organização", "Por favor, informe o domínio da organização.");
            return;
        }

        if (editOrganization.OwnerId == Guid.Empty)
        {
            ToastService.ShowError("Erro ao atualizar organização", "Por favor, selecione um líder para a organização.");
            return;
        }

        isSubmitting = true;

        try
        {
            await UiOps.RunAsync(
                async () =>
                {
                    await Api.UpdateOrganizationAsync(editingOrganizationId.Value, editOrganization);
                    await LoadOrganizations();
                    await RefreshAvailableOrganizations();
                    ToastService.ShowSuccess("Organização atualizada com sucesso!", "As alterações foram salvas.");
                    CloseModal();
                },
                "Erro ao atualizar organização",
                "Não foi possível atualizar a organização. Verifique os dados e tente novamente.");
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private void HandleDeleteClick(Guid organizationId)
    {
        if (deletingOrganizationId == organizationId)
        {
            // Segunda clique - confirma exclusão
            _ = DeleteOrganization(organizationId);
        }
        else
        {
            // Primeiro clique - mostra confirmação
            deletingOrganizationId = organizationId;

            // Reseta após 3 segundos
            deleteConfirmTimer?.Dispose();
            deleteConfirmTimer = new System.Threading.Timer(_ =>
            {
                InvokeAsync(() =>
                {
                    deletingOrganizationId = null;
                    StateHasChanged();
                });
            }, null, 3000, Timeout.Infinite);
        }
    }

    private async Task DeleteOrganization(Guid organizationId)
    {
        deletingOrganizationId = null;
        deleteConfirmTimer?.Dispose();

        var orgName = organizations?.Items.FirstOrDefault(o => o.Id == organizationId)?.Name ?? "organização";

        await UiOps.RunAsync(
            async () =>
            {
                await Api.DeleteOrganizationAsync(organizationId);
                await LoadOrganizations();
                await RefreshAvailableOrganizations();
                ToastService.ShowSuccess("Organização excluída", $"A organização '{orgName}' foi removida com sucesso.");
            },
            "Erro ao excluir organização",
            "Não foi possível excluir a organização. Tente novamente.");
    }

    private async Task OpenDetailsModal(Organization org)
    {
        selectedOrganization = org;
        isDetailsModalOpen = true;
        orgWorkspaces = null;
        orgCollaborators = null;

        // Carregar dados em paralelo para melhor performance
        await Task.WhenAll(
            LoadOrganizationWorkspaces(org.Id),
            LoadOrganizationCollaborators(org.Id)
        );
    }

    private void CloseDetailsModal()
    {
        isDetailsModalOpen = false;
        selectedOrganization = null;
        orgWorkspaces = null;
        orgCollaborators = null;
    }

    private async Task LoadOrganizationWorkspaces(Guid organizationId)
    {
        try
        {
            orgWorkspaces = await Api.GetWorkspacesAsync(organizationId, null, 1, 10);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao carregar workspaces: {ex.Message}");
            ToastService.ShowError("Erro", "Não foi possível carregar os workspaces.");
            orgWorkspaces = new PagedResult<Workspace> { Items = Array.Empty<Workspace>() };
        }
    }

    private async Task LoadOrganizationCollaborators(Guid organizationId)
    {
        try
        {
            orgCollaborators = await Api.GetOrganizationCollaboratorsAsync(organizationId, 1, 10);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao carregar colaboradores: {ex.Message}");
            ToastService.ShowError("Erro", "Não foi possível carregar os colaboradores.");
            orgCollaborators = new PagedResult<Collaborator> { Items = Array.Empty<Collaborator>() };
        }
    }

    private async Task RefreshAvailableOrganizations()
    {
        try
        {
            var organizations = await Api.GetMyOrganizationsAsync();
            if (organizations != null)
            {
                OrgContext.UpdateAvailableOrganizations(organizations);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao atualizar lista de organizações: {ex.Message}");
        }
    }

    public void Dispose()
    {
        deleteConfirmTimer?.Dispose();
        OrgContext.OnOrganizationChanged -= HandleOrganizationChanged;
        GC.SuppressFinalize(this);
    }
}
