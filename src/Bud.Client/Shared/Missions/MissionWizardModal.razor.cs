using Bud.Client.Shared;
using Bud.Shared.Domain;
using Microsoft.AspNetCore.Components;

#pragma warning disable IDE0011

namespace Bud.Client.Shared.Missions;

public partial class MissionWizardModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public bool IsEditMode { get; set; }
    [Parameter] public string OrganizationName { get; set; } = "Organização";
    [Parameter] public MissionWizardModel? InitialModel { get; set; }
    [Parameter] public Func<string?, IEnumerable<ScopeOption>> GetScopeOptions { get; set; } = _ => [];
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<MissionWizardResult> OnSave { get; set; }
    [Parameter] public EventCallback<MissionWizardResult> OnSaveDraft { get; set; }

    // Wizard step
    private int wizardStep = 1;

    // Mission form fields
    private string name = "";
    private string? description;
    private DateTime startDate = DateTime.Today;
    private DateTime endDate = DateTime.Today.AddDays(7);
    private string? scopeTypeValue;
    private string? scopeId;
    private string? statusValue;
    private bool showResponsibleSelector;
    private bool showPeriodSelector;
    private bool showStatusSelector;

    // Metrics
    private List<TempMetric> tempMetrics = [];
    private MetricFormFields.MetricFormModel newMetricModel = new();
    private MetricFormFields.MetricFormModel editTempMetricModel = new();
    private int? editingTempMetricIndex;

    // Objectives
    private List<TempObjective> tempObjectives = [];
    private string tempObjectiveName = "";
    private string? tempObjectiveDescription;
    private int? editingTempObjectiveIndex;
    private string editingObjectiveName = "";
    private string? editingObjectiveDescription;
    private string? addingMetricToObjectiveTempId;
    private bool showObjectiveForm;

    // Edit tracking
    private HashSet<Guid> deletedMetricIds = [];
    private HashSet<Guid> deletedObjectiveIds = [];

    private bool wasOpen;

    protected override void OnParametersSet()
    {
        if (IsOpen && !wasOpen)
        {
            InitializeFromModel(InitialModel);
        }
        wasOpen = IsOpen;
    }

    private void InitializeFromModel(MissionWizardModel? model)
    {
        wizardStep = 1;
        name = model?.Name ?? "";
        description = model?.Description;
        startDate = model?.StartDate ?? DateTime.Today;
        endDate = model?.EndDate ?? DateTime.Today.AddDays(7);
        scopeTypeValue = model?.ScopeTypeValue;
        scopeId = model?.ScopeId;
        statusValue = model?.StatusValue;
        showResponsibleSelector = false;
        showPeriodSelector = false;
        showStatusSelector = false;
        tempMetrics = model?.Metrics?.ToList() ?? [];
        tempObjectives = model?.Objectives?.ToList() ?? [];
        newMetricModel = new();
        editTempMetricModel = new();
        editingTempMetricIndex = null;
        tempObjectiveName = "";
        tempObjectiveDescription = null;
        editingTempObjectiveIndex = null;
        editingObjectiveName = "";
        editingObjectiveDescription = null;
        addingMetricToObjectiveTempId = null;
        showObjectiveForm = false;
        deletedMetricIds = [];
        deletedObjectiveIds = [];
    }

    // ---- Save / Close ----

    private async Task HandleSave()
    {
        var errorTitle = IsEditMode ? "Erro ao salvar" : "Erro ao criar missão";
        if (!Validate(errorTitle)) return;
        FlushPendingMetric();
        await OnSave.InvokeAsync(BuildResult());
    }

    private async Task HandleSaveDraft()
    {
        if (!Validate("Erro ao salvar rascunho")) return;
        FlushPendingMetric();
        await OnSaveDraft.InvokeAsync(BuildResult());
    }

    private async Task HandleClose() => await OnClose.InvokeAsync();

    private bool Validate(string errorTitle)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ToastService.ShowError(errorTitle, "Informe o nome da missão.");
            return false;
        }
        if (string.IsNullOrEmpty(scopeTypeValue) || !Enum.TryParse<MissionScopeType>(scopeTypeValue, out _))
        {
            ToastService.ShowError(errorTitle, "Selecione o escopo.");
            return false;
        }
        if (string.IsNullOrEmpty(scopeId) || !Guid.TryParse(scopeId, out _))
        {
            ToastService.ShowError(errorTitle, "Selecione a referência do escopo.");
            return false;
        }
        if (endDate < startDate)
        {
            ToastService.ShowError(errorTitle, "A data de fim precisa ser igual ou maior que a data de início.");
            return false;
        }
        return true;
    }

    private MissionWizardResult BuildResult() => new()
    {
        Name = name.Trim(),
        Description = description,
        StartDate = startDate,
        EndDate = endDate,
        ScopeTypeValue = scopeTypeValue,
        ScopeId = scopeId,
        StatusValue = statusValue,
        Metrics = tempMetrics.ToList(),
        Objectives = tempObjectives.ToList(),
        DeletedMetricIds = new HashSet<Guid>(deletedMetricIds),
        DeletedObjectiveIds = new HashSet<Guid>(deletedObjectiveIds)
    };

    // ---- Scope ----

    private void HandleScopeTypeChanged(ChangeEventArgs e)
    {
        scopeTypeValue = e.Value?.ToString();
        scopeId = null;
    }

    // ---- Metric Methods ----

    private void FlushPendingMetric()
    {
        if (TryBuildTempMetric(newMetricModel, null, null, out var metric))
        {
            tempMetrics.Add(metric);
            newMetricModel.Clear();
        }
    }

    private void AddMetricFromForm()
    {
        if (!TryBuildTempMetric(newMetricModel, null, null, out var metric)) return;

        tempMetrics.Add(metric);
        newMetricModel.Clear();
    }

    private void CancelAddMetricForm() => newMetricModel.Clear();

    private void RemoveMetric(TempMetric metric)
    {
        tempMetrics.Remove(metric);
        if (IsEditMode && metric.OriginalId.HasValue)
        {
            deletedMetricIds.Add(metric.OriginalId.Value);
        }
    }

    private void StartAddMetricToObjective(string objectiveTempId)
    {
        addingMetricToObjectiveTempId = objectiveTempId;
        newMetricModel.Clear();
    }

    private void AddMetricToObjective()
    {
        if (!TryBuildTempMetric(newMetricModel, null, addingMetricToObjectiveTempId, out var metric)) return;

        tempMetrics.Add(metric);
        ResetAddMetricToObjectiveState();
    }

    private void CancelAddMetricToObjective()
    {
        ResetAddMetricToObjectiveState();
    }

    private void StartTempMetric(int index)
    {
        var metric = tempMetrics[index];
        editingTempMetricIndex = index;
        editTempMetricModel = new MetricFormFields.MetricFormModel
        {
            Name = metric.Name,
            TypeValue = metric.Type,
            QuantitativeTypeValue = metric.QuantitativeType,
            UnitValue = metric.Unit,
            MinValue = metric.MinValue,
            MaxValue = metric.MaxValue,
            TargetText = metric.TargetText
        };
    }

    private void CancelTempMetric()
    {
        editingTempMetricIndex = null;
        editTempMetricModel.Clear();
    }

    private void SaveTempMetric()
    {
        if (editingTempMetricIndex is null) return;
        var existing = tempMetrics[editingTempMetricIndex.Value];
        if (!TryBuildTempMetric(editTempMetricModel, existing.OriginalId, existing.ObjectiveTempId, out var updatedMetric)) return;

        tempMetrics[editingTempMetricIndex.Value] = updatedMetric;

        CancelTempMetric();
    }

    private bool TryBuildTempMetric(
        MetricFormFields.MetricFormModel model,
        Guid? originalId,
        string? objectiveTempId,
        out TempMetric metric)
    {
        metric = default!;
        if (!ValidateMetricModel(model))
        {
            return false;
        }

        metric = new TempMetric(
            OriginalId: originalId,
            Name: model.Name,
            Type: model.TypeValue!,
            Details: BuildMetricDetails(model),
            QuantitativeType: model.QuantitativeTypeValue,
            MinValue: model.MinValue,
            MaxValue: model.MaxValue,
            TargetText: model.TargetText,
            Unit: model.UnitValue,
            ObjectiveTempId: objectiveTempId);
        return true;
    }

    private static string BuildMetricDetails(MetricFormFields.MetricFormModel model)
    {
        return model.TypeValue == "Quantitative"
            ? BuildQuantitativeDetails(model.QuantitativeTypeValue, model.MinValue, model.MaxValue, model.UnitValue)
            : model.TargetText ?? "";
    }

    private void ResetAddMetricToObjectiveState()
    {
        addingMetricToObjectiveTempId = null;
        newMetricModel.Clear();
    }

    private bool ValidateMetricModel(MetricFormFields.MetricFormModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.TypeValue))
        {
            ToastService.ShowError("Erro de validação", "Informe o nome e tipo da métrica.");
            return false;
        }
        return true;
    }

    // ---- Objective Methods ----

    private void AddTempObjective()
    {
        if (string.IsNullOrWhiteSpace(tempObjectiveName))
        {
            ToastService.ShowError("Erro de validação", "Informe o nome do objetivo.");
            return;
        }

        tempObjectives.Add(new TempObjective(
            TempId: Guid.NewGuid().ToString(),
            Name: tempObjectiveName.Trim(),
            Description: string.IsNullOrWhiteSpace(tempObjectiveDescription) ? null : tempObjectiveDescription.Trim()));

        tempObjectiveName = "";
        tempObjectiveDescription = null;
        showObjectiveForm = false;
    }

    private void CancelAddObjective()
    {
        tempObjectiveName = "";
        tempObjectiveDescription = null;
        showObjectiveForm = false;
    }

    private void StartEditTempObjective(int index)
    {
        var obj = tempObjectives[index];
        editingTempObjectiveIndex = index;
        editingObjectiveName = obj.Name;
        editingObjectiveDescription = obj.Description;
    }

    private void CancelEditTempObjective()
    {
        editingTempObjectiveIndex = null;
        editingObjectiveName = "";
        editingObjectiveDescription = null;
    }

    private void SaveEditTempObjective()
    {
        if (editingTempObjectiveIndex is null || string.IsNullOrWhiteSpace(editingObjectiveName)) return;

        var existing = tempObjectives[editingTempObjectiveIndex.Value];
        tempObjectives[editingTempObjectiveIndex.Value] = new TempObjective(
            TempId: existing.TempId,
            Name: editingObjectiveName.Trim(),
            Description: string.IsNullOrWhiteSpace(editingObjectiveDescription) ? null : editingObjectiveDescription.Trim(),
            OriginalId: existing.OriginalId);

        CancelEditTempObjective();
    }

    private void RemoveTempObjective(int index)
    {
        var obj = tempObjectives[index];
        var removedTempId = obj.TempId;
        tempObjectives.RemoveAt(index);

        if (IsEditMode && obj.OriginalId.HasValue)
        {
            deletedObjectiveIds.Add(obj.OriginalId.Value);
        }

        var metricsToRemove = tempMetrics.Where(m => m.ObjectiveTempId == removedTempId).ToList();
        foreach (var metric in metricsToRemove)
        {
            if (IsEditMode && metric.OriginalId.HasValue)
            {
                deletedMetricIds.Add(metric.OriginalId.Value);
            }
            tempMetrics.Remove(metric);
        }

        if (editingTempObjectiveIndex.HasValue)
        {
            if (editingTempObjectiveIndex.Value == index)
                CancelEditTempObjective();
            else if (editingTempObjectiveIndex.Value > index)
                editingTempObjectiveIndex--;
        }
    }

    // ---- Display Helpers ----

    private string GetResponsibleLabel()
    {
        if (!string.IsNullOrEmpty(scopeId) && Enum.TryParse<MissionScopeType>(scopeTypeValue, out _))
        {
            var option = GetScopeOptions(scopeTypeValue).FirstOrDefault(o => o.Id == scopeId);
            if (option != null) return $"Responsável: {option.Name}";
        }
        return "Responsável";
    }

    private string GetPeriodLabel()
    {
        if (startDate != DateTime.MinValue && endDate != DateTime.MinValue)
            return $"Período de início e fim: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
        return "Período de início e fim";
    }

    private string GetStatusChipLabel()
    {
        if (Enum.TryParse<MissionStatus>(statusValue, out var status))
            return $"Status: {GetStatusLabel(status)}";
        return "Status";
    }

    private static string GetStatusLabel(MissionStatus status) => status switch
    {
        MissionStatus.Planned => "Planejada",
        MissionStatus.Active => "Ativa",
        MissionStatus.Completed => "Concluída",
        MissionStatus.Cancelled => "Cancelada",
        _ => status.ToString()
    };

    private static string GetScopeLabel(MissionScopeType scopeType) => scopeType switch
    {
        MissionScopeType.Organization => "Organização",
        MissionScopeType.Workspace => "Espaço de trabalho",
        MissionScopeType.Team => "Equipe",
        MissionScopeType.Collaborator => "Colaborador",
        _ => scopeType.ToString()
    };

    private static string BuildQuantitativeDetails(string? quantitativeType, decimal? minValue, decimal? maxValue, string? unit)
    {
        var unitLabel = unit switch
        {
            "Integer" or "Decimal" => "un",
            "Percentage" => "%",
            "Hours" => "h",
            "Points" => "pts",
            _ => ""
        };
        return quantitativeType switch
        {
            "KeepAbove" => $"Acima de {minValue} {unitLabel}",
            "KeepBelow" => $"Abaixo de {maxValue} {unitLabel}",
            "KeepBetween" => $"Entre {minValue} e {maxValue} {unitLabel}",
            "Achieve" => $"Atingir {maxValue} {unitLabel}",
            "Reduce" => $"Reduzir para {maxValue} {unitLabel}",
            _ => ""
        };
    }
}
