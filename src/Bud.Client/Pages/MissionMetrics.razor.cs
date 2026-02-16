using Bud.Client.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.AspNetCore.Components;

#pragma warning disable IDE0011, CA1805

namespace Bud.Client.Pages;

public partial class MissionMetrics
{
    private CreateMissionMetricRequest newMetric = new();
    private PagedResult<MissionMetric>? metrics;
    private Dictionary<Guid, MetricProgressDto> metricProgress = new();
    private List<Mission> missions = new();
    private string? createMissionId;
    private string? createMetricTypeValue;
    private string? createQuantitativeTypeValue;
    private string? createMetricUnitValue;
    private string? filterMissionId;
    private string? search;
    private bool isModalOpen;

    // Edit modal state
    private bool isEditModalOpen;
    private MissionMetric? selectedMetric;
    private UpdateMissionMetricRequest editMetric = new();
    private string? editMetricTypeValue;
    private string? editQuantitativeTypeValue;
    private string? editMetricUnitValue;

    // Delete confirmation
    private Guid? deletingMetricId;
    private System.Threading.Timer? deleteMetricConfirmTimer;

    protected override async Task OnInitializedAsync()
    {
        await LoadMissions();
        await LoadMetrics();
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
            await InvokeAsync(async () =>
            {
                filterMissionId = null;
                await LoadMissions();
                await LoadMetrics();
                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao atualizar métricas por troca de organização: {ex.Message}");
            ToastService.ShowError("Erro ao atualizar métricas", "Não foi possível atualizar os dados da organização selecionada.");
        }
    }

    public void Dispose()
    {
        OrgContext.OnOrganizationChanged -= HandleOrganizationChanged;
        deleteMetricConfirmTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task LoadMissions()
    {
        try
        {
            var result = await Api.GetMissionsAsync(null, null, null, 1, 100);
            missions = result?.Items.ToList() ?? new List<Mission>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao carregar missões para métricas: {ex.Message}");
            missions = new List<Mission>();
            ToastService.ShowError("Erro ao carregar missões", "Não foi possível carregar as missões.");
        }
    }

    private async Task LoadMetrics()
    {
        try
        {
            var missionId = Guid.TryParse(filterMissionId, out var parsedMissionId)
                ? parsedMissionId
                : (Guid?)null;

            metrics = await Api.GetMissionMetricsAsync(missionId, search, 1, 20) ?? new PagedResult<MissionMetric>();
            await LoadMetricProgress();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao carregar métricas: {ex.Message}");
            metrics = new PagedResult<MissionMetric>();
            metricProgress = new();
            ToastService.ShowError("Erro ao carregar métricas", "Não foi possível carregar as métricas.");
        }
    }

    private async Task LoadMetricProgress()
    {
        if (metrics is null || metrics.Items.Count == 0)
        {
            metricProgress = new();
            return;
        }

        try
        {
            var ids = metrics.Items.Select(m => m.Id).ToList();
            var progressList = await Api.GetMetricProgressAsync(ids);
            metricProgress = progressList?.ToDictionary(p => p.MetricId) ?? new();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao carregar progresso das métricas: {ex.Message}");
            metricProgress = new();
        }
    }

    private async Task OnMetricTypeChanged(ChangeEventArgs e)
    {
        createMetricTypeValue = e.Value?.ToString();
        ResetCreateMetricInputFields();
        await InvokeAsync(StateHasChanged);
    }

    private void OpenCreateModal()
    {
        ResetCreateMetricForm();
        isModalOpen = true;
    }

    private void CloseModal()
    {
        isModalOpen = false;
    }

    private async Task CreateMetric()
    {
        const string errorTitle = "Erro ao criar métrica";

        if (!TryPrepareMetricForCreate(errorTitle))
        {
            return;
        }

        await UiOps.RunAsync(
            async () =>
            {
                await Api.CreateMissionMetricAsync(newMetric);
                var createdMetricName = newMetric.Name;
                ResetCreateMetricForm();
                await LoadMetrics();

                ToastService.ShowSuccess("Métrica criada com sucesso!", $"A métrica '{createdMetricName}' foi criada.");
                CloseModal();
            },
            errorTitle,
            "Não foi possível criar a métrica. Verifique os dados e tente novamente.");
    }

    private bool TryPrepareMetricForCreate(string errorTitle)
    {
        if (!Guid.TryParse(createMissionId, out var missionId))
        {
            ToastService.ShowError(errorTitle, "Selecione uma missão.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(newMetric.Name))
        {
            ToastService.ShowError(errorTitle, "Informe o nome da métrica.");
            return false;
        }

        if (!TryPrepareMetricForUpsert(
                createMetricTypeValue,
                createQuantitativeTypeValue,
                createMetricUnitValue,
                newMetric.MinValue,
                newMetric.MaxValue,
                newMetric.TargetText,
                errorTitle,
                "Selecione o tipo da metrica.",
                out var metricType,
                out var quantitativeType,
                out var unit))
        {
            return false;
        }

        newMetric.MissionId = missionId;
        newMetric.Type = metricType;
        newMetric.QuantitativeType = quantitativeType;
        newMetric.Unit = unit;
        return true;
    }

    private bool TryParseMetricType(
        string? metricTypeValue,
        string errorTitle,
        string errorMessage,
        out MetricType metricType)
    {
        if (!Enum.TryParse(metricTypeValue, out metricType))
        {
            ToastService.ShowError(errorTitle, errorMessage);
            return false;
        }

        return true;
    }

    private void ResetCreateMetricForm()
    {
        newMetric = new CreateMissionMetricRequest();
        createMissionId = null;
        createMetricTypeValue = null;
        ResetCreateMetricInputFields();
    }

    private void ResetCreateMetricInputFields()
    {
        newMetric.TargetText = null;
        newMetric.MinValue = null;
        newMetric.MaxValue = null;
        newMetric.QuantitativeType = null;
        createQuantitativeTypeValue = null;
        createMetricUnitValue = null;
    }

    private string GetMissionName(Guid missionId)
    {
        return missions.FirstOrDefault(m => m.Id == missionId)?.Name ?? "—";
    }

    private static string GetMetricTypeLabel(MetricType type) => MissionMetricDisplayHelper.GetMetricTypeLabel(type);

    private static string GetQuantitativeTypeLabel(QuantitativeMetricType type) => MissionMetricDisplayHelper.GetQuantitativeTypeLabel(type);

    private static string GetQuantitativeTypeIcon(QuantitativeMetricType type) => MissionMetricDisplayHelper.GetQuantitativeTypeIcon(type);

    private static string GetUnitLabel(MetricUnit unit) => MissionMetricDisplayHelper.GetUnitLabel(unit);

    private static string GetTargetLabel(MissionMetric metric) => MissionMetricDisplayHelper.GetTargetLabel(metric);

    private static string GetMaxValuePlaceholderForMetrics(string? quantitativeType) => quantitativeType switch
    {
        nameof(QuantitativeMetricType.KeepBelow) => "Valor máximo",
        nameof(QuantitativeMetricType.KeepBetween) => "Valor máximo",
        nameof(QuantitativeMetricType.Achieve) => "Valor alvo",
        nameof(QuantitativeMetricType.Reduce) => "Valor alvo",
        _ => "Valor máximo"
    };

    // ---- Edit Methods ----

    private void OpenEditMetricModal(MissionMetric metric)
    {
        selectedMetric = metric;
        editMetric = new UpdateMissionMetricRequest
        {
            Name = metric.Name,
            Type = metric.Type,
            QuantitativeType = metric.QuantitativeType,
            MinValue = metric.MinValue,
            MaxValue = metric.MaxValue,
            Unit = metric.Unit,
            TargetText = metric.TargetText
        };
        editMetricTypeValue = metric.Type.ToString();
        editQuantitativeTypeValue = metric.QuantitativeType?.ToString();
        editMetricUnitValue = metric.Unit?.ToString();
        isEditModalOpen = true;
    }

    private void CloseEditMetricModal()
    {
        isEditModalOpen = false;
        ResetEditMetricForm();
    }

    private async Task UpdateMetric()
    {
        if (selectedMetric == null) return;

        if (!TryPrepareMetricForUpsert(
                editMetricTypeValue,
                editQuantitativeTypeValue,
                editMetricUnitValue,
                editMetric.MinValue,
                editMetric.MaxValue,
                editMetric.TargetText,
                "Erro ao atualizar métrica",
                "Selecione o tipo da métrica.",
                out var metricType,
                out var quantitativeType,
                out var unit))
        {
            return;
        }

        editMetric.Type = metricType;
        editMetric.QuantitativeType = quantitativeType;
        editMetric.Unit = unit;

        await UiOps.RunAsync(
            async () =>
            {
                await Api.UpdateMissionMetricAsync(selectedMetric.Id, editMetric);
                ToastService.ShowSuccess("Métrica atualizada", "As alterações foram salvas com sucesso.");
                CloseEditMetricModal();
                await LoadMetrics();
            },
            "Erro ao atualizar",
            "Não foi possível atualizar a métrica. Verifique os dados e tente novamente.");
    }

    private async Task OnEditMetricTypeChanged(ChangeEventArgs e)
    {
        editMetricTypeValue = e.Value?.ToString();
        ResetEditMetricInputFields();
        await InvokeAsync(StateHasChanged);
    }

    private bool TryPrepareMetricForUpsert(
        string? metricTypeValue,
        string? quantitativeTypeValue,
        string? metricUnitValue,
        decimal? minValue,
        decimal? maxValue,
        string? targetText,
        string errorTitle,
        string metricTypeErrorMessage,
        out MetricType metricType,
        out QuantitativeMetricType? quantitativeType,
        out MetricUnit? unit)
    {
        quantitativeType = null;
        unit = null;
        if (!TryParseMetricType(metricTypeValue, errorTitle, metricTypeErrorMessage, out metricType))
        {
            return false;
        }

        if (metricType == MetricType.Qualitative)
        {
            if (string.IsNullOrWhiteSpace(targetText))
            {
                ToastService.ShowError(errorTitle, "Informe o texto alvo.");
                return false;
            }

            return true;
        }

        return TryPrepareQuantitativeFields(quantitativeTypeValue, metricUnitValue, minValue, maxValue, errorTitle, out quantitativeType, out unit);
    }

    private bool TryPrepareQuantitativeFields(
        string? quantitativeTypeValue,
        string? metricUnitValue,
        decimal? minValue,
        decimal? maxValue,
        string errorTitle,
        out QuantitativeMetricType? quantitativeType,
        out MetricUnit? unit)
    {
        quantitativeType = null;
        unit = null;
        if (!Enum.TryParse<QuantitativeMetricType>(quantitativeTypeValue, out var parsedQuantitativeType))
        {
            ToastService.ShowError(errorTitle, "Selecione o tipo de métrica quantitativa.");
            return false;
        }

        if (parsedQuantitativeType == QuantitativeMetricType.KeepAbove && minValue is null)
        {
            ToastService.ShowError(errorTitle, "Informe o valor mínimo.");
            return false;
        }

        if (parsedQuantitativeType == QuantitativeMetricType.KeepBelow && maxValue is null)
        {
            ToastService.ShowError(errorTitle, "Informe o valor máximo.");
            return false;
        }

        if (parsedQuantitativeType == QuantitativeMetricType.KeepBetween)
        {
            if (minValue is null || maxValue is null)
            {
                ToastService.ShowError(errorTitle, "Informe os valores mínimo e máximo.");
                return false;
            }

            if (minValue >= maxValue)
            {
                ToastService.ShowError(errorTitle, "O valor mínimo deve ser menor que o valor máximo.");
                return false;
            }
        }

        if ((parsedQuantitativeType == QuantitativeMetricType.Achieve || parsedQuantitativeType == QuantitativeMetricType.Reduce) &&
            maxValue is null)
        {
            ToastService.ShowError(errorTitle, "Informe o valor alvo.");
            return false;
        }

        if (!Enum.TryParse<MetricUnit>(metricUnitValue, out var parsedUnit))
        {
            ToastService.ShowError(errorTitle, "Selecione a unidade.");
            return false;
        }

        quantitativeType = parsedQuantitativeType;
        unit = parsedUnit;
        return true;
    }

    private void ResetEditMetricForm()
    {
        selectedMetric = null;
        editMetric = new();
        editMetricTypeValue = null;
        ResetEditMetricInputFields();
    }

    private void ResetEditMetricInputFields()
    {
        editMetric.TargetText = null;
        editMetric.MinValue = null;
        editMetric.MaxValue = null;
        editMetric.QuantitativeType = null;
        editQuantitativeTypeValue = null;
        editMetricUnitValue = null;
    }

    // ---- Progress & Confidence Helpers ----

    private static string GetProgressStatusClass(MetricProgressDto progress) => MissionProgressDisplayHelper.GetMetricProgressStatusClass(progress);

    private static (string statusClass, string label) GetConfidenceDisplay(int confidence) => MissionProgressDisplayHelper.GetConfidenceDisplay(confidence);

    private static string GetConfidenceStarsText(int confidence) => MissionProgressDisplayHelper.GetConfidenceStarsText(confidence);

    // ---- Delete Methods ----

    private async Task HandleDeleteMetricClick(Guid metricId)
    {
        if (deletingMetricId == metricId)
        {
            await DeleteMetric(metricId);
        }
        else
        {
            ArmMetricDeleteConfirmation(metricId);
        }
    }

    private async Task DeleteMetric(Guid metricId)
    {
        try
        {
            await UiOps.RunAsync(
                async () =>
                {
                    await Api.DeleteMissionMetricAsync(metricId);
                    ToastService.ShowSuccess("Métrica excluída", "A métrica foi removida com sucesso.");
                    await LoadMetrics();
                },
                "Erro ao excluir",
                "Não foi possível excluir a métrica. Tente novamente.");
        }
        finally
        {
            ClearMetricDeleteConfirmation();
        }
    }

    private void ArmMetricDeleteConfirmation(Guid metricId)
    {
        deletingMetricId = metricId;
        deleteMetricConfirmTimer?.Dispose();
        deleteMetricConfirmTimer = new System.Threading.Timer(
            _ => InvokeAsync(() =>
            {
                deletingMetricId = null;
                StateHasChanged();
            }),
            null,
            3000,
            Timeout.Infinite);
    }

    private void ClearMetricDeleteConfirmation()
    {
        deletingMetricId = null;
        deleteMetricConfirmTimer?.Dispose();
        deleteMetricConfirmTimer = null;
    }
}
