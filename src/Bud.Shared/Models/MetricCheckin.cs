namespace Bud.Shared.Models;

public sealed class MetricCheckin : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid MissionMetricId { get; set; }
    public MissionMetric MissionMetric { get; set; } = null!;
    public Guid CollaboratorId { get; set; }
    public Collaborator Collaborator { get; set; } = null!;

    public decimal? Value { get; set; }
    public string? Text { get; set; }

    public DateTime CheckinDate { get; set; }
    public string? Note { get; set; }
    public int ConfidenceLevel { get; set; }
}
