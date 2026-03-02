using System.ComponentModel.DataAnnotations.Schema;
using Bud.Server.Domain.Events;

namespace Bud.Server.Domain.Model;

public sealed class Goal : ITenantEntity, IAggregateRoot, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Dimension { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public GoalStatus Status { get; set; }

    // Tenant discriminator — always set to the owning organization
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    // Recursive parent-child relationship
    public Guid? ParentId { get; set; }
    public Goal? Parent { get; set; }
    public ICollection<Goal> Children { get; set; } = [];

    // Scope FKs — only one is set (inherits from parent when null)
    public Guid? WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }
    public Guid? CollaboratorId { get; set; }
    public Collaborator? Collaborator { get; set; }

    public ICollection<Indicator> Indicators { get; set; } = [];
    public ICollection<GoalTask> Tasks { get; set; } = [];

    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Goal Create(
        Guid id,
        Guid organizationId,
        string name,
        string? description,
        string? dimension,
        DateTime startDate,
        DateTime endDate,
        GoalStatus status,
        Guid? parentId = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainInvariantException("Meta deve pertencer a uma organização válida.");
        }

        var goal = new Goal
        {
            Id = id,
            OrganizationId = organizationId,
            ParentId = parentId
        };

        goal.ApplyDetails(name, description, dimension, startDate, endDate, status);
        goal.AddDomainEvent(new GoalCreatedDomainEvent(goal.Id, goal.OrganizationId));
        return goal;
    }

    public void UpdateDetails(
        string name,
        string? description,
        string? dimension,
        DateTime startDate,
        DateTime endDate,
        GoalStatus status)
    {
        ApplyDetails(name, description, dimension, startDate, endDate, status);
    }

    public void MarkAsUpdated()
    {
        AddDomainEvent(new GoalUpdatedDomainEvent(Id, OrganizationId));
    }

    public void MarkAsDeleted()
    {
        AddDomainEvent(new GoalDeletedDomainEvent(Id, OrganizationId));
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private void ApplyDetails(
        string name,
        string? description,
        string? dimension,
        DateTime startDate,
        DateTime endDate,
        GoalStatus status)
    {
        if (!EntityName.TryCreate(name, out var entityName))
        {
            throw new DomainInvariantException("O nome da meta é obrigatório e deve ter até 200 caracteres.");
        }

        if (endDate < startDate)
        {
            throw new DomainInvariantException("Data de término deve ser igual ou posterior à data de início.");
        }

        Name = entityName.Value;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Dimension = string.IsNullOrWhiteSpace(dimension) ? null : dimension.Trim();
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void SetScope(GoalScopeType scopeType, Guid scopeId)
    {
        SetScope(GoalScope.Create(scopeType, scopeId));
    }

    public void SetScope(GoalScope scope)
    {
        WorkspaceId = null;
        TeamId = null;
        CollaboratorId = null;

        switch (scope.ScopeType)
        {
            case GoalScopeType.Organization:
                return;
            case GoalScopeType.Workspace:
                WorkspaceId = scope.ScopeId;
                return;
            case GoalScopeType.Team:
                TeamId = scope.ScopeId;
                return;
            case GoalScopeType.Collaborator:
                CollaboratorId = scope.ScopeId;
                return;
            default:
                throw new DomainInvariantException("Escopo da meta inválido.");
        }
    }
}
