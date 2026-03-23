using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class IndicatorFormFields
{
    [Parameter, EditorRequired] public IndicatorFormModel Model { get; set; } = default!;
    [Parameter, EditorRequired] public EventCallback OnSave { get; set; }
    [Parameter, EditorRequired] public EventCallback OnCancel { get; set; }
    [Parameter] public string SaveLabel { get; set; } = "Salvar indicador";
    [Parameter] public string CancelLabel { get; set; } = "Cancelar";
    [Parameter] public bool ShowAddButton { get; set; }
    [Parameter] public string AddButtonLabel { get; set; } = "Adicionar item";

    private bool isFormVisible;

    private async Task HandleSave()
    {
        await OnSave.InvokeAsync();
        // If the parent cleared the model after a successful save, collapse to button
        if (ShowAddButton && string.IsNullOrWhiteSpace(Model.Name) && string.IsNullOrWhiteSpace(Model.TypeValue))
        {
            isFormVisible = false;
        }
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
        if (ShowAddButton)
        {
            isFormVisible = false;
        }
    }

    private void OnTypeChanged(ChangeEventArgs e)
    {
        Model.TypeValue = e.Value?.ToString();
        Model.QuantitativeTypeValue = null;
        Model.UnitValue = null;
        Model.MinValue = null;
        Model.MaxValue = null;
        Model.TargetText = null;
    }

    internal static string GetMaxValuePlaceholder(string? quantitativeType) => quantitativeType switch
    {
        "KeepBelow" or "KeepBetween" => "Valor máximo",
        "Achieve" or "Reduce" => "Valor alvo",
        _ => "Valor máximo"
    };

    public class IndicatorFormModel
    {
        public string Name { get; set; } = string.Empty;
        public string? TypeValue { get; set; }
        public string? QuantitativeTypeValue { get; set; }
        public string? UnitValue { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string? TargetText { get; set; }

        public void Clear()
        {
            Name = string.Empty;
            TypeValue = null;
            QuantitativeTypeValue = null;
            UnitValue = null;
            MinValue = null;
            MaxValue = null;
            TargetText = null;
        }
    }
}
