using System.Security.Claims;
using Bud.Server.Application.Abstractions;
using Bud.Server.Application.Common.Authorization;
using Bud.Server.Application.Common.Events;
using Bud.Server.Application.Common.Pipeline;
using Bud.Server.Application.Common.ReadModel;
using Bud.Server.Domain.Missions.Events;
using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Missions;

public sealed class MissionCommandUseCase(
    IMissionCommandService missionService,
    IMissionScopeResolver missionScopeResolver,
    IApplicationAuthorizationGateway authorizationGateway,
    IApplicationEntityLookup entityLookup,
    IUseCasePipeline? useCasePipeline = null,
    IDomainEventDispatcher? domainEventDispatcher = null) : IMissionCommandUseCase
{
    private readonly IUseCasePipeline _useCasePipeline = useCasePipeline ?? NoOpUseCasePipeline.Instance;
    private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher ?? NoOpDomainEventDispatcher.Instance;

    public async Task<ServiceResult<Mission>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(MissionCommandUseCase), nameof(CreateAsync)),
            async ct =>
            {
                var scopeResolution = await missionScopeResolver.ResolveScopeOrganizationIdAsync(
                    request.ScopeType,
                    request.ScopeId,
                    ignoreQueryFilters: true,
                    cancellationToken: ct);

                if (!scopeResolution.IsSuccess)
                {
                    return ServiceResult<Mission>.NotFound(scopeResolution.Error ?? "Escopo não encontrado.");
                }

                var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, scopeResolution.Value, ct);
                if (!canCreate)
                {
                    return ServiceResult<Mission>.Forbidden("Você não tem permissão para criar missões nesta organização.");
                }

                var createResult = await missionService.CreateAsync(request, ct);
                if (createResult.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new MissionCreatedDomainEvent(createResult.Value!.Id, createResult.Value.OrganizationId),
                        ct);
                }

                return createResult;
            },
            cancellationToken);
    }

    public async Task<ServiceResult<Mission>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(MissionCommandUseCase), nameof(UpdateAsync)),
            async ct =>
            {
                var mission = await entityLookup.GetMissionAsync(id, cancellationToken: ct);

                if (mission is null)
                {
                    return ServiceResult<Mission>.NotFound("Missão não encontrada.");
                }

                var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, ct);
                if (!canUpdate)
                {
                    return ServiceResult<Mission>.Forbidden("Você não tem permissão para atualizar missões nesta organização.");
                }

                var result = await missionService.UpdateAsync(id, request, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new MissionUpdatedDomainEvent(result.Value!.Id, result.Value.OrganizationId),
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
            new UseCaseExecutionContext(nameof(MissionCommandUseCase), nameof(DeleteAsync)),
            async ct =>
            {
                var mission = await entityLookup.GetMissionAsync(id, cancellationToken: ct);

                if (mission is null)
                {
                    return ServiceResult.NotFound("Missão não encontrada.");
                }

                var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, ct);
                if (!canDelete)
                {
                    return ServiceResult.Forbidden("Você não tem permissão para excluir missões nesta organização.");
                }

                var result = await missionService.DeleteAsync(id, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new MissionDeletedDomainEvent(mission.Id, mission.OrganizationId),
                        ct);
                }

                return result;
            },
            cancellationToken);
    }
}
