using Bud.Shared.Domain;

namespace Bud.Server.Domain.Specifications;

public sealed class MissionScopeSpecification(MissionScopeType? scopeType, Guid? scopeId) : IQuerySpecification<Mission>
{
    public IQueryable<Mission> Apply(IQueryable<Mission> query)
    {
        if (!scopeType.HasValue)
        {
            return query;
        }

        if (scopeId.HasValue)
        {
            return scopeType.Value switch
            {
                MissionScopeType.Organization => query.Where(m =>
                    m.OrganizationId == scopeId.Value &&
                    m.WorkspaceId == null &&
                    m.TeamId == null &&
                    m.CollaboratorId == null),
                MissionScopeType.Workspace => query.Where(m => m.WorkspaceId == scopeId.Value),
                MissionScopeType.Team => query.Where(m => m.TeamId == scopeId.Value),
                MissionScopeType.Collaborator => query.Where(m => m.CollaboratorId == scopeId.Value),
                _ => query
            };
        }

        return scopeType.Value switch
        {
            MissionScopeType.Organization => query.Where(m =>
                m.WorkspaceId == null &&
                m.TeamId == null &&
                m.CollaboratorId == null),
            MissionScopeType.Workspace => query.Where(m => m.WorkspaceId != null),
            MissionScopeType.Team => query.Where(m => m.TeamId != null),
            MissionScopeType.Collaborator => query.Where(m => m.CollaboratorId != null),
            _ => query
        };
    }
}
