using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Application.Ports;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Services;

public sealed class NotificationRecipientResolver(ApplicationDbContext dbContext) : INotificationRecipientResolver
{
    public async Task<List<Guid>> ResolveGoalRecipientsAsync(
        Guid goalId,
        Guid organizationId,
        Guid? excludeCollaboratorId = null,
        CancellationToken cancellationToken = default)
    {
        var goal = await dbContext.Goals
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == goalId && g.OrganizationId == organizationId, cancellationToken);

        if (goal is null)
        {
            return [];
        }

        List<Guid> recipientIds;

        if (goal.CollaboratorId.HasValue)
        {
            // Collaborator scope: the assigned collaborator + their leader
            recipientIds = [goal.CollaboratorId.Value];

            var leader = await dbContext.Collaborators
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.Id == goal.CollaboratorId.Value && c.OrganizationId == organizationId)
                .Select(c => c.LeaderId)
                .FirstOrDefaultAsync(cancellationToken);

            if (leader.HasValue)
            {
                recipientIds.Add(leader.Value);
            }
        }
        else if (goal.TeamId.HasValue)
        {
            // Team scope: all collaborators in the team
            recipientIds = await dbContext.CollaboratorTeams
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(ct => ct.TeamId == goal.TeamId.Value)
                .Select(ct => ct.CollaboratorId)
                .ToListAsync(cancellationToken);
        }
        else if (goal.WorkspaceId.HasValue)
        {
            // Workspace scope: all collaborators in teams of the workspace
            var teamIds = await dbContext.Teams
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(t => t.WorkspaceId == goal.WorkspaceId.Value && t.OrganizationId == organizationId)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            recipientIds = await dbContext.CollaboratorTeams
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(ct => teamIds.Contains(ct.TeamId))
                .Select(ct => ct.CollaboratorId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }
        else
        {
            // Organization scope: all collaborators in the org
            recipientIds = await dbContext.Collaborators
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.OrganizationId == organizationId)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
        }

        if (excludeCollaboratorId.HasValue)
        {
            recipientIds.Remove(excludeCollaboratorId.Value);
        }

        return recipientIds.Distinct().ToList();
    }

    public async Task<Guid?> ResolveGoalIdFromIndicatorAsync(
        Guid indicatorId,
        CancellationToken cancellationToken = default)
    {
        var goalId = await dbContext.Indicators
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(i => i.Id == indicatorId)
            .Select(i => i.GoalId)
            .FirstOrDefaultAsync(cancellationToken);

        return goalId == Guid.Empty ? null : goalId;
    }
}
