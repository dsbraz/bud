namespace Bud.Shared.Domain;

public sealed class Mission : ITenantEntity, IAggregateRoot
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
    public ICollection<MissionObjective> Objectives { get; set; } = new List<MissionObjective>();

    public static Mission Create(
        Guid id,
        Guid organizationId,
        string name,
        string? description,
        DateTime startDate,
        DateTime endDate,
        MissionStatus status)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainInvariantException("Missão deve pertencer a uma organização válida.");
        }

        var mission = new Mission
        {
            Id = id,
            OrganizationId = organizationId
        };

        mission.UpdateDetails(name, description, startDate, endDate, status);
        return mission;
    }

    public void UpdateDetails(
        string name,
        string? description,
        DateTime startDate,
        DateTime endDate,
        MissionStatus status)
    {
        if (!EntityName.TryCreate(name, out var entityName))
        {
            throw new DomainInvariantException("O nome da missão é obrigatório e deve ter até 200 caracteres.");
        }

        if (endDate < startDate)
        {
            throw new DomainInvariantException("Data de término deve ser igual ou posterior à data de início.");
        }

        Name = entityName.Value;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
    }

    public void SetScope(MissionScopeType scopeType, Guid scopeId)
    {
        SetScope(MissionScope.Create(scopeType, scopeId));
    }

    public void SetScope(MissionScope scope)
    {
        WorkspaceId = null;
        TeamId = null;
        CollaboratorId = null;

        switch (scope.ScopeType)
        {
            case MissionScopeType.Organization:
                return;
            case MissionScopeType.Workspace:
                WorkspaceId = scope.ScopeId;
                return;
            case MissionScopeType.Team:
                TeamId = scope.ScopeId;
                return;
            case MissionScopeType.Collaborator:
                CollaboratorId = scope.ScopeId;
                return;
            default:
                throw new DomainInvariantException("Escopo da missão inválido.");
        }
    }
}
