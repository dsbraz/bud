using Bud.Server.Application.Common;
using Bud.Server.Application.Ports;
using Bud.Server.Domain.Specifications;
using Bud.Server.Infrastructure.Persistence;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Repositories;

public sealed class OrganizationRepository(ApplicationDbContext dbContext) : IOrganizationRepository
{
    public async Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Organization?> GetByIdWithOwnerAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Organizations
            .Include(o => o.Owner)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<PagedResult<Organization>> GetAllAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.Organizations.AsNoTracking();
        query = new OrganizationSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await query.CountAsync(ct);
        var items = await query
            .Include(o => o.Owner)
            .OrderBy(o => o.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Organization>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Workspace>> GetWorkspacesAsync(
        Guid organizationId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.Workspaces
            .AsNoTracking()
            .Where(w => w.OrganizationId == organizationId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(w => w.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Workspace>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Collaborator>> GetCollaboratorsAsync(
        Guid organizationId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.Collaborators
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId);

        var total = await query.CountAsync(ct);
        var items = await query
            .Include(c => c.Team)
            .OrderBy(c => c.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Collaborator>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Organizations.AnyAsync(o => o.Id == id, ct);

    public async Task<bool> HasWorkspacesAsync(Guid organizationId, CancellationToken ct = default)
        => await dbContext.Workspaces.AnyAsync(w => w.OrganizationId == organizationId, ct);

    public async Task<bool> HasCollaboratorsAsync(Guid organizationId, CancellationToken ct = default)
        => await dbContext.Collaborators.AnyAsync(c => c.OrganizationId == organizationId, ct);

    public async Task AddAsync(Organization entity, CancellationToken ct = default)
        => await dbContext.Organizations.AddAsync(entity, ct);

    public Task RemoveAsync(Organization entity, CancellationToken ct = default)
    {
        dbContext.Organizations.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);
}
