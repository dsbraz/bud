namespace Bud.Shared.Models;

public sealed class Collaborator : ITenantEntity
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CollaboratorRole Role { get; set; } = CollaboratorRole.IndividualContributor;
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }
    public Guid? LeaderId { get; set; }
    public Collaborator? Leader { get; set; }
}
