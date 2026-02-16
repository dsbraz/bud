using Bud.Client.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bud.Client.Pages;

public partial class ObjectiveDimensions : IDisposable
{
    [Inject] private ApiClient Api { get; set; } = default!;
    [Inject] private ToastService ToastService { get; set; } = default!;
    [Inject] private OrganizationContext OrgContext { get; set; } = default!;
    [Inject] private UiOperationService UiOps { get; set; } = default!;

    private PagedResult<ObjectiveDimension>? dimensions;
    private string? search;
    private bool showFilterPanel;
    private bool isModalOpen;
    private bool isEditing;
    private Guid? editingDimensionId;
    private string formName = string.Empty;

    private Guid? deletingDimensionId;
    private System.Threading.Timer? deleteConfirmTimer;

    protected override async Task OnInitializedAsync()
    {
        await LoadDimensions();
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
            await LoadDimensions();
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao atualizar dimensões por troca de organização: {ex.Message}");
            ToastService.ShowError("Erro ao atualizar dimensões", "Não foi possível atualizar os dados da organização selecionada.");
        }
    }

    public void Dispose()
    {
        OrgContext.OnOrganizationChanged -= HandleOrganizationChanged;
        deleteConfirmTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task LoadDimensions()
    {
        dimensions = await Api.GetObjectiveDimensionsAsync(search, 1, 50) ?? new PagedResult<ObjectiveDimension>();
    }

    private void ToggleFilterPanel() => showFilterPanel = !showFilterPanel;
    private bool HasActiveFilters() => !string.IsNullOrWhiteSpace(search);

    private async Task ClearFilters()
    {
        search = null;
        await LoadDimensions();
    }

    private async Task OnSearchKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await LoadDimensions();
        }
    }

    private void OpenCreateModal()
    {
        isEditing = false;
        editingDimensionId = null;
        formName = string.Empty;
        isModalOpen = true;
    }

    private void OpenEditModal(ObjectiveDimension dimension)
    {
        isEditing = true;
        editingDimensionId = dimension.Id;
        formName = dimension.Name;
        isModalOpen = true;
    }

    private void CloseModal()
    {
        isModalOpen = false;
        isEditing = false;
        editingDimensionId = null;
        formName = string.Empty;
    }

    private async Task SaveDimension()
    {
        if (string.IsNullOrWhiteSpace(formName))
        {
            ToastService.ShowError("Erro de validação", "Informe o nome da dimensão.");
            return;
        }

        if (isEditing && editingDimensionId.HasValue)
        {
            await UpdateDimension();
            return;
        }

        await CreateDimension();
    }

    private async Task CreateDimension()
    {
        await UiOps.RunAsync(
            async () =>
            {
                await Api.CreateObjectiveDimensionAsync(new CreateObjectiveDimensionRequest
                {
                    Name = formName
                });
                await LoadDimensions();
                ToastService.ShowSuccess("Dimensão criada", "A dimensão foi criada com sucesso.");
                CloseModal();
            },
            "Erro ao criar dimensão",
            "Não foi possível criar a dimensão. Verifique os dados e tente novamente.");
    }

    private async Task UpdateDimension()
    {
        if (!editingDimensionId.HasValue)
        {
            return;
        }

        await UiOps.RunAsync(
            async () =>
            {
                await Api.UpdateObjectiveDimensionAsync(editingDimensionId.Value, new UpdateObjectiveDimensionRequest
                {
                    Name = formName
                });
                await LoadDimensions();
                ToastService.ShowSuccess("Dimensão atualizada", "As alterações foram salvas com sucesso.");
                CloseModal();
            },
            "Erro ao atualizar dimensão",
            "Não foi possível atualizar a dimensão. Verifique os dados e tente novamente.");
    }

    private void HandleDeleteClick(Guid dimensionId)
    {
        if (deletingDimensionId == dimensionId)
        {
            _ = DeleteDimension(dimensionId);
            return;
        }

        deletingDimensionId = dimensionId;
        deleteConfirmTimer?.Dispose();
        deleteConfirmTimer = new System.Threading.Timer(
            _ => InvokeAsync(() =>
            {
                deletingDimensionId = null;
                StateHasChanged();
            }),
            null,
            3000,
            Timeout.Infinite);
    }

    private async Task DeleteDimension(Guid dimensionId)
    {
        try
        {
            await UiOps.RunAsync(
                async () =>
                {
                    await Api.DeleteObjectiveDimensionAsync(dimensionId);
                    await LoadDimensions();
                    ToastService.ShowSuccess("Dimensão excluída", "A dimensão foi removida com sucesso.");
                },
                "Erro ao excluir dimensão",
                "Não foi possível excluir a dimensão. Verifique se ela está em uso.");
        }
        finally
        {
            deletingDimensionId = null;
            deleteConfirmTimer?.Dispose();
            deleteConfirmTimer = null;
        }
    }
}
