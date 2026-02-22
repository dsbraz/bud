using Bud.Server.Infrastructure.Querying;
using Bud.Server.Infrastructure.Persistence;
using Bud.Server.Domain.Model;
using Microsoft.EntityFrameworkCore;

using Bud.Shared.Contracts;

namespace Bud.Server.Infrastructure.Repositories;

public sealed class CollaboratorRepository(ApplicationDbContext dbContext) : ICollaboratorRepository
{
    public async Task<Collaborator?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Collaborators.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Collaborator?> GetByIdWithCollaboratorTeamsAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Collaborators.Include(c => c.CollaboratorTeams).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<PagedResult<Collaborator>> GetAllAsync(
        Guid? teamId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.Collaborators.AsNoTracking();

        if (teamId.HasValue)
        {
            var teamCollaboratorIds = dbContext.CollaboratorTeams
                .Where(ct2 => ct2.TeamId == teamId.Value)
                .Select(ct2 => ct2.CollaboratorId);
            query = query.Where(c => teamCollaboratorIds.Contains(c.Id));
        }

        query = new CollaboratorSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Collaborator> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<List<Collaborator>> GetLeadersAsync(Guid? organizationId, CancellationToken ct = default)
    {
        var query = dbContext.Collaborators
            .AsNoTracking()
            .Include(c => c.Organization)
            .Include(c => c.Team!)
                .ThenInclude(t => t.Workspace)
            .Where(c => c.Role == Bud.Server.Domain.Model.CollaboratorRole.Leader);

        if (organizationId.HasValue)
        {
            query = query.Where(c => c.OrganizationId == organizationId.Value);
        }

        return await query
            .OrderBy(c => c.FullName)
            .ToListAsync(ct);
    }

    public async Task<List<Collaborator>> GetSubordinatesAsync(
        Guid collaboratorId, int maxDepth, CancellationToken ct = default)
    {
        var allSubordinates = await dbContext.Collaborators
            .AsNoTracking()
            .Where(c => c.LeaderId != null)
            .Include(c => c.Leader)
            .ToListAsync(ct);

        var childrenByLeader = allSubordinates
            .GroupBy(c => c.LeaderId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(c => c.FullName).ToList());

        var result = new List<Collaborator>();
        CollectSubordinates(collaboratorId, 0, maxDepth, childrenByLeader, result);
        return result;
    }

    public async Task<List<Team>> GetTeamsAsync(Guid collaboratorId, CancellationToken ct = default)
    {
        return await dbContext.CollaboratorTeams
            .AsNoTracking()
            .Where(ct2 => ct2.CollaboratorId == collaboratorId)
            .Include(ct2 => ct2.Team)
                .ThenInclude(t => t.Workspace)
            .Select(ct2 => ct2.Team)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<List<Team>> GetAvailableTeamsAsync(
        Guid collaboratorId, Guid organizationId, string? search, int limit, CancellationToken ct = default)
    {
        var currentTeamIds = await dbContext.CollaboratorTeams
            .Where(ct2 => ct2.CollaboratorId == collaboratorId)
            .Select(ct2 => ct2.TeamId)
            .ToListAsync(ct);

        var query = dbContext.Teams
            .AsNoTracking()
            .Include(t => t.Workspace)
            .Where(t => t.OrganizationId == organizationId)
            .Where(t => !currentTeamIds.Contains(t.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = new TeamSearchSpecification(search, dbContext.Database.IsNpgsql(), includeWorkspaceName: true).Apply(query);
        }

        return await query
            .OrderBy(t => t.Name)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<List<Collaborator>> GetSummariesAsync(string? search, int limit, CancellationToken ct = default)
    {
        var query = dbContext.Collaborators.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = new CollaboratorSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);
        }

        return await query
            .OrderBy(c => c.FullName)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Collaborators.AnyAsync(c => c.Id == id, ct);

    public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId, CancellationToken ct = default)
    {
        var query = dbContext.Collaborators.AsQueryable();
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return !await query.AnyAsync(c => c.Email == email, ct);
    }

    public async Task<bool> HasSubordinatesAsync(Guid collaboratorId, CancellationToken ct = default)
        => await dbContext.Collaborators.AnyAsync(c => c.LeaderId == collaboratorId, ct);

    public async Task<bool> IsOrganizationOwnerAsync(Guid collaboratorId, CancellationToken ct = default)
        => await dbContext.Organizations.AnyAsync(o => o.OwnerId == collaboratorId, ct);

    public async Task<bool> HasMissionsAsync(Guid collaboratorId, CancellationToken ct = default)
        => await dbContext.Missions.AnyAsync(m => m.CollaboratorId == collaboratorId, ct);

    public async Task<int> CountTeamsByIdsAndOrganizationAsync(List<Guid> teamIds, Guid organizationId, CancellationToken ct = default)
        => await dbContext.Teams.CountAsync(t => teamIds.Contains(t.Id) && t.OrganizationId == organizationId, ct);

    public async Task<int> CountByIdsAndOrganizationAsync(List<Guid> ids, Guid organizationId, CancellationToken ct = default)
        => await dbContext.Collaborators.CountAsync(c => ids.Contains(c.Id) && c.OrganizationId == organizationId, ct);

    public async Task<bool> IsValidLeaderAsync(Guid leaderId, Guid? requiredOrganizationId, CancellationToken ct = default)
    {
        var leader = await dbContext.Collaborators
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == leaderId, ct);

        if (leader is null || leader.Role != Bud.Server.Domain.Model.CollaboratorRole.Leader)
        {
            return false;
        }

        return !requiredOrganizationId.HasValue || leader.OrganizationId == requiredOrganizationId.Value;
    }

    public async Task AddAsync(Collaborator entity, CancellationToken ct = default)
        => await dbContext.Collaborators.AddAsync(entity, ct);

    public async Task RemoveAsync(Collaborator entity, CancellationToken ct = default)
        => await Task.FromResult(dbContext.Collaborators.Remove(entity));

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);

    private static void CollectSubordinates(
        Guid leaderId,
        int depth,
        int maxDepth,
        IReadOnlyDictionary<Guid, List<Collaborator>> childrenByLeader,
        List<Collaborator> acc)
    {
        if (depth >= maxDepth || !childrenByLeader.TryGetValue(leaderId, out var children))
        {
            return;
        }

        foreach (var child in children)
        {
            acc.Add(child);
            CollectSubordinates(child.Id, depth + 1, maxDepth, childrenByLeader, acc);
        }
    }
}
