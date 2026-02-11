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
}
