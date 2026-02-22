using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Metrics;

public sealed class CreateMetric(
    IMetricRepository metricRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Metric>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateMetricRequest request,
        CancellationToken cancellationToken = default)
    {
        var mission = await metricRepository.GetMissionByIdAsync(request.MissionId, cancellationToken);

        if (mission is null)
        {
            return Result<Metric>.NotFound("Missão não encontrada.");
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            return Result<Metric>.Forbidden("Você não tem permissão para criar métricas nesta missão.");
        }

        try
        {
            var type = request.Type.ToDomain();
            var quantitativeType = request.QuantitativeType.ToDomain();
            var unit = request.Unit.ToDomain();

            var metric = Metric.Create(
                Guid.NewGuid(),
                mission.OrganizationId,
                request.MissionId,
                request.Name,
                type);

            metric.ApplyTarget(type, quantitativeType, request.MinValue, request.MaxValue, unit, request.TargetText);

            if (request.ObjectiveId.HasValue)
            {
                var objective = await metricRepository.GetObjectiveByIdAsync(request.ObjectiveId.Value, cancellationToken);

                if (objective is null)
                {
                    return Result<Metric>.NotFound("Objetivo não encontrado.");
                }

                if (objective.MissionId != request.MissionId)
                {
                    return Result<Metric>.Failure(
                        "Objetivo deve pertencer à mesma missão.",
                        ErrorType.Validation);
                }

                metric.ObjectiveId = request.ObjectiveId.Value;
            }

            await metricRepository.AddAsync(metric, cancellationToken);
            await unitOfWork.CommitAsync(metricRepository.SaveChangesAsync, cancellationToken);

            return Result<Metric>.Success(metric);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Metric>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}
