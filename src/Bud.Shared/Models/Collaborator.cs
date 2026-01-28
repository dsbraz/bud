namespace Bud.Shared.Models;

public sealed class Collaborator
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CollaboratorRole Role { get; set; } = CollaboratorRole.IndividualContributor;
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
}
