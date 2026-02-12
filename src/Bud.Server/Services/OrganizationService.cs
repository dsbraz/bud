using Bud.Server.Data;
using Bud.Server.Domain.Common.Specifications;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bud.Server.Services;

public sealed class OrganizationService(
    ApplicationDbContext dbContext,
    IOptions<GlobalAdminSettings> globalAdminSettings) : IOrganizationService
{
    private readonly string _globalAdminEmail = globalAdminSettings.Value.Email;
    private readonly string _globalAdminOrgName = globalAdminSettings.Value.OrganizationName;

    public async Task<ServiceResult<Organization>> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validate Owner Exists
            var owner = await dbContext.Collaborators
                .FirstOrDefaultAsync(c => c.Id == request.OwnerId, cancellationToken);

            if (owner == null)
            {
                return ServiceResult<Organization>.Failure(
                    "O líder selecionado não foi encontrado.",
                    ServiceErrorType.NotFound);
            }

            // 2. Validate Owner is Leader
            if (owner.Role != CollaboratorRole.Leader)
            {
                return ServiceResult<Organization>.Failure(
                    "O proprietário da organização deve ter a função de Líder.",
                    ServiceErrorType.Validation);
            }

            // 3. Create Organization
            var organization = Organization.Create(Guid.NewGuid(), request.Name, request.OwnerId);

            dbContext.Organizations.Add(organization);
            await dbContext.SaveChangesAsync(cancellationToken);

            // 5. Load Owner for Response
            await dbContext.Entry(organization)
                .Reference(o => o.Owner)
                .LoadAsync(cancellationToken);

            return ServiceResult<Organization>.Success(organization);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<Organization>.Failure(ex.Message, ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult<Organization>> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        var organization = await dbContext.Organizations
            .Include(o => o.Owner)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (organization is null)
        {
            return ServiceResult<Organization>.NotFound("Organização não encontrada.");
        }

        // Check if organization is protected
        if (!string.IsNullOrEmpty(_globalAdminOrgName) &&
            organization.Name.Equals(_globalAdminOrgName, StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<Organization>.Failure(
                "Esta organização está protegida e não pode ser alterada.",
                ServiceErrorType.Validation);
        }

        try
        {
            // Update name
            organization.Rename(request.Name);

            // Update owner if provided
            if (request.OwnerId.HasValue && request.OwnerId.Value != Guid.Empty)
            {
                // Validate new owner exists
                var newOwner = await dbContext.Collaborators
                    .FirstOrDefaultAsync(c => c.Id == request.OwnerId.Value, cancellationToken);

                if (newOwner == null)
                {
                    return ServiceResult<Organization>.Failure(
                        "O líder selecionado não foi encontrado.",
                        ServiceErrorType.NotFound);
                }

                // Validate new owner is Leader
                if (newOwner.Role != CollaboratorRole.Leader)
                {
                    return ServiceResult<Organization>.Failure(
                        "O proprietário da organização deve ter a função de Líder.",
                        ServiceErrorType.Validation);
                }

                organization.AssignOwner(request.OwnerId.Value);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            // Reload owner for response
            await dbContext.Entry(organization)
                .Reference(o => o.Owner)
                .LoadAsync(cancellationToken);

            return ServiceResult<Organization>.Success(organization);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<Organization>.Failure(ex.Message, ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await dbContext.Organizations.FindAsync([id], cancellationToken);

        if (organization is null)
        {
            return ServiceResult.NotFound("Organização não encontrada.");
        }

        // Check if organization is protected
        if (!string.IsNullOrEmpty(_globalAdminOrgName) &&
            organization.Name.Equals(_globalAdminOrgName, StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult.Failure(
                "Esta organização está protegida e não pode ser excluída.",
                ServiceErrorType.Validation);
        }

        // Check if organization has associated workspaces
        var hasWorkspaces = await dbContext.Workspaces
            .AnyAsync(w => w.OrganizationId == id, cancellationToken);

        if (hasWorkspaces)
        {
            return ServiceResult.Failure(
                "Não é possível excluir a organização porque ela possui workspaces associados. Exclua os workspaces primeiro.",
                ServiceErrorType.Conflict);
        }

        // Check if organization has collaborators assigned
        var hasCollaborators = await dbContext.Collaborators
            .AnyAsync(c => c.OrganizationId == id, cancellationToken);

        if (hasCollaborators)
        {
            return ServiceResult.Failure(
                "Não é possível excluir a organização porque ela possui colaboradores associados. Remova os colaboradores primeiro.",
                ServiceErrorType.Conflict);
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
            ? ServiceResult<Organization>.NotFound("Organização não encontrada.")
            : ServiceResult<Organization>.Success(organization);
    }

    public async Task<ServiceResult<PagedResult<Organization>>> GetAllAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.Organizations.AsNoTracking();
        query = new OrganizationSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(o => o.Owner)
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
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var organizationExists = await dbContext.Organizations.AnyAsync(o => o.Id == id, cancellationToken);
        if (!organizationExists)
        {
            return ServiceResult<PagedResult<Workspace>>.NotFound("Organização não encontrada.");
        }

        var query = dbContext.Workspaces
            .AsNoTracking()
            .Where(w => w.OrganizationId == id);

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

    public async Task<ServiceResult<PagedResult<Collaborator>>> GetCollaboratorsAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var organizationExists = await dbContext.Organizations.AnyAsync(o => o.Id == id, cancellationToken);
        if (!organizationExists)
        {
            return ServiceResult<PagedResult<Collaborator>>.NotFound("Organização não encontrada.");
        }

        var query = dbContext.Collaborators
            .AsNoTracking()
            .Where(c => c.OrganizationId == id);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(c => c.Team)
            .OrderBy(c => c.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<Collaborator>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<Collaborator>>.Success(result);
    }
}
