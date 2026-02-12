namespace Bud.Shared.Models;

public sealed class MissionTemplateMetric : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid MissionTemplateId { get; set; }
    public MissionTemplate MissionTemplate { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public MetricType Type { get; set; }
    public int OrderIndex { get; set; }

    // Quantitative metric fields
    public QuantitativeMetricType? QuantitativeType { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public MetricUnit? Unit { get; set; }

    // Qualitative metric fields
    public string? TargetText { get; set; }

    public static MissionTemplateMetric Create(
        Guid id,
        Guid organizationId,
        Guid missionTemplateId,
        string name,
        MetricType type,
        int orderIndex,
        QuantitativeMetricType? quantitativeType,
        decimal? minValue,
        decimal? maxValue,
        MetricUnit? unit,
        string? targetText)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainInvariantException("O nome da métrica do template é obrigatório.");
        }

        if (orderIndex < 0)
        {
            throw new DomainInvariantException("A ordem da métrica do template deve ser maior ou igual a zero.");
        }

        var metric = new MissionTemplateMetric
        {
            Id = id,
            OrganizationId = organizationId,
            MissionTemplateId = missionTemplateId,
            Name = name.Trim(),
            Type = type,
            OrderIndex = orderIndex
        };

        metric.ApplyTarget(type, quantitativeType, minValue, maxValue, unit, targetText);
        return metric;
    }

    public void ApplyTarget(
        MetricType type,
        QuantitativeMetricType? quantitativeType,
        decimal? minValue,
        decimal? maxValue,
        MetricUnit? unit,
        string? targetText)
    {
        Type = type;

        if (type == MetricType.Qualitative)
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
            throw new DomainInvariantException("Tipo quantitativo é obrigatório para métricas quantitativas.");
        }

        if (minValue.HasValue && maxValue.HasValue)
        {
            _ = MetricRange.Create(minValue, maxValue);
        }

        QuantitativeType = quantitativeType;
        MinValue = minValue;
        MaxValue = maxValue;
        Unit = unit;
        TargetText = null;
    }
}
