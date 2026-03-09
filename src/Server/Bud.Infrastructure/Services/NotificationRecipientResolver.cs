using Bud.Infrastructure.Persistence;
using Bud.Application.Ports;
using Microsoft.EntityFrameworkCore;

namespace Bud.Infrastructure.Services;

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
            // Responsible collaborator + their leader
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
        else
        {
            // No responsible: notify all collaborators in the org
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

    public async Task<string?> ResolveCollaboratorNameAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Collaborators
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.Id == collaboratorId)
            .Select(c => c.FullName)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
