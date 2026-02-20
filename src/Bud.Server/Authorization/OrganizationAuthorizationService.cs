using Bud.Server.Infrastructure.Persistence;
using Bud.Server.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Bud.Server.Application.Common;

namespace Bud.Server.Authorization;

public sealed class OrganizationAuthorizationService(
    ApplicationDbContext dbContext,
    ITenantProvider tenantProvider) : IOrganizationAuthorizationService
{
    public async Task<Result> RequireOrgOwnerAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        // Global admin sempre tem acesso
        if (tenantProvider.IsGlobalAdmin)
        {
            return Result.Success();
        }

        if (tenantProvider.CollaboratorId is null)
        {
            return Result.Forbidden("Colaborador não identificado.");
        }

        var isOwner = await dbContext.Organizations
            .AnyAsync(o =>
                o.Id == organizationId &&
                o.OwnerId == tenantProvider.CollaboratorId.Value,
                cancellationToken);

        return isOwner
            ? Result.Success()
            : Result.Forbidden("Apenas o proprietário da organização pode realizar esta ação.");
    }

    public async Task<Result> RequireWriteAccessAsync(
        Guid organizationId,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.IsGlobalAdmin)
        {
            return Result.Success();
        }

        if (tenantProvider.CollaboratorId is null)
        {
            return Result.Forbidden("Colaborador não identificado.");
        }

        // Implementar lógica de write access conforme regras de negócio
        var isOwner = await dbContext.Organizations
            .AnyAsync(o =>
                o.Id == organizationId &&
                o.OwnerId == tenantProvider.CollaboratorId.Value,
                cancellationToken);

        return isOwner
            ? Result.Success()
            : Result.Forbidden("Você não tem permissão de escrita nesta organização.");
    }
}
