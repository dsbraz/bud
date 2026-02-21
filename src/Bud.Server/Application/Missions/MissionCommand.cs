using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Infrastructure.Services;
using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Notifications;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Missions;

public sealed class MissionCommand(
    IMissionRepository missionRepository,
    IMissionScopeResolver missionScopeResolver,
    IApplicationAuthorizationGateway authorizationGateway,
    NotificationOrchestrator notificationOrchestrator)
{
    public async Task<Result<Mission>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var scopeResolution = await missionScopeResolver.ResolveScopeOrganizationIdAsync(
            request.ScopeType,
            request.ScopeId,
            ignoreQueryFilters: true,
            cancellationToken: cancellationToken);

        if (!scopeResolution.IsSuccess)
        {
            return Result<Mission>.NotFound(scopeResolution.Error ?? "Escopo não encontrado.");
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, scopeResolution.Value, cancellationToken);
        if (!canCreate)
        {
            return Result<Mission>.Forbidden("Você não tem permissão para criar missões nesta organização.");
        }

        try
        {
            var missionScope = MissionScope.Create(request.ScopeType, request.ScopeId);

            var mission = Mission.Create(
                Guid.NewGuid(),
                scopeResolution.Value,
                request.Name,
                request.Description,
                NormalizeToUtc(request.StartDate),
                NormalizeToUtc(request.EndDate),
                request.Status);

            mission.SetScope(missionScope);

            await missionRepository.AddAsync(mission, cancellationToken);
            await missionRepository.SaveChangesAsync(cancellationToken);

            await notificationOrchestrator.NotifyMissionCreatedAsync(
                mission.Id, mission.OrganizationId, cancellationToken);

            return Result<Mission>.Success(mission);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Mission>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result<Mission>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var mission = await missionRepository.GetByIdAsync(id, cancellationToken);
        if (mission is null)
        {
            return Result<Mission>.NotFound("Missão não encontrada.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return Result<Mission>.Forbidden("Você não tem permissão para atualizar missões nesta organização.");
        }

        try
        {
            mission.UpdateDetails(
                request.Name,
                request.Description,
                NormalizeToUtc(request.StartDate),
                NormalizeToUtc(request.EndDate),
                request.Status);

            var shouldUpdateScope = request.ScopeId != Guid.Empty;
            if (shouldUpdateScope)
            {
                var missionScope = MissionScope.Create(request.ScopeType, request.ScopeId);

                var scopeResolution = await missionScopeResolver.ResolveScopeOrganizationIdAsync(
                    request.ScopeType,
                    request.ScopeId,
                    cancellationToken: cancellationToken);
                if (!scopeResolution.IsSuccess)
                {
                    return Result<Mission>.NotFound(scopeResolution.Error ?? "Escopo não encontrado.");
                }

                mission.OrganizationId = scopeResolution.Value;
                mission.SetScope(missionScope);
            }

            await missionRepository.SaveChangesAsync(cancellationToken);

            await notificationOrchestrator.NotifyMissionUpdatedAsync(
                mission.Id, mission.OrganizationId, cancellationToken);

            return Result<Mission>.Success(mission);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Mission>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var mission = await missionRepository.GetByIdAsync(id, cancellationToken);
        if (mission is null)
        {
            return Result.NotFound("Missão não encontrada.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return Result.Forbidden("Você não tem permissão para excluir missões nesta organização.");
        }

        var orgId = mission.OrganizationId;
        await missionRepository.RemoveAsync(mission, cancellationToken);
        await missionRepository.SaveChangesAsync(cancellationToken);

        await notificationOrchestrator.NotifyMissionDeletedAsync(
            id, orgId, cancellationToken);

        return Result.Success();
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };
    }
}
