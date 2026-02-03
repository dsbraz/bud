namespace Bud.Shared.Models;

public sealed class Mission : ITenantEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public MissionStatus Status { get; set; }

    // Tenant discriminator — always set to the owning organization
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    // Scope FKs — only one is set to indicate mission scope level
    public Guid? WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }
    public Guid? CollaboratorId { get; set; }
    public Collaborator? Collaborator { get; set; }

    public ICollection<MissionMetric> Metrics { get; set; } = new List<MissionMetric>();
}
