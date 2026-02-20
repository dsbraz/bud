using Bud.Server.Application.Ports;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Domain.Specifications;
using Bud.Server.Infrastructure.Persistence;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

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

    public async Task<List<LeaderCollaborator>> GetLeadersAsync(Guid? organizationId, CancellationToken ct = default)
    {
        var query = dbContext.Collaborators
            .Include(c => c.Organization)
            .Include(c => c.Team!)
                .ThenInclude(t => t.Workspace)
            .Where(c => c.Role == CollaboratorRole.Leader);

        if (organizationId.HasValue)
        {
            query = query.Where(c => c.OrganizationId == organizationId.Value);
        }

        return await query
            .OrderBy(c => c.FullName)
            .Select(c => new LeaderCollaborator
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                TeamName = c.Team != null ? c.Team.Name : null,
                WorkspaceName = c.Team != null && c.Team.Workspace != null ? c.Team.Workspace.Name : null,
                OrganizationName = c.Organization.Name
            })
            .ToListAsync(ct);
    }

    public async Task<List<CollaboratorHierarchyNode>> GetSubordinatesAsync(
        Guid collaboratorId, int maxDepth, CancellationToken ct = default)
    {
        var allSubordinates = await dbContext.Collaborators
            .AsNoTracking()
            .Where(c => c.LeaderId != null)
            .ToListAsync(ct);

        var childrenByLeader = allSubordinates
            .GroupBy(c => c.LeaderId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.FullName).ToList());

        return BuildSubordinateTree(childrenByLeader, collaboratorId, 0, maxDepth);
    }

    public async Task<List<TeamSummary>> GetTeamsAsync(Guid collaboratorId, CancellationToken ct = default)
    {
        return await dbContext.CollaboratorTeams
            .AsNoTracking()
            .Where(ct2 => ct2.CollaboratorId == collaboratorId)
            .Include(ct2 => ct2.Team)
                .ThenInclude(t => t.Workspace)
            .Select(ct2 => new TeamSummary
            {
                Id = ct2.Team.Id,
                Name = ct2.Team.Name,
                WorkspaceName = ct2.Team.Workspace.Name
            })
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<List<TeamSummary>> GetAvailableTeamsAsync(
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
            .Select(t => new TeamSummary
            {
                Id = t.Id,
                Name = t.Name,
                WorkspaceName = t.Workspace.Name
            })
            .OrderBy(t => t.Name)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<List<CollaboratorSummary>> GetSummariesAsync(string? search, int limit, CancellationToken ct = default)
    {
        var query = dbContext.Collaborators.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = new CollaboratorSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);
        }

        return await query
            .Select(c => new CollaboratorSummary
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                Role = c.Role
            })
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

        if (leader is null || leader.Role != CollaboratorRole.Leader)
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

    private static List<CollaboratorHierarchyNode> BuildSubordinateTree(
        Dictionary<Guid, List<Collaborator>> childrenByLeader,
        Guid parentId,
        int currentDepth,
        int maxDepth)
    {
        if (currentDepth >= maxDepth || !childrenByLeader.TryGetValue(parentId, out var children))
        {
            return [];
        }

        return children.Select(c => new CollaboratorHierarchyNode
        {
            Id = c.Id,
            FullName = c.FullName,
            Initials = GetInitials(c.FullName),
            Role = c.Role == CollaboratorRole.Leader ? "Líder" : "Contribuidor individual",
            Children = BuildSubordinateTree(childrenByLeader, c.Id, currentDepth + 1, maxDepth)
        }).ToList();
    }

    private static string GetInitials(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "?";
        }

        if (parts.Length == 1)
        {
            return parts[0][..1].ToUpperInvariant();
        }

        return $"{parts[0][..1]}{parts[^1][..1]}".ToUpperInvariant();
    }
}
