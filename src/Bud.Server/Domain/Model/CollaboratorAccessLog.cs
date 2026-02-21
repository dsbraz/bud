namespace Bud.Server.Domain.Model;

public sealed class CollaboratorAccessLog : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CollaboratorId { get; set; }
    public Collaborator Collaborator { get; set; } = null!;
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public DateTime AccessedAt { get; set; }

    public static CollaboratorAccessLog Create(Guid id, Guid collaboratorId, Guid organizationId, DateTime accessedAt)
    {
        if (collaboratorId == Guid.Empty)
        {
            throw new DomainInvariantException("O log de acesso deve possuir um colaborador válido.");
        }

        if (organizationId == Guid.Empty)
        {
            throw new DomainInvariantException("O log de acesso deve possuir uma organização válida.");
        }

        return new CollaboratorAccessLog
        {
            Id = id,
            CollaboratorId = collaboratorId,
            OrganizationId = organizationId,
            AccessedAt = accessedAt
        };
    }
}
