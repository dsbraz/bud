using Bud.Server.Data;
using Bud.Server.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public interface IOrganizationAuthorizationService
{
    Task<ServiceResult> RequireOrgOwnerAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<ServiceResult> RequireWriteAccessAsync(Guid organizationId, Guid resourceId, CancellationToken cancellationToken = default);
}

public sealed class OrganizationAuthorizationService(
    ApplicationDbContext dbContext,
    ITenantProvider tenantProvider) : IOrganizationAuthorizationService
{
    public async Task<ServiceResult> RequireOrgOwnerAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        // Global admin sempre tem acesso
        if (tenantProvider.IsGlobalAdmin)
        {
            return ServiceResult.Success();
        }

        if (tenantProvider.CollaboratorId is null)
        {
            return ServiceResult.Forbidden("Colaborador não identificado.");
        }

        var isOwner = await dbContext.Organizations
            .AnyAsync(o =>
                o.Id == organizationId &&
                o.OwnerId == tenantProvider.CollaboratorId.Value,
                cancellationToken);

        return isOwner
            ? ServiceResult.Success()
            : ServiceResult.Forbidden("Apenas o proprietário da organização pode realizar esta ação.");
    }

    public async Task<ServiceResult> RequireWriteAccessAsync(
        Guid organizationId,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.IsGlobalAdmin)
        {
            return ServiceResult.Success();
        }

        if (tenantProvider.CollaboratorId is null)
        {
            return ServiceResult.Forbidden("Colaborador não identificado.");
        }

        // Implementar lógica de write access conforme regras de negócio
        var isOwner = await dbContext.Organizations
            .AnyAsync(o =>
                o.Id == organizationId &&
                o.OwnerId == tenantProvider.CollaboratorId.Value,
                cancellationToken);

        return isOwner
            ? ServiceResult.Success()
            : ServiceResult.Forbidden("Você não tem permissão de escrita nesta organização.");
    }
}
