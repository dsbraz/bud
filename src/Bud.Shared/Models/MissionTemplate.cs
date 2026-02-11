namespace Bud.Shared.Models;

public sealed class MissionTemplate : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MissionNamePattern { get; set; }
    public string? MissionDescriptionPattern { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MissionTemplateMetric> Metrics { get; set; } = new List<MissionTemplateMetric>();
}
