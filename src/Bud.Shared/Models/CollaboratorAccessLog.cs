namespace Bud.Shared.Models;

public sealed class CollaboratorAccessLog : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CollaboratorId { get; set; }
    public Collaborator Collaborator { get; set; } = null!;
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public DateTime AccessedAt { get; set; }
}
