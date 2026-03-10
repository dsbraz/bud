using Bud.Infrastructure.Persistence;
using Bud.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Bud.Infrastructure.Goals;

public sealed class GoalRepository(ApplicationDbContext dbContext) : IGoalRepository
{
    public async Task<Goal?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Goals.FindAsync([id], ct);

    public async Task<Goal?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Goals
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<PagedResult<Goal>> GetAllAsync(
        GoalFilter? filter, Guid? collaboratorId, string? search,
        int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.Goals.AsNoTracking();

        query = new GoalSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        if (filter is GoalFilter.Mine && collaboratorId.HasValue)
        {
            var rootIds = await FindRootGoalIdsForCollaboratorsAsync(
                [collaboratorId.Value], ct);

            query = query.Where(g => g.ParentId == null && rootIds.Contains(g.Id));
        }
        else if (filter is GoalFilter.MyTeam && collaboratorId.HasValue)
        {
            var teamCollaboratorIds = await GetTeamCollaboratorIdsAsync(
                collaboratorId.Value, ct);

            var rootIds = await FindRootGoalIdsForCollaboratorsAsync(
                teamCollaboratorIds, ct);

            query = query.Where(g => g.ParentId == null && rootIds.Contains(g.Id));
        }
        else
        {
            // GoalFilter.All or no filter — just root goals with tenant filter
            query = query.Where(g => g.ParentId == null);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(g => g.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Goal>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Goal>> GetChildrenAsync(Guid parentId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.Goals
            .AsNoTracking()
            .Where(g => g.ParentId == parentId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(g => g.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Goal>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Indicator>> GetIndicatorsAsync(Guid goalId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.Indicators
            .AsNoTracking()
            .Where(i => i.GoalId == goalId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(i => i.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Indicator>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Goals.AnyAsync(g => g.Id == id, ct);

    public async Task AddAsync(Goal entity, CancellationToken ct = default)
        => await dbContext.Goals.AddAsync(entity, ct);

    public Task RemoveAsync(Goal entity, CancellationToken ct = default)
    {
        dbContext.Goals.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);

    /// <summary>
    /// Two-pass: loads all goals (Id, ParentId, CollaboratorId), finds which ones
    /// have any of the given collaboratorIds in their subtree, and walks up to roots.
    /// </summary>
    private async Task<HashSet<Guid>> FindRootGoalIdsForCollaboratorsAsync(
        HashSet<Guid> collaboratorIds, CancellationToken ct)
    {
        var allGoals = await dbContext.Goals
            .AsNoTracking()
            .Select(g => new { g.Id, g.ParentId, g.CollaboratorId })
            .ToListAsync(ct);

        var parentMap = allGoals.ToDictionary(g => g.Id, g => g.ParentId);

        var rootIds = new HashSet<Guid>();
        foreach (var goal in allGoals)
        {
            if (goal.CollaboratorId.HasValue && collaboratorIds.Contains(goal.CollaboratorId.Value))
            {
                // Walk up to root
                var current = goal.Id;
                while (parentMap.TryGetValue(current, out var parentId) && parentId.HasValue)
                {
                    current = parentId.Value;
                }
                rootIds.Add(current);
            }
        }

        return rootIds;
    }

    /// <summary>
    /// Gets all collaborator IDs from the same teams as the given collaborator.
    /// </summary>
    private async Task<HashSet<Guid>> GetTeamCollaboratorIdsAsync(
        Guid collaboratorId, CancellationToken ct)
    {
        // Get collaborator's primary team
        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .Where(c => c.Id == collaboratorId)
            .Select(c => new { c.TeamId })
            .FirstOrDefaultAsync(ct);

        // Get all team IDs for this collaborator (primary + additional)
        var additionalTeamIds = await dbContext.CollaboratorTeams
            .AsNoTracking()
            .Where(ct2 => ct2.CollaboratorId == collaboratorId)
            .Select(ct2 => ct2.TeamId)
            .ToListAsync(ct);

        var allTeamIds = new HashSet<Guid>(additionalTeamIds);
        if (collaborator?.TeamId.HasValue == true)
        {
            allTeamIds.Add(collaborator.TeamId.Value);
        }

        if (allTeamIds.Count == 0)
        {
            return [collaboratorId];
        }

        // Get all collaborators from those teams (primary team members)
        var teamMemberIds = await dbContext.Collaborators
            .AsNoTracking()
            .Where(c => c.TeamId.HasValue && allTeamIds.Contains(c.TeamId.Value))
            .Select(c => c.Id)
            .ToListAsync(ct);

        // Also get additional team members
        var additionalMemberIds = await dbContext.CollaboratorTeams
            .AsNoTracking()
            .Where(ct2 => allTeamIds.Contains(ct2.TeamId))
            .Select(ct2 => ct2.CollaboratorId)
            .ToListAsync(ct);

        var result = new HashSet<Guid>(teamMemberIds);
        result.UnionWith(additionalMemberIds);
        result.Add(collaboratorId);

        return result;
    }
}
