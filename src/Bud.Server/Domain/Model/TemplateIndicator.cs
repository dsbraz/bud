namespace Bud.Server.Domain.Model;

public sealed class TemplateIndicator : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid TemplateId { get; set; }
    public Template Template { get; set; } = null!;
    public Guid? TemplateGoalId { get; set; }
    public TemplateGoal? TemplateGoal { get; set; }

    public string Name { get; set; } = string.Empty;
    public IndicatorType Type { get; set; }
    public int OrderIndex { get; set; }

    // Quantitative indicator fields
    public QuantitativeIndicatorType? QuantitativeType { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public IndicatorUnit? Unit { get; set; }

    // Qualitative indicator fields
    public string? TargetText { get; set; }

    public static TemplateIndicator Create(
        Guid id,
        Guid organizationId,
        Guid templateId,
        string name,
        IndicatorType type,
        int orderIndex,
        Guid? templateGoalId,
        QuantitativeIndicatorType? quantitativeType,
        decimal? minValue,
        decimal? maxValue,
        IndicatorUnit? unit,
        string? targetText)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainInvariantException("O nome do indicador do template é obrigatório.");
        }

        if (orderIndex < 0)
        {
            throw new DomainInvariantException("A ordem do indicador do template deve ser maior ou igual a zero.");
        }

        var indicator = new TemplateIndicator
        {
            Id = id,
            OrganizationId = organizationId,
            TemplateId = templateId,
            TemplateGoalId = templateGoalId,
            Name = name.Trim(),
            Type = type,
            OrderIndex = orderIndex
        };

        indicator.ApplyTarget(type, quantitativeType, minValue, maxValue, unit, targetText);
        return indicator;
    }

    public void ApplyTarget(
        IndicatorType type,
        QuantitativeIndicatorType? quantitativeType,
        decimal? minValue,
        decimal? maxValue,
        IndicatorUnit? unit,
        string? targetText)
    {
        Type = type;

        if (type == IndicatorType.Qualitative)
        {
            TargetText = string.IsNullOrWhiteSpace(targetText) ? null : targetText.Trim();
            QuantitativeType = null;
            MinValue = null;
            MaxValue = null;
            Unit = null;
            return;
        }

        if (quantitativeType is null)
        {
            throw new DomainInvariantException("Tipo quantitativo é obrigatório para indicadores quantitativos.");
        }

        if (minValue.HasValue && maxValue.HasValue)
        {
            _ = IndicatorRange.Create(minValue, maxValue);
        }

        QuantitativeType = quantitativeType;
        MinValue = minValue;
        MaxValue = maxValue;
        Unit = unit;
        TargetText = null;
    }
}
