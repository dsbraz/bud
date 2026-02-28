using Bud.Server.Infrastructure.Querying;
using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Domain.Model;
using Bud.Server.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Repositories;

public sealed class GoalRepository(ApplicationDbContext dbContext) : IGoalRepository
{
    public async Task<Goal?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Goals.FindAsync([id], ct);

    public async Task<Goal?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Goals
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<PagedResult<Goal>> GetAllAsync(
        Guid? parentId, GoalScopeType? scopeType, Guid? scopeId, string? search,
        int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.Goals.AsNoTracking();

        if (parentId.HasValue)
        {
            query = query.Where(g => g.ParentId == parentId.Value);
        }
        else
        {
            // When no parent is specified, default to root goals
            query = query.Where(g => g.ParentId == null);
        }

        query = new GoalScopeSpecification(scopeType, scopeId).Apply(query);
        query = new GoalSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

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

    public async Task<PagedResult<Goal>> GetMyGoalsAsync(
        Guid collaboratorId, Guid organizationId,
        List<Guid> teamIds, List<Guid> workspaceIds, string? search,
        int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.Goals
            .AsNoTracking()
            .Where(g =>
                g.CollaboratorId == collaboratorId ||
                (g.TeamId.HasValue && teamIds.Contains(g.TeamId.Value)) ||
                (g.WorkspaceId.HasValue && workspaceIds.Contains(g.WorkspaceId.Value)) ||
                (g.OrganizationId == organizationId && g.WorkspaceId == null && g.TeamId == null && g.CollaboratorId == null));

        query = new GoalSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(g => g.StartDate)
            .ThenBy(g => g.Name)
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

    public async Task<Collaborator?> FindCollaboratorForMyGoalsAsync(Guid collaboratorId, CancellationToken ct = default)
    {
        return await dbContext.Collaborators
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(c => c.Team!)
                .ThenInclude(t => t.Workspace!)
                    .ThenInclude(w => w.Organization)
            .FirstOrDefaultAsync(c => c.Id == collaboratorId, ct);
    }

    public async Task<List<Guid>> GetCollaboratorTeamIdsAsync(Guid collaboratorId, Guid? primaryTeamId, CancellationToken ct = default)
    {
        var additionalTeamIds = await dbContext.CollaboratorTeams
            .AsNoTracking()
            .Where(ct2 => ct2.CollaboratorId == collaboratorId)
            .Select(ct2 => ct2.TeamId)
            .ToListAsync(ct);

        var allTeamIds = new HashSet<Guid>(additionalTeamIds);
        if (primaryTeamId.HasValue)
        {
            allTeamIds.Add(primaryTeamId.Value);
        }

        return allTeamIds.ToList();
    }

    public async Task<List<Guid>> GetWorkspaceIdsForTeamsAsync(List<Guid> teamIds, CancellationToken ct = default)
    {
        if (teamIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Teams
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => teamIds.Contains(t.Id))
            .Select(t => t.WorkspaceId)
            .Distinct()
            .ToListAsync(ct);
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
}
