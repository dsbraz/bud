using Bud.Infrastructure.Persistence;
using Bud.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Bud.Application.Common;

namespace Bud.Infrastructure.Authorization;

public sealed class OrganizationAuthorizationService(
    ApplicationDbContext dbContext,
    ITenantProvider tenantProvider) : IOrganizationAuthorizationService
{
    public async Task<Result> RequireOrgOwnerAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (tenantProvider.IsGlobalAdmin)
        {
            return Result.Success();
        }

        if (tenantProvider.CollaboratorId is null)
        {
            return Result.Forbidden("Colaborador não identificado.");
        }

        var isMember = await dbContext.Collaborators
            .AnyAsync(c =>
                c.OrganizationId == organizationId &&
                c.Id == tenantProvider.CollaboratorId.Value,
                cancellationToken);

        return isMember
            ? Result.Success()
            : Result.Forbidden("Você não tem acesso a esta organização.");
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

        var isMember = await dbContext.Collaborators
            .AnyAsync(c =>
                c.OrganizationId == organizationId &&
                c.Id == tenantProvider.CollaboratorId.Value,
                cancellationToken);

        return isMember
            ? Result.Success()
            : Result.Forbidden("Você não tem permissão de escrita nesta organização.");
    }
}
