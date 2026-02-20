using Bud.Server.Application.Ports;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Domain.Specifications;
using Bud.Server.Infrastructure.Persistence;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Repositories;

public sealed class TeamRepository(ApplicationDbContext dbContext) : ITeamRepository
{
    public async Task<Team?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Team?> GetByIdWithCollaboratorTeamsAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Teams.Include(t => t.CollaboratorTeams).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResult<Team>> GetAllAsync(
        Guid? workspaceId, Guid? parentTeamId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        IQueryable<Team> query = dbContext.Teams.AsNoTracking().Include(t => t.Leader);

        if (workspaceId.HasValue)
        {
            query = query.Where(t => t.WorkspaceId == workspaceId.Value);
        }

        if (parentTeamId.HasValue)
        {
            query = query.Where(t => t.ParentTeamId == parentTeamId.Value);
        }

        query = new TeamSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Team> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<PagedResult<Team>> GetSubTeamsAsync(Guid teamId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.Teams.AsNoTracking().Where(t => t.ParentTeamId == teamId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Team> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<PagedResult<Collaborator>> GetCollaboratorsAsync(Guid teamId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.Collaborators.AsNoTracking().Where(c => c.TeamId == teamId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Collaborator> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<List<CollaboratorSummary>> GetCollaboratorSummariesAsync(Guid teamId, CancellationToken ct = default)
    {
        return await dbContext.CollaboratorTeams
            .AsNoTracking()
            .Where(ct2 => ct2.TeamId == teamId)
            .Include(ct2 => ct2.Collaborator)
            .Select(ct2 => new CollaboratorSummary
            {
                Id = ct2.Collaborator.Id,
                FullName = ct2.Collaborator.FullName,
                Email = ct2.Collaborator.Email,
                Role = ct2.Collaborator.Role
            })
            .OrderBy(c => c.FullName)
            .ToListAsync(ct);
    }

    public async Task<List<CollaboratorSummary>> GetAvailableCollaboratorsAsync(
        Guid teamId, Guid organizationId, string? search, int limit, CancellationToken ct = default)
    {
        var currentCollaboratorIds = await dbContext.CollaboratorTeams
            .Where(ct2 => ct2.TeamId == teamId)
            .Select(ct2 => ct2.CollaboratorId)
            .ToListAsync(ct);

        var query = dbContext.Collaborators
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId)
            .Where(c => !currentCollaboratorIds.Contains(c.Id));

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
        => await dbContext.Teams.AnyAsync(t => t.Id == id, ct);

    public async Task<bool> HasSubTeamsAsync(Guid teamId, CancellationToken ct = default)
        => await dbContext.Teams.AnyAsync(t => t.ParentTeamId == teamId, ct);

    public async Task<bool> HasMissionsAsync(Guid teamId, CancellationToken ct = default)
        => await dbContext.Missions.AnyAsync(m => m.TeamId == teamId, ct);

    public async Task AddAsync(Team entity, CancellationToken ct = default)
        => await dbContext.Teams.AddAsync(entity, ct);

    public async Task RemoveAsync(Team entity, CancellationToken ct = default)
        => await Task.FromResult(dbContext.Teams.Remove(entity));

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);
}
