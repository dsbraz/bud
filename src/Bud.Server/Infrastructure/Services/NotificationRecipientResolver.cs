using Bud.Server.Application.Notifications;
using Bud.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Services;

public sealed class NotificationRecipientResolver(ApplicationDbContext dbContext) : INotificationRecipientResolver
{
    public async Task<List<Guid>> ResolveMissionRecipientsAsync(
        Guid missionId,
        Guid organizationId,
        Guid? excludeCollaboratorId = null,
        CancellationToken cancellationToken = default)
    {
        var mission = await dbContext.Missions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == missionId && m.OrganizationId == organizationId, cancellationToken);

        if (mission is null)
        {
            return [];
        }

        List<Guid> recipientIds;

        if (mission.CollaboratorId.HasValue)
        {
            // Collaborator scope: the assigned collaborator + their leader
            recipientIds = [mission.CollaboratorId.Value];

            var leader = await dbContext.Collaborators
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.Id == mission.CollaboratorId.Value && c.OrganizationId == organizationId)
                .Select(c => c.LeaderId)
                .FirstOrDefaultAsync(cancellationToken);

            if (leader.HasValue)
            {
                recipientIds.Add(leader.Value);
            }
        }
        else if (mission.TeamId.HasValue)
        {
            // Team scope: all collaborators in the team
            recipientIds = await dbContext.CollaboratorTeams
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(ct => ct.TeamId == mission.TeamId.Value)
                .Select(ct => ct.CollaboratorId)
                .ToListAsync(cancellationToken);
        }
        else if (mission.WorkspaceId.HasValue)
        {
            // Workspace scope: all collaborators in teams of the workspace
            var teamIds = await dbContext.Teams
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(t => t.WorkspaceId == mission.WorkspaceId.Value && t.OrganizationId == organizationId)
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

    public async Task<Guid?> ResolveMissionIdFromMetricAsync(
        Guid missionMetricId,
        CancellationToken cancellationToken = default)
    {
        var missionId = await dbContext.MissionMetrics
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(mm => mm.Id == missionMetricId)
            .Select(mm => mm.MissionId)
            .FirstOrDefaultAsync(cancellationToken);

        return missionId == Guid.Empty ? null : missionId;
    }
}
