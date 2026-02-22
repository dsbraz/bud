using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Metrics;

public sealed class PatchMetric(
    IMetricRepository metricRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Metric>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchMetricRequest request,
        CancellationToken cancellationToken = default)
    {
        var metricForAuthorization = await metricRepository.GetByIdAsync(id, cancellationToken);

        if (metricForAuthorization is null)
        {
            return Result<Metric>.NotFound("Métrica da missão não encontrada.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(
            user,
            metricForAuthorization.OrganizationId,
            cancellationToken);
        if (!canUpdate)
        {
            return Result<Metric>.Forbidden("Você não tem permissão para atualizar métricas nesta missão.");
        }

        var metric = await metricRepository.GetByIdTrackingAsync(id, cancellationToken);
        if (metric is null)
        {
            return Result<Metric>.NotFound("Métrica da missão não encontrada.");
        }

        try
        {
            var type = request.Type.ToDomain();
            var quantitativeType = request.QuantitativeType.ToDomain();
            var unit = request.Unit.ToDomain();

            metric.UpdateDefinition(request.Name, type);
            metric.ApplyTarget(type, quantitativeType, request.MinValue, request.MaxValue, unit, request.TargetText);

            await unitOfWork.CommitAsync(metricRepository.SaveChangesAsync, cancellationToken);

            return Result<Metric>.Success(metric);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Metric>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

