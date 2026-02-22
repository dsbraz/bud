#pragma warning disable IDE0011, IDE0044, CA1822, CA1859, CA1860, CA1868

using Bud.Client.Services;
using Bud.Client.Shared.Missions;
using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bud.Client.Pages;

public partial class MissionTemplates : IDisposable
{
    [Inject] private ApiClient Api { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;
    [Inject] private OrganizationContext OrgContext { get; set; } = null!;
    [Inject] private UiOperationService UiOps { get; set; } = null!;

    // Data
    private PagedResult<MissionTemplate>? _templates;

    // Filter
    private string? _search;
    private bool _showFilterPanel;

    // Wizard
    private bool _isWizardOpen;
    private bool _isEditMode;
    private Guid? _editingTemplateId;
    private MissionWizardModel? _wizardInitialModel;

    // Delete
    private Guid? _deletingTemplateId;
    private System.Threading.Timer? _deleteConfirmTimer;

    protected override async Task OnInitializedAsync()
    {
        await LoadTemplates();
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
            await LoadTemplates();
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao atualizar modelos por troca de organização: {ex.Message}");
            ToastService.ShowError("Erro ao atualizar modelos", "Não foi possível atualizar os dados da organização selecionada.");
        }
    }

    public void Dispose()
    {
        OrgContext.OnOrganizationChanged -= HandleOrganizationChanged;
        _deleteConfirmTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    // ---- Data Loading ----

    private async Task LoadTemplates()
    {
        _templates = await Api.GetMissionTemplatesAsync(_search, 1, 20) ?? new PagedResult<MissionTemplate>();
    }

    // ---- Filter ----

    private void ToggleFilterPanel() => _showFilterPanel = !_showFilterPanel;
    private bool HasActiveFilters() => !string.IsNullOrEmpty(_search);

    private async Task ClearFilters()
    {
        _search = null;
        await LoadTemplates();
    }

    private async Task OnSearchKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await LoadTemplates();
    }

    // ---- Wizard Open / Close ----

    private void OpenCreateModal()
    {
        _isEditMode = false;
        _editingTemplateId = null;
        _wizardInitialModel = new MissionWizardModel();
        _isWizardOpen = true;
    }

    private void OpenEditModal(MissionTemplate template)
    {
        _isEditMode = true;
        _editingTemplateId = template.Id;

        var objectiveIdToTempId = template.Objectives
            .OrderBy(o => o.OrderIndex)
            .ToDictionary(o => o.Id, _ => Guid.NewGuid().ToString());

        var objectives = template.Objectives
            .OrderBy(o => o.OrderIndex)
            .Select(o => new TempObjective(
                objectiveIdToTempId[o.Id],
                o.Name,
                o.Description,
                o.Id,
                o.Dimension))
            .ToList();

        var metrics = template.Metrics
            .OrderBy(m => m.OrderIndex)
            .Select(m => new TempMetric(
                null,
                m.Name,
                m.Type.ToString(),
                BuildMetricDetails(m),
                m.QuantitativeType?.ToString(),
                m.MinValue,
                m.MaxValue,
                m.TargetText,
                m.Unit?.ToString(),
                m.MissionTemplateObjectiveId.HasValue && objectiveIdToTempId.TryGetValue(m.MissionTemplateObjectiveId.Value, out var tid)
                    ? tid
                    : null))
            .ToList();

        _wizardInitialModel = new MissionWizardModel
        {
            Name = template.Name,
            Description = template.Description,
            Metrics = metrics,
            Objectives = objectives
        };

        _isWizardOpen = true;
    }

    private void CloseWizard()
    {
        _isWizardOpen = false;
        _isEditMode = false;
        _editingTemplateId = null;
        _wizardInitialModel = null;
    }

    // ---- Wizard Save Handler ----

    private async Task HandleWizardSave(MissionWizardResult result)
    {
        if (_isEditMode && _editingTemplateId.HasValue)
        {
            await UpdateTemplate(result);
        }
        else
        {
            await CreateTemplate(result);
        }
    }

    private async Task CreateTemplate(MissionWizardResult result)
    {
        var request = BuildCreateRequest(result);

        await UiOps.RunAsync(
            async () =>
            {
                await Api.CreateMissionTemplateAsync(request);
                await LoadTemplates();
                ToastService.ShowSuccess("Modelo criado com sucesso!", $"O modelo '{result.Name}' foi criado.");
                CloseWizard();
            },
            "Erro ao criar modelo",
            "Não foi possível criar o modelo. Verifique os dados e tente novamente.");
    }

