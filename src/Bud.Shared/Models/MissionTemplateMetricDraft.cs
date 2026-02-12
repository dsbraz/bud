namespace Bud.Shared.Models;

public readonly record struct MissionTemplateMetricDraft(
    string Name,
    MetricType Type,
    int OrderIndex,
    QuantitativeMetricType? QuantitativeType,
    decimal? MinValue,
    decimal? MaxValue,
    MetricUnit? Unit,
    string? TargetText);
