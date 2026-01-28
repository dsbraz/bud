namespace Bud.Shared.Models;

public sealed class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;
    public Guid? ParentTeamId { get; set; }
    public Team? ParentTeam { get; set; }
    public ICollection<Team> SubTeams { get; set; } = new List<Team>();
    public ICollection<Collaborator> Collaborators { get; set; } = new List<Collaborator>();
}
