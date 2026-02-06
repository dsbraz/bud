using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class WorkspaceService(
    ApplicationDbContext dbContext) : IWorkspaceService
{
    public async Task<ServiceResult<Workspace>> CreateAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken = default)
    {
        var organizationExists = await dbContext.Organizations
            .AnyAsync(o => o.Id == request.OrganizationId, cancellationToken);

        if (!organizationExists)
        {
            return ServiceResult<Workspace>.NotFound("Organização não encontrada.");
        }

        var nameExists = await dbContext.Workspaces
            .AnyAsync(w => w.OrganizationId == request.OrganizationId && w.Name == request.Name.Trim(), cancellationToken);

        if (nameExists)
        {
            return ServiceResult<Workspace>.Failure("Já existe um workspace com este nome nesta organização.", ServiceErrorType.Conflict);
        }

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            OrganizationId = request.OrganizationId,
        };

        dbContext.Workspaces.Add(workspace);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<Workspace>.Success(workspace);
    }

    public async Task<ServiceResult<Workspace>> UpdateAsync(Guid id, UpdateWorkspaceRequest request, CancellationToken cancellationToken = default)
    {
        var workspace = await dbContext.Workspaces.FindAsync([id], cancellationToken);

        if (workspace is null)
        {
            return ServiceResult<Workspace>.NotFound("Workspace não encontrado.");
        }

        var nameExists = await dbContext.Workspaces
            .AnyAsync(w => w.OrganizationId == workspace.OrganizationId && w.Name == request.Name.Trim() && w.Id != id, cancellationToken);

        if (nameExists)
        {
            return ServiceResult<Workspace>.Failure("Já existe um workspace com este nome nesta organização.", ServiceErrorType.Conflict);
        }

        workspace.Name = request.Name.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<Workspace>.Success(workspace);
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspace = await dbContext.Workspaces.FindAsync([id], cancellationToken);

        if (workspace is null)
        {
            return ServiceResult.NotFound("Workspace não encontrado.");
        }

        dbContext.Workspaces.Remove(workspace);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<Workspace>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspace = await dbContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (workspace is null)
        {
            return ServiceResult<Workspace>.NotFound("Workspace não encontrado.");
        }

        return ServiceResult<Workspace>.Success(workspace);
    }

    public async Task<ServiceResult<PagedResult<Workspace>>> GetAllAsync(Guid? organizationId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var query = dbContext.Workspaces.AsNoTracking();

        if (organizationId.HasValue)
        {
            query = query.Where(w => w.OrganizationId == organizationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(w => w.Name.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(w => w.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<Workspace>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<Workspace>>.Success(result);
    }

    public async Task<ServiceResult<PagedResult<Team>>> GetTeamsAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var workspace = await dbContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (workspace is null)
        {
            return ServiceResult<PagedResult<Team>>.NotFound("Workspace não encontrado.");
        }

        var query = dbContext.Teams
            .AsNoTracking()
            .Where(t => t.WorkspaceId == id);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<Team>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<Team>>.Success(result);
    }
}
