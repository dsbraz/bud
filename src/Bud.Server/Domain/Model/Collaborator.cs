namespace Bud.Server.Domain.Model;

public sealed class Collaborator : ITenantEntity, IAggregateRoot
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
    public bool IsGlobalAdmin { get; set; }
    public ICollection<CollaboratorTeam> CollaboratorTeams { get; set; } = new List<CollaboratorTeam>();

    public static Collaborator Create(
        Guid id,
        Guid organizationId,
        string fullName,
        string email,
        CollaboratorRole role,
        Guid? leaderId = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainInvariantException("Colaborador deve pertencer a uma organização válida.");
        }

        var collaborator = new Collaborator
        {
            Id = id,
            OrganizationId = organizationId,
            TeamId = null
        };

        collaborator.UpdateProfile(fullName, email, role, leaderId, id);
        return collaborator;
    }

    public void UpdateProfile(string fullName, string email, CollaboratorRole role, Guid? leaderId, Guid selfId)
    {
        if (!PersonName.TryCreate(fullName, out var personName))
        {
            throw new DomainInvariantException("O nome do colaborador é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainInvariantException("O e-mail do colaborador é obrigatório.");
        }

        if (leaderId.HasValue && leaderId.Value == selfId)
        {
            throw new DomainInvariantException("Um colaborador não pode ser líder de si mesmo.");
        }

        FullName = personName.Value;
        Email = email.Trim();
        Role = role;
        LeaderId = leaderId;
    }
}