    private async Task UpdateTemplate(MissionWizardResult result)
    {
        if (!_editingTemplateId.HasValue) return;

        var request = BuildUpdateRequest(result);

        await UiOps.RunAsync(
            async () =>
            {
                await Api.UpdateMissionTemplateAsync(_editingTemplateId.Value, request);
                await LoadTemplates();
                ToastService.ShowSuccess("Modelo atualizado", "As alterações foram salvas com sucesso.");
                CloseWizard();
            },
            "Erro ao atualizar modelo",
            "Não foi possível atualizar o modelo. Verifique os dados e tente novamente.");
    }

    // ---- Payload Builders ----

    private CreateMissionTemplateRequest BuildCreateRequest(MissionWizardResult result)
    {
        var (objectives, metrics) = BuildTemplatePayload(result);
        return new CreateMissionTemplateRequest
        {
            Name = result.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(result.Description) ? null : result.Description.Trim(),
            Objectives = objectives,
            Metrics = metrics
        };
    }

    private PatchMissionTemplateRequest BuildUpdateRequest(MissionWizardResult result)
    {
        var (objectives, metrics) = BuildTemplatePayload(result);
        return new PatchMissionTemplateRequest
        {
            Name = result.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(result.Description) ? null : result.Description.Trim(),
            Objectives = objectives,
            Metrics = metrics
        };
    }

    private static (List<MissionTemplateObjectiveDto> Objectives, List<MissionTemplateMetricDto> Metrics) BuildTemplatePayload(MissionWizardResult result)
    {
        var objectiveIdByTempId = new Dictionary<string, Guid>();

        var objectives = result.Objectives
            .Select((objective, index) =>
            {
                var objectiveId = objective.OriginalId ?? Guid.NewGuid();
                objectiveIdByTempId[objective.TempId] = objectiveId;

                return new MissionTemplateObjectiveDto
                {
                    Id = objectiveId,
                    Name = objective.Name,
                    Description = objective.Description,
                    Dimension = objective.Dimension,
                    OrderIndex = index
                };
            })
            .ToList();

        var metrics = result.Metrics
            .Select((metric, index) => new MissionTemplateMetricDto
            {
                Name = metric.Name,
                Type = Enum.Parse<MetricType>(metric.Type),
                OrderIndex = index,
                QuantitativeType = ParseOptionalEnum<QuantitativeMetricType>(metric.QuantitativeType),
                MinValue = metric.MinValue,
                MaxValue = metric.MaxValue,
                Unit = ParseOptionalEnum<MetricUnit>(metric.Unit),
                TargetText = metric.TargetText,
                MissionTemplateObjectiveId = metric.ObjectiveTempId is not null && objectiveIdByTempId.TryGetValue(metric.ObjectiveTempId, out var objectiveId)
                    ? objectiveId
                    : null
            })
            .ToList();

        return (objectives, metrics);
    }

    // ---- Delete ----

    private void HandleDeleteClick(Guid templateId)
    {
        if (_deletingTemplateId == templateId)
        {
            _ = DeleteTemplate(templateId);
        }
        else
        {
            _deletingTemplateId = templateId;
            _deleteConfirmTimer?.Dispose();
            _deleteConfirmTimer = new System.Threading.Timer(
                _ => InvokeAsync(() =>
                {
                    _deletingTemplateId = null;
                    StateHasChanged();
                }),
                null,
                3000,
                Timeout.Infinite);
        }
    }

    private async Task DeleteTemplate(Guid templateId)
    {
        try
        {
            await UiOps.RunAsync(
                async () =>
                {
                    await Api.DeleteMissionTemplateAsync(templateId);
                    ToastService.ShowSuccess("Modelo excluído", "O modelo foi removido com sucesso.");
                    await LoadTemplates();
                },
                "Erro ao excluir",
                "Não foi possível excluir o modelo. Tente novamente.");
        }
        finally
        {
            _deletingTemplateId = null;
            _deleteConfirmTimer?.Dispose();
            _deleteConfirmTimer = null;
        }
    }

    // ---- Helpers ----

    private static TEnum? ParseOptionalEnum<TEnum>(string? value)
        where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(value, out var parsed) ? parsed : null;

    private static string BuildMetricDetails(MissionTemplateMetric metric)
    {
        return metric.Type == MetricType.Quantitative
            ? BuildQuantitativeDetails(metric.QuantitativeType?.ToString(), metric.MinValue, metric.MaxValue, metric.Unit?.ToString())
            : metric.TargetText ?? string.Empty;
    }

    private static string BuildQuantitativeDetails(string? quantitativeType, decimal? minValue, decimal? maxValue, string? unit)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(quantitativeType))
            parts.Add(quantitativeType);
        if (!string.IsNullOrWhiteSpace(unit))
            parts.Add(unit);
        if (minValue.HasValue)
            parts.Add($"Min: {minValue}");
        if (maxValue.HasValue)
            parts.Add($"Max: {maxValue}");
        return string.Join(" — ", parts);
    }
}
