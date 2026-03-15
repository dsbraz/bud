using Bud.Shared.Contracts.Features.Goals;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class IndicatorFormModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public bool IsEditMode { get; set; }
    [Parameter] public TempIndicator? InitialModel { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<TempIndicator> OnSave { get; set; }

    private IndicatorFormFields.IndicatorFormModel _model = new();
    private bool _wasOpen;

    protected override void OnParametersSet()
    {
        if (IsOpen && !_wasOpen)
        {
            InitializeModel();
        }
        _wasOpen = IsOpen;
    }

    private void InitializeModel()
    {
        if (InitialModel is not null)
        {
            _model = new IndicatorFormFields.IndicatorFormModel
            {
                Name = InitialModel.Name,
                TypeValue = InitialModel.Type,
                QuantitativeTypeValue = InitialModel.QuantitativeType,
                UnitValue = InitialModel.Unit,
                MinValue = InitialModel.MinValue,
                MaxValue = InitialModel.MaxValue,
                TargetText = InitialModel.TargetText
            };
        }
        else
        {
            _model = new IndicatorFormFields.IndicatorFormModel();
        }
    }

    private async Task HandleSave()
    {
        if (string.IsNullOrWhiteSpace(_model.Name) || string.IsNullOrWhiteSpace(_model.TypeValue))
        {
            return;
        }

        var details = _model.TypeValue == "Quantitative"
            ? BuildQuantitativeDetails(_model.QuantitativeTypeValue, _model.MinValue, _model.MaxValue, _model.UnitValue)
            : _model.TargetText ?? "";

        var indicator = new TempIndicator(
            OriginalId: InitialModel?.OriginalId,
            Name: _model.Name,
            Type: _model.TypeValue,
            Details: details,
            QuantitativeType: _model.QuantitativeTypeValue,
            MinValue: _model.MinValue,
            MaxValue: _model.MaxValue,
            TargetText: _model.TargetText,
            Unit: _model.UnitValue);

        await OnSave.InvokeAsync(indicator);
    }

    private async Task HandleClose() => await OnClose.InvokeAsync();

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
