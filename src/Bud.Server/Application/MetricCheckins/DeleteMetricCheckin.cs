using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;

namespace Bud.Server.Application.MetricCheckins;

public sealed class DeleteMetricCheckin(
    IMetricCheckinRepository checkinRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var checkin = await checkinRepository.GetByIdAsync(id, cancellationToken);

        if (checkin is null)
        {
            return Result.NotFound("Check-in não encontrado.");
        }

        var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, checkin.OrganizationId, cancellationToken);
        if (!hasTenantAccess)
        {
            return Result.Forbidden("Você não tem permissão para excluir este check-in.");
        }

        if (!tenantProvider.IsGlobalAdmin && tenantProvider.CollaboratorId != checkin.CollaboratorId)
        {
            return Result.Forbidden("Apenas o autor pode excluir este check-in.");
        }

        await checkinRepository.RemoveAsync(checkin, cancellationToken);
        await unitOfWork.CommitAsync(checkinRepository.SaveChangesAsync, cancellationToken);

        return Result.Success();
    }
}
