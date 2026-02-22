using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;

namespace Bud.Server.Application.UseCases.Metrics;

public sealed class DeleteMetricCheckin(
    IMetricRepository metricRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid metricId,
        Guid checkinId,
        CancellationToken cancellationToken = default)
    {
        var checkin = await metricRepository.GetCheckinByIdAsync(checkinId, cancellationToken);
        if (checkin is null || checkin.MetricId != metricId)
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

        await metricRepository.RemoveCheckinAsync(checkin, cancellationToken);
        await unitOfWork.CommitAsync(metricRepository.SaveChangesAsync, cancellationToken);
        return Result.Success();
    }
}
