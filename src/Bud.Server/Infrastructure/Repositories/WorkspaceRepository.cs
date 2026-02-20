using Bud.Server.Application.Ports;
using Bud.Server.Domain.Specifications;
using Bud.Server.Infrastructure.Persistence;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Repositories;

public sealed class WorkspaceRepository(ApplicationDbContext dbContext) : IWorkspaceRepository
{
    public async Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Workspaces.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<PagedResult<Workspace>> GetAllAsync(
        Guid? organizationId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.Workspaces.AsNoTracking();

        if (organizationId.HasValue)
        {
            query = query.Where(w => w.OrganizationId == organizationId.Value);
        }

        query = new WorkspaceSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(w => w.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Workspace> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<PagedResult<Team>> GetTeamsAsync(Guid workspaceId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.Teams
            .AsNoTracking()
            .Where(t => t.WorkspaceId == workspaceId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Team> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Workspaces.AnyAsync(w => w.Id == id, ct);

    public async Task<bool> IsNameUniqueAsync(Guid organizationId, string name, Guid? excludeId = null, CancellationToken ct = default)
    {
        var normalizedName = name.Trim();
        var query = dbContext.Workspaces.Where(w => w.OrganizationId == organizationId && w.Name == normalizedName);
        if (excludeId.HasValue)
        {
            query = query.Where(w => w.Id != excludeId.Value);
        }
        return !await query.AnyAsync(ct);
    }

    public async Task<bool> HasMissionsAsync(Guid workspaceId, CancellationToken ct = default)
        => await dbContext.Missions.AnyAsync(m => m.WorkspaceId == workspaceId, ct);

    public async Task AddAsync(Workspace entity, CancellationToken ct = default)
        => await dbContext.Workspaces.AddAsync(entity, ct);

    public Task RemoveAsync(Workspace entity, CancellationToken ct = default)
    {
        dbContext.Workspaces.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);
}
