using Bud.Server.Data;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class WorkspaceService(
    ApplicationDbContext dbContext,
    ITenantProvider tenantProvider) : IWorkspaceService
{
    public async Task<ServiceResult<Workspace>> CreateAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken = default)
    {
        var organizationExists = await dbContext.Organizations
            .AnyAsync(o => o.Id == request.OrganizationId, cancellationToken);

        if (!organizationExists)
        {
            return ServiceResult<Workspace>.NotFound("Organização não encontrada.");
        }

        if (!tenantProvider.IsGlobalAdmin)
        {
            var isOwner = await IsOrgOwnerAsync(request.OrganizationId, cancellationToken);
            if (!isOwner)
            {
                return ServiceResult<Workspace>.Forbidden("Apenas o proprietário da organização pode criar workspaces.");
            }
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
            Visibility = request.Visibility!.Value,
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

        if (!tenantProvider.IsGlobalAdmin)
        {
            var hasWriteAccess = await HasWriteAccessAsync(workspace.OrganizationId, id, cancellationToken);
            if (!hasWriteAccess)
            {
                return ServiceResult<Workspace>.Forbidden(
                    "Você não tem permissão para atualizar este workspace.");
            }
        }

        var nameExists = await dbContext.Workspaces
            .AnyAsync(w => w.OrganizationId == workspace.OrganizationId && w.Name == request.Name.Trim() && w.Id != id, cancellationToken);

        if (nameExists)
        {
            return ServiceResult<Workspace>.Failure("Já existe um workspace com este nome nesta organização.", ServiceErrorType.Conflict);
        }

        workspace.Name = request.Name.Trim();
        workspace.Visibility = request.Visibility;
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

        if (!tenantProvider.IsGlobalAdmin)
        {
            var hasWriteAccess = await HasWriteAccessAsync(workspace.OrganizationId, id, cancellationToken);
            if (!hasWriteAccess)
            {
                return ServiceResult.Forbidden(
                    "Você não tem permissão para excluir este workspace.");
            }
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

        if (!await HasReadAccessAsync(workspace, cancellationToken))
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
            query = query.Where(w => w.Name.Contains(search.Trim()));
        }

        query = await ApplyVisibilityFilterAsync(query, cancellationToken);

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

        if (!await HasReadAccessAsync(workspace, cancellationToken))
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

    private async Task<bool> HasReadAccessAsync(Workspace workspace, CancellationToken cancellationToken)
    {
        if (tenantProvider.IsGlobalAdmin)
            return true;

        if (workspace.Visibility == Visibility.Public)
            return true;

        if (await IsOrgOwnerAsync(workspace.OrganizationId, cancellationToken))
            return true;

        return await IsCollaboratorInWorkspaceAsync(workspace.Id, cancellationToken);
    }

    private async Task<bool> HasWriteAccessAsync(Guid organizationId, Guid workspaceId, CancellationToken cancellationToken)
    {
        if (await IsOrgOwnerAsync(organizationId, cancellationToken))
            return true;

        return await IsCollaboratorInWorkspaceAsync(workspaceId, cancellationToken);
    }

    private async Task<bool> IsCollaboratorInWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (tenantProvider.CollaboratorId is null)
            return false;

        return await dbContext.Collaborators
            .AsNoTracking()
            .AnyAsync(c =>
                c.Id == tenantProvider.CollaboratorId.Value &&
                c.Team.WorkspaceId == workspaceId,
                cancellationToken);
    }

    private async Task<bool> IsOrgOwnerAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        if (tenantProvider.CollaboratorId is null)
            return false;

        return await dbContext.Organizations
            .AsNoTracking()
            .AnyAsync(o =>
                o.Id == organizationId &&
                o.OwnerId == tenantProvider.CollaboratorId.Value,
                cancellationToken);
    }

    private async Task<IQueryable<Workspace>> ApplyVisibilityFilterAsync(
        IQueryable<Workspace> query, CancellationToken cancellationToken)
    {
        if (tenantProvider.IsGlobalAdmin || tenantProvider.CollaboratorId is null)
            return query;

        var collaboratorId = tenantProvider.CollaboratorId.Value;

        var isOrgOwner = tenantProvider.TenantId.HasValue &&
            await dbContext.Organizations
                .AsNoTracking()
                .AnyAsync(o =>
                    o.Id == tenantProvider.TenantId.Value &&
                    o.OwnerId == collaboratorId,
                    cancellationToken);

        if (isOrgOwner)
            return query;

        var memberWorkspaceIds = await dbContext.Collaborators
            .AsNoTracking()
            .Where(c => c.Id == collaboratorId)
            .Select(c => c.Team.WorkspaceId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return query.Where(w =>
            w.Visibility == Visibility.Public ||
            memberWorkspaceIds.Contains(w.Id));
    }
}
