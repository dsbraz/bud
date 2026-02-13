using Bud.Shared.Domain;

namespace Bud.Client.Services;

public static class MissionMetricDisplayHelper
{
    public static string GetMetricTypeLabel(MetricType type) => type switch
    {
        MetricType.Qualitative => "Qualitativa",
        MetricType.Quantitative => "Quantitativa",
        _ => type.ToString()
    };

    public static string GetQuantitativeTypeLabel(QuantitativeMetricType type) => type switch
    {
        QuantitativeMetricType.KeepAbove => "Manter acima",
        QuantitativeMetricType.KeepBelow => "Manter abaixo",
        QuantitativeMetricType.KeepBetween => "Manter entre",
        QuantitativeMetricType.Achieve => "Atingir",
        QuantitativeMetricType.Reduce => "Reduzir",
        _ => type.ToString()
    };

    public static string GetQuantitativeTypeIcon(QuantitativeMetricType type) => type switch
    {
        QuantitativeMetricType.KeepAbove => "↑",
        QuantitativeMetricType.KeepBelow => "↓",
        QuantitativeMetricType.KeepBetween => "↕",
        QuantitativeMetricType.Achieve => "→",
        QuantitativeMetricType.Reduce => "↘",
        _ => string.Empty
    };

    public static string GetUnitLabel(MetricUnit unit) => unit switch
    {
        MetricUnit.Integer => "Inteiro",
        MetricUnit.Decimal => "Decimal",
        MetricUnit.Percentage => "Percentual",
        MetricUnit.Hours => "Horas",
        MetricUnit.Points => "Pontos",
        _ => unit.ToString()
    };

    public static string GetTargetLabel(MissionMetric metric)
    {
        if (metric.Type == MetricType.Qualitative)
        {
            return metric.TargetText ?? "—";
        }

        var unit = metric.Unit.HasValue ? GetUnitLabel(metric.Unit.Value) : string.Empty;
        var quantType = metric.QuantitativeType.HasValue
            ? GetQuantitativeTypeLabel(metric.QuantitativeType.Value)
            : "—";

        return metric.QuantitativeType switch
        {
            QuantitativeMetricType.KeepAbove => $"{quantType} {metric.MinValue} {unit}",
            QuantitativeMetricType.KeepBelow => $"{quantType} {metric.MaxValue} {unit}",
            QuantitativeMetricType.KeepBetween => $"{quantType} {metric.MinValue} e {metric.MaxValue} {unit}",
            QuantitativeMetricType.Achieve => $"{quantType} {metric.MaxValue} {unit}",
            QuantitativeMetricType.Reduce => $"{quantType} para {metric.MaxValue} {unit}",
            _ => "—"
        };
    }

    public static string GetCheckinTargetHint(MissionMetric metric, bool useAbbreviatedUnit = false)
    {
        if (metric.Type != MetricType.Quantitative || !metric.QuantitativeType.HasValue)
        {
            return string.Empty;
        }

        var unit = metric.Unit.HasValue
            ? (useAbbreviatedUnit ? GetUnitLabelShort(metric.Unit.Value) : GetUnitLabel(metric.Unit.Value))
            : string.Empty;

        return metric.QuantitativeType switch
        {
            QuantitativeMetricType.KeepAbove => $"(manter acima de {metric.MinValue} {unit})",
            QuantitativeMetricType.KeepBelow => $"(manter abaixo de {metric.MaxValue} {unit})",
            QuantitativeMetricType.KeepBetween => $"(manter entre {metric.MinValue} e {metric.MaxValue} {unit})",
            QuantitativeMetricType.Achieve => $"(atingir {metric.MaxValue} {unit})",
            QuantitativeMetricType.Reduce => $"(reduzir para {metric.MaxValue} {unit})",
            _ => string.Empty
        };
    }

    public static string GetUnitLabelShort(MetricUnit unit) => unit switch
    {
        MetricUnit.Integer => "inteiro",
        MetricUnit.Decimal => "decimal",
        MetricUnit.Percentage => "%",
        MetricUnit.Hours => "h",
        MetricUnit.Points => "pts",
        _ => unit.ToString()
    };
}
