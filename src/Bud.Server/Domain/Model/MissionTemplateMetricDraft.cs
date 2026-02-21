namespace Bud.Server.Domain.Model;

public readonly record struct MissionTemplateMetricDraft(
    string Name,
    MetricType Type,
    int OrderIndex,
    Guid? MissionTemplateObjectiveId,
    QuantitativeMetricType? QuantitativeType,
    decimal? MinValue,
    decimal? MaxValue,
    MetricUnit? Unit,
    string? TargetText);
