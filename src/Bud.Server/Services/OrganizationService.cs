using Bud.Server.Data;
using Bud.Server.MultiTenancy;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bud.Server.Services;

public sealed class OrganizationService(
    ApplicationDbContext dbContext,
    IOptions<GlobalAdminSettings> globalAdminSettings,
    ITenantProvider tenantProvider) : IOrganizationService
{
    private readonly string _globalAdminEmail = globalAdminSettings.Value.Email;

    public async Task<ServiceResult<Organization>> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Global Admin Authorization Check
        var normalizedEmail = request.UserEmail.Trim().ToLowerInvariant();
        var isGlobalAdmin = normalizedEmail.Equals(_globalAdminEmail, StringComparison.OrdinalIgnoreCase);

        if (!isGlobalAdmin)
        {
            return ServiceResult<Organization>.Failure(
                "Apenas administradores globais podem criar organizações.",
                ServiceErrorType.Validation);
        }

        // 2. Validate Owner Exists
        var owner = await dbContext.Collaborators
            .FirstOrDefaultAsync(c => c.Id == request.OwnerId, cancellationToken);

        if (owner == null)
        {
            return ServiceResult<Organization>.Failure(
                "O líder selecionado não foi encontrado.",
                ServiceErrorType.NotFound);
        }

        // 3. Validate Owner is Leader
        if (owner.Role != CollaboratorRole.Leader)
        {
            return ServiceResult<Organization>.Failure(
                "O proprietário da organização deve ter a função de Líder.",
                ServiceErrorType.Validation);
        }

        // 4. Create Organization
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            OwnerId = request.OwnerId
        };

        dbContext.Organizations.Add(organization);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 5. Load Owner for Response
        await dbContext.Entry(organization)
            .Reference(o => o.Owner)
            .LoadAsync(cancellationToken);

        return ServiceResult<Organization>.Success(organization);
    }

    public async Task<ServiceResult<Organization>> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        var organization = await dbContext.Organizations.FindAsync([id], cancellationToken);

        if (organization is null)
        {
            return ServiceResult<Organization>.NotFound("Organization not found.");
        }

        organization.Name = request.Name.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<Organization>.Success(organization);
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await dbContext.Organizations.FindAsync([id], cancellationToken);

        if (organization is null)
        {
            return ServiceResult.NotFound("Organization not found.");
        }

        dbContext.Organizations.Remove(organization);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<Organization>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return organization is null
            ? ServiceResult<Organization>.NotFound("Organization not found.")
            : ServiceResult<Organization>.Success(organization);
    }

    public async Task<ServiceResult<PagedResult<Organization>>> GetAllAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var query = dbContext.Organizations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o => o.Name.Contains(search.Trim()));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(o => o.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<Organization>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<Organization>>.Success(result);
    }

    public async Task<ServiceResult<PagedResult<Workspace>>> GetWorkspacesAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var organizationExists = await dbContext.Organizations.AnyAsync(o => o.Id == id, cancellationToken);
        if (!organizationExists)
        {
            return ServiceResult<PagedResult<Workspace>>.NotFound("Organization not found.");
        }

        var query = dbContext.Workspaces
            .AsNoTracking()
            .Where(w => w.OrganizationId == id);

        // Visibility filtering
        if (!tenantProvider.IsGlobalAdmin && tenantProvider.CollaboratorId.HasValue)
        {
            var collaboratorId = tenantProvider.CollaboratorId.Value;
            var isOrgOwner = await dbContext.Organizations
                .AsNoTracking()
                .AnyAsync(o => o.Id == id && o.OwnerId == collaboratorId, cancellationToken);

            if (!isOrgOwner)
            {
                var memberWorkspaceIds = await dbContext.Collaborators
                    .AsNoTracking()
                    .Where(c => c.Id == collaboratorId && c.Team != null)
                    .Select(c => c.Team!.WorkspaceId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                query = query.Where(w =>
                    w.Visibility == Visibility.Public ||
                    memberWorkspaceIds.Contains(w.Id));
            }
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
}
