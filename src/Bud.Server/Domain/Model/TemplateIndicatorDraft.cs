namespace Bud.Server.Domain.Model;

public readonly record struct TemplateIndicatorDraft(
    string Name,
    IndicatorType Type,
    int OrderIndex,
    Guid? TemplateGoalId,
    QuantitativeIndicatorType? QuantitativeType,
    decimal? MinValue,
    decimal? MaxValue,
    IndicatorUnit? Unit,
    string? TargetText);
