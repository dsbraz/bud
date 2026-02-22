using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Metrics;

public sealed class DeleteMetric(
    IMetricRepository metricRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var metricForAuthorization = await metricRepository.GetByIdAsync(id, cancellationToken);

        if (metricForAuthorization is null)
        {
            return Result.NotFound("Métrica da missão não encontrada.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(
            user,
            metricForAuthorization.OrganizationId,
            cancellationToken);
        if (!canDelete)
        {
            return Result.Forbidden("Você não tem permissão para excluir métricas nesta missão.");
        }

        var metric = await metricRepository.GetByIdTrackingAsync(id, cancellationToken);
        if (metric is null)
        {
            return Result.NotFound("Métrica da missão não encontrada.");
        }

        await metricRepository.RemoveAsync(metric, cancellationToken);
        await unitOfWork.CommitAsync(metricRepository.SaveChangesAsync, cancellationToken);

        return Result.Success();
    }
}

