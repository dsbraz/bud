#pragma warning disable IDE0011, IDE0044, CA1822, CA1859, CA1860, CA1868

using Bud.BlazorWasm.Api;
using Bud.BlazorWasm.Features.Goals.Components;
using Bud.BlazorWasm.Services;
using Bud.BlazorWasm.State;
using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bud.BlazorWasm.Features.Templates.Pages;

public partial class GoalTemplates : IDisposable
{
    [Inject] private ApiClient Api { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;
    [Inject] private OrganizationContext OrgContext { get; set; } = null!;
    [Inject] private UiOperationService UiOps { get; set; } = null!;

    // Data
    private PagedResult<TemplateResponse>? _templates;

    // Filter
    private string? _search;
    private bool _showFilterPanel;

    // Form modal
    private bool _isWizardOpen;
    private bool _isEditMode;
    private Guid? _editingTemplateId;
    private GoalFormModel? _wizardInitialModel;

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
        _templates = await Api.GetTemplatesAsync(_search, 1, 20) ?? new PagedResult<TemplateResponse>();
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
        _wizardInitialModel = new GoalFormModel();
        _isWizardOpen = true;
    }

    private void OpenEditModal(TemplateResponse template)
    {
        _isEditMode = true;
        _editingTemplateId = template.Id;

        var directIndicators = template.Indicators
            .Where(m => !m.TemplateGoalId.HasValue)
            .OrderBy(m => m.OrderIndex)
            .Select(BuildTempIndicatorFromTemplate)
            .ToList();

        var children = template.Goals
            .OrderBy(o => o.OrderIndex)
            .Select(o =>
            {
                var goalIndicators = template.Indicators
                    .Where(m => m.TemplateGoalId == o.Id)
                    .OrderBy(m => m.OrderIndex)
                    .Select(BuildTempIndicatorFromTemplate)
                    .ToList();

                return new TempGoal(
                    Guid.NewGuid().ToString(),
                    o.Name,
                    o.Description,
                    o.Id,
                    o.Dimension)
                {
                    Indicators = goalIndicators
                };
            })
            .ToList();

        _wizardInitialModel = new GoalFormModel
        {
            Name = template.Name,
            Description = template.Description,
            Indicators = directIndicators,
            Children = children
        };

        _isWizardOpen = true;
    }

    private static TempIndicator BuildTempIndicatorFromTemplate(TemplateIndicatorResponse m)
    {
        return new TempIndicator(
            null,
            m.Name,
            m.Type.ToString(),
            BuildIndicatorDetails(m),
            m.QuantitativeType?.ToString(),
            m.MinValue,
            m.MaxValue,
            m.TargetText,
            m.Unit?.ToString());
    }

    private void CloseWizard()
    {
        _isWizardOpen = false;
        _isEditMode = false;
        _editingTemplateId = null;
        _wizardInitialModel = null;
    }

    // ---- Wizard Save Handler ----

    private async Task HandleWizardSave(GoalFormResult result)
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

    private async Task CreateTemplate(GoalFormResult result)
    {
        var request = BuildCreateRequest(result);

        await UiOps.RunAsync(
            async () =>
            {
                await Api.CreateTemplateAsync(request);
                await LoadTemplates();
                ToastService.ShowSuccess("Modelo criado com sucesso!", $"O modelo '{result.Name}' foi criado.");
                CloseWizard();
            },
            "Erro ao criar modelo",
            "Não foi possível criar o modelo. Verifique os dados e tente novamente.");
    }

    private async Task UpdateTemplate(GoalFormResult result)
    {
        if (!_editingTemplateId.HasValue) return;

        var request = BuildUpdateRequest(result);

        await UiOps.RunAsync(
            async () =>
            {
                await Api.UpdateTemplateAsync(_editingTemplateId.Value, request);
                await LoadTemplates();
                ToastService.ShowSuccess("Modelo atualizado", "As alterações foram salvas com sucesso.");
                CloseWizard();
            },
            "Erro ao atualizar modelo",
            "Não foi possível atualizar o modelo. Verifique os dados e tente novamente.");
    }

    // ---- Payload Builders ----

    private static CreateTemplateRequest BuildCreateRequest(GoalFormResult result)
    {
        var (goals, indicators) = BuildTemplatePayload(result);
        return new CreateTemplateRequest
        {
            Name = result.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(result.Description) ? null : result.Description.Trim(),
            Goals = goals,
            Indicators = indicators
        };
    }

    private static PatchTemplateRequest BuildUpdateRequest(GoalFormResult result)
    {
        var (goals, indicators) = BuildTemplatePayload(result);
        return new PatchTemplateRequest
        {
            Name = result.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(result.Description) ? null : result.Description.Trim(),
            Goals = goals,
            Indicators = indicators
        };
    }

    private static (List<TemplateGoalRequest> Goals, List<TemplateIndicatorRequest> Indicators) BuildTemplatePayload(GoalFormResult result)
    {
        var goals = new List<TemplateGoalRequest>();
        var indicators = new List<TemplateIndicatorRequest>();

        // Direct indicators (no goal)
        foreach (var indicator in result.Indicators)
        {
            indicators.Add(BuildTemplateIndicatorRequest(indicator, null, indicators.Count));
        }

        // Flatten hierarchical children into flat goals + indicators for template API
        FlattenChildrenForTemplate(result.Children, goals, indicators);

        return (goals, indicators);
    }

    private static void FlattenChildrenForTemplate(
        List<TempGoal> children,
        List<TemplateGoalRequest> goals,
        List<TemplateIndicatorRequest> indicators)
    {
        foreach (var child in children)
        {
            var goalId = child.OriginalId ?? Guid.NewGuid();
            goals.Add(new TemplateGoalRequest
            {
                Id = goalId,
                Name = child.Name,
                Description = child.Description,
                Dimension = child.Dimension,
                OrderIndex = goals.Count
            });

            foreach (var indicator in child.Indicators)
            {
                indicators.Add(BuildTemplateIndicatorRequest(indicator, goalId, indicators.Count));
            }

            // Templates are flat, so nested children also become top-level goals
            FlattenChildrenForTemplate(child.Children, goals, indicators);
        }
    }

    private static TemplateIndicatorRequest BuildTemplateIndicatorRequest(TempIndicator indicator, Guid? templateGoalId, int orderIndex)
    {
        return new TemplateIndicatorRequest
        {
            Name = indicator.Name,
            Type = Enum.Parse<IndicatorType>(indicator.Type),
            OrderIndex = orderIndex,
            QuantitativeType = ParseOptionalEnum<QuantitativeIndicatorType>(indicator.QuantitativeType),
            MinValue = indicator.MinValue,
            MaxValue = indicator.MaxValue,
            Unit = ParseOptionalEnum<IndicatorUnit>(indicator.Unit),
            TargetText = indicator.TargetText,
            TemplateGoalId = templateGoalId
        };
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
                    await Api.DeleteTemplateAsync(templateId);
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

    private static string BuildIndicatorDetails(TemplateIndicatorResponse indicator)
    {
        return indicator.Type == IndicatorType.Quantitative
            ? BuildQuantitativeDetails(indicator.QuantitativeType?.ToString(), indicator.MinValue, indicator.MaxValue, indicator.Unit?.ToString())
            : indicator.TargetText ?? string.Empty;
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
