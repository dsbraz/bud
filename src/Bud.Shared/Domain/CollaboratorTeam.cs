namespace Bud.Shared.Domain;

public sealed class CollaboratorTeam
{
    public Guid CollaboratorId { get; set; }
    public Collaborator Collaborator { get; set; } = null!;
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
