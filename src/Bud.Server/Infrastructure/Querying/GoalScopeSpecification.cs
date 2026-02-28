using Bud.Server.Domain.Model;

namespace Bud.Server.Infrastructure.Querying;

public sealed class GoalScopeSpecification(GoalScopeType? scopeType, Guid? scopeId) : IQuerySpecification<Goal>
{
    public IQueryable<Goal> Apply(IQueryable<Goal> query)
    {
        if (!scopeType.HasValue)
        {
            return query;
        }

        if (scopeId.HasValue)
        {
            return scopeType.Value switch
            {
                GoalScopeType.Organization => query.Where(g =>
                    g.OrganizationId == scopeId.Value &&
                    g.WorkspaceId == null &&
                    g.TeamId == null &&
                    g.CollaboratorId == null),
                GoalScopeType.Workspace => query.Where(g => g.WorkspaceId == scopeId.Value),
                GoalScopeType.Team => query.Where(g => g.TeamId == scopeId.Value),
                GoalScopeType.Collaborator => query.Where(g => g.CollaboratorId == scopeId.Value),
                _ => query
            };
        }

        return scopeType.Value switch
        {
            GoalScopeType.Organization => query.Where(g =>
                g.WorkspaceId == null &&
                g.TeamId == null &&
                g.CollaboratorId == null),
            GoalScopeType.Workspace => query.Where(g => g.WorkspaceId != null),
            GoalScopeType.Team => query.Where(g => g.TeamId != null),
            GoalScopeType.Collaborator => query.Where(g => g.CollaboratorId != null),
            _ => query
        };
    }
}
