using System.Security.Claims;
using Bud.Server.Application.Common.Authorization;
using Bud.Server.Application.Common.Events;
using Bud.Server.Application.Common.Pipeline;
using Bud.Server.Application.Common.ReadModel;
using Bud.Server.Domain.MetricCheckins.Events;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.MetricCheckins;

public sealed class MetricCheckinCommandUseCase(
    IMetricCheckinService checkinService,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    IApplicationEntityLookup entityLookup,
    IUseCasePipeline? useCasePipeline = null,
    IDomainEventDispatcher? domainEventDispatcher = null) : IMetricCheckinCommandUseCase
{
    private readonly IUseCasePipeline _useCasePipeline = useCasePipeline ?? NoOpUseCasePipeline.Instance;
    private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher ?? NoOpDomainEventDispatcher.Instance;

    public async Task<ServiceResult<MetricCheckin>> CreateAsync(
        ClaimsPrincipal user,
        CreateMetricCheckinRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(MetricCheckinCommandUseCase), nameof(CreateAsync)),
            async ct =>
            {
                var metric = await entityLookup.GetMissionMetricAsync(
                    request.MissionMetricId,
                    includeMission: true,
                    cancellationToken: ct);

                if (metric is null)
                {
                    return ServiceResult<MetricCheckin>.NotFound("Métrica não encontrada.");
                }

                var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, metric.OrganizationId, ct);
                if (!hasTenantAccess)
                {
                    return ServiceResult<MetricCheckin>.Forbidden("Você não tem permissão para criar check-ins nesta métrica.");
                }

                var mission = metric.Mission;
                var hasScopeAccess = await authorizationGateway.CanAccessMissionScopeAsync(
                    user,
                    mission.WorkspaceId,
                    mission.TeamId,
                    mission.CollaboratorId,
                    ct);
                if (!hasScopeAccess)
                {
                    return ServiceResult<MetricCheckin>.Forbidden("Você não tem permissão para fazer check-in nesta métrica.");
                }

                var collaboratorId = tenantProvider.CollaboratorId;
                if (!collaboratorId.HasValue)
                {
                    return ServiceResult<MetricCheckin>.Forbidden("Colaborador não identificado.");
                }

                var collaborator = await entityLookup.GetCollaboratorAsync(collaboratorId.Value, ct);
                if (collaborator is null)
                {
                    return ServiceResult<MetricCheckin>.Forbidden("Colaborador não encontrado.");
                }

                var createResult = await checkinService.CreateAsync(request, collaboratorId.Value, ct);
                if (createResult.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new MetricCheckinCreatedDomainEvent(
                            createResult.Value!.Id,
                            createResult.Value.MissionMetricId,
                            createResult.Value.OrganizationId,
                            createResult.Value.CollaboratorId),
                        ct);
                }

                return createResult;
            },
            cancellationToken);
    }

    public async Task<ServiceResult<MetricCheckin>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMetricCheckinRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(MetricCheckinCommandUseCase), nameof(UpdateAsync)),
            async ct =>
            {
                var checkin = await entityLookup.GetMetricCheckinAsync(id, ignoreQueryFilters: true, cancellationToken: ct);

                if (checkin is null)
                {
                    return ServiceResult<MetricCheckin>.NotFound("Check-in não encontrado.");
                }

                var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, checkin.OrganizationId, ct);
                if (!hasTenantAccess)
                {
                    return ServiceResult<MetricCheckin>.Forbidden("Você não tem permissão para atualizar este check-in.");
                }

                if (!tenantProvider.IsGlobalAdmin && tenantProvider.CollaboratorId != checkin.CollaboratorId)
                {
                    return ServiceResult<MetricCheckin>.Forbidden("Apenas o autor pode editar este check-in.");
                }

                var result = await checkinService.UpdateAsync(id, request, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new MetricCheckinUpdatedDomainEvent(
                            result.Value!.Id,
                            result.Value.MissionMetricId,
                            result.Value.OrganizationId,
                            result.Value.CollaboratorId),
                        ct);
                }

                return result;
            },
            cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(MetricCheckinCommandUseCase), nameof(DeleteAsync)),
            async ct =>
            {
                var checkin = await entityLookup.GetMetricCheckinAsync(id, ignoreQueryFilters: true, cancellationToken: ct);

                if (checkin is null)
                {
                    return ServiceResult.NotFound("Check-in não encontrado.");
                }

                var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, checkin.OrganizationId, ct);
                if (!hasTenantAccess)
                {
                    return ServiceResult.Forbidden("Você não tem permissão para excluir este check-in.");
                }

                if (!tenantProvider.IsGlobalAdmin && tenantProvider.CollaboratorId != checkin.CollaboratorId)
                {
                    return ServiceResult.Forbidden("Apenas o autor pode excluir este check-in.");
                }

                var result = await checkinService.DeleteAsync(id, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new MetricCheckinDeletedDomainEvent(
                            checkin.Id,
                            checkin.MissionMetricId,
                            checkin.OrganizationId,
                            checkin.CollaboratorId),
                        ct);
                }

                return result;
            },
            cancellationToken);
    }
}
