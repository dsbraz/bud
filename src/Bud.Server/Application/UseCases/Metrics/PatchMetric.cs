using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Metrics;

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
            var type = request.Type.HasValue ? request.Type.Value.ToDomain() : metric.Type;
            var quantitativeType = request.QuantitativeType.HasValue
                ? request.QuantitativeType.Value.ToDomain()
                : metric.QuantitativeType;
            var unit = request.Unit.HasValue ? request.Unit.Value.ToDomain() : metric.Unit;
            var name = request.Name.HasValue ? (request.Name.Value ?? metric.Name) : metric.Name;
            var minValue = request.MinValue.HasValue ? request.MinValue.Value : metric.MinValue;
            var maxValue = request.MaxValue.HasValue ? request.MaxValue.Value : metric.MaxValue;
            var targetText = request.TargetText.HasValue ? request.TargetText.Value : metric.TargetText;

            metric.UpdateDefinition(name, type);
            metric.ApplyTarget(type, quantitativeType, minValue, maxValue, unit, targetText);

            await unitOfWork.CommitAsync(metricRepository.SaveChangesAsync, cancellationToken);

            return Result<Metric>.Success(metric);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Metric>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}
