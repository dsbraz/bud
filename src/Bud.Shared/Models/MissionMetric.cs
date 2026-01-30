namespace Bud.Shared.Models;

public sealed class MissionMetric : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid MissionId { get; set; }
    public Mission Mission { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public MetricType Type { get; set; }
    public decimal? TargetValue { get; set; }
    public MetricUnit? Unit { get; set; }
    public string? TargetText { get; set; }
}
