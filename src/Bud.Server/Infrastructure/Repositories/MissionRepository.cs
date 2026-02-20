using Bud.Server.Domain.Specifications;
using Bud.Server.Infrastructure.Persistence;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Repositories;

public sealed class MissionRepository(ApplicationDbContext dbContext) : IMissionRepository
{
    public async Task<Mission?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Missions.FindAsync([id], ct);

    public async Task<Mission?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Missions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<PagedResult<Mission>> GetAllAsync(
        MissionScopeType? scopeType, Guid? scopeId, string? search,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.Missions.AsNoTracking();

        query = new MissionScopeSpecification(scopeType, scopeId).Apply(query);
        query = new MissionSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Mission>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Mission>> GetMyMissionsAsync(
        Guid collaboratorId, Guid organizationId,
        List<Guid> teamIds, List<Guid> workspaceIds, string? search,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.Missions
            .AsNoTracking()
            .Where(m =>
                m.CollaboratorId == collaboratorId ||
                (m.TeamId.HasValue && teamIds.Contains(m.TeamId.Value)) ||
                (m.WorkspaceId.HasValue && workspaceIds.Contains(m.WorkspaceId.Value)) ||
                (m.OrganizationId == organizationId && m.WorkspaceId == null && m.TeamId == null && m.CollaboratorId == null));

        query = new MissionSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(m => m.StartDate)
            .ThenBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Mission>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Collaborator?> FindCollaboratorForMyMissionsAsync(Guid collaboratorId, CancellationToken ct = default)
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

    public async Task<PagedResult<MissionMetric>> GetMetricsAsync(Guid missionId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.MissionMetrics
            .AsNoTracking()
            .Where(metric => metric.MissionId == missionId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(metric => metric.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<MissionMetric>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Missions.AnyAsync(m => m.Id == id, ct);

    public async Task AddAsync(Mission entity, CancellationToken ct = default)
        => await dbContext.Missions.AddAsync(entity, ct);

    public async Task RemoveAsync(Mission entity, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        dbContext.Missions.Remove(entity);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };
    }
}
