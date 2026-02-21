namespace Bud.Server.Domain.Model;

public sealed class Team : ITenantEntity, IAggregateRoot
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;
    public Guid? ParentTeamId { get; set; }
    public Team? ParentTeam { get; set; }
    public ICollection<Team> SubTeams { get; set; } = new List<Team>();
    public ICollection<Collaborator> Collaborators { get; set; } = new List<Collaborator>();
    public ICollection<CollaboratorTeam> CollaboratorTeams { get; set; } = new List<CollaboratorTeam>();
    public Guid LeaderId { get; set; }
    public Collaborator? Leader { get; set; }

    public static Team Create(Guid id, Guid organizationId, Guid workspaceId, string name, Guid leaderId, Guid? parentTeamId = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainInvariantException("Time deve pertencer a uma organização válida.");
        }

        if (workspaceId == Guid.Empty)
        {
            throw new DomainInvariantException("Time deve pertencer a um workspace válido.");
        }

        if (leaderId == Guid.Empty)
        {
            throw new DomainInvariantException("O líder do time é obrigatório.");
        }

        var team = new Team
        {
            Id = id,
            OrganizationId = organizationId,
            WorkspaceId = workspaceId,
            LeaderId = leaderId
        };

        team.Rename(name);
        team.Reparent(parentTeamId, id);

        return team;
    }

    public void AssignLeader(Guid leaderId)
    {
        if (leaderId == Guid.Empty)
        {
            throw new DomainInvariantException("O líder do time é obrigatório.");
        }

        LeaderId = leaderId;
    }

    public void Rename(string name)
    {
        if (!EntityName.TryCreate(name, out var entityName))
        {
            throw new DomainInvariantException("O nome do time é obrigatório e deve ter até 200 caracteres.");
        }

        Name = entityName.Value;
    }

    public void Reparent(Guid? parentTeamId, Guid selfId)
    {
        if (parentTeamId.HasValue && parentTeamId.Value == selfId)
        {
            throw new DomainInvariantException("Um time não pode ser seu próprio pai.");
        }

        ParentTeamId = parentTeamId;
    }
}
