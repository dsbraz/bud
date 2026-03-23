using Bud.Shared.Contracts;

namespace Bud.BlazorWasm.Services;

public static class IndicatorDisplayHelper
{
    public static string GetIndicatorTypeLabel(IndicatorType type) => type switch
    {
        IndicatorType.Qualitative => "Qualitativa",
        IndicatorType.Quantitative => "Quantitativa",
        _ => type.ToString()
    };

    public static string GetQuantitativeTypeLabel(QuantitativeIndicatorType type) => type switch
    {
        QuantitativeIndicatorType.KeepAbove => "Manter acima",
        QuantitativeIndicatorType.KeepBelow => "Manter abaixo",
        QuantitativeIndicatorType.KeepBetween => "Manter entre",
        QuantitativeIndicatorType.Achieve => "Atingir",
        QuantitativeIndicatorType.Reduce => "Reduzir",
        _ => type.ToString()
    };

    public static string GetQuantitativeTypeIcon(QuantitativeIndicatorType type) => type switch
    {
        QuantitativeIndicatorType.KeepAbove => "↑",
        QuantitativeIndicatorType.KeepBelow => "↓",
        QuantitativeIndicatorType.KeepBetween => "↕",
        QuantitativeIndicatorType.Achieve => "→",
        QuantitativeIndicatorType.Reduce => "↘",
        _ => string.Empty
    };

    public static string GetUnitLabel(IndicatorUnit unit) => unit switch
    {
        IndicatorUnit.Integer => "Inteiro",
        IndicatorUnit.Decimal => "Decimal",
        IndicatorUnit.Percentage => "Percentual",
        IndicatorUnit.Hours => "Horas",
        IndicatorUnit.Points => "Pontos",
        _ => unit.ToString()
    };

    public static string GetTargetLabel(IndicatorResponse metric)
    {
        if (metric.Type == IndicatorType.Qualitative)
        {
            return metric.TargetText ?? "—";
        }

        var unit = metric.Unit.HasValue ? GetUnitLabel(metric.Unit.Value) : string.Empty;
        var quantType = metric.QuantitativeType.HasValue
            ? GetQuantitativeTypeLabel(metric.QuantitativeType.Value)
            : "—";

        return metric.QuantitativeType switch
        {
            QuantitativeIndicatorType.KeepAbove => $"{quantType} {metric.MinValue} {unit}",
            QuantitativeIndicatorType.KeepBelow => $"{quantType} {metric.MaxValue} {unit}",
            QuantitativeIndicatorType.KeepBetween => $"{quantType} {metric.MinValue} e {metric.MaxValue} {unit}",
            QuantitativeIndicatorType.Achieve => $"{quantType} {metric.MaxValue} {unit}",
            QuantitativeIndicatorType.Reduce => $"{quantType} para {metric.MaxValue} {unit}",
            _ => "—"
        };
    }

    public static string GetCheckinTargetHint(IndicatorResponse metric, bool useAbbreviatedUnit = false)
    {
        if (metric.Type != IndicatorType.Quantitative || !metric.QuantitativeType.HasValue)
        {
            return string.Empty;
        }

        var unit = metric.Unit.HasValue
            ? (useAbbreviatedUnit ? GetUnitLabelShort(metric.Unit.Value) : GetUnitLabel(metric.Unit.Value))
            : string.Empty;

        return metric.QuantitativeType switch
        {
            QuantitativeIndicatorType.KeepAbove => $"(manter acima de {metric.MinValue} {unit})",
            QuantitativeIndicatorType.KeepBelow => $"(manter abaixo de {metric.MaxValue} {unit})",
            QuantitativeIndicatorType.KeepBetween => $"(manter entre {metric.MinValue} e {metric.MaxValue} {unit})",
            QuantitativeIndicatorType.Achieve => $"(atingir {metric.MaxValue} {unit})",
            QuantitativeIndicatorType.Reduce => $"(reduzir para {metric.MaxValue} {unit})",
            _ => string.Empty
        };
    }

    public static string GetUnitLabelShort(IndicatorUnit unit) => unit switch
    {
        IndicatorUnit.Integer => "inteiro",
        IndicatorUnit.Decimal => "decimal",
        IndicatorUnit.Percentage => "%",
        IndicatorUnit.Hours => "h",
        IndicatorUnit.Points => "pts",
        _ => unit.ToString()
    };
}
