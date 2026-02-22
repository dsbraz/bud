using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Missions;

public sealed class PatchMission(
    IMissionRepository missionRepository,
    IMissionScopeResolver missionScopeResolver,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Mission>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchMissionRequest request,
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
            var status = request.Status.ToDomain();
            var scopeType = request.ScopeType.ToDomain();

            mission.UpdateDetails(
                request.Name,
                request.Description,
                NormalizeToUtc(request.StartDate),
                NormalizeToUtc(request.EndDate),
                status);

            var shouldUpdateScope = request.ScopeId != Guid.Empty;
            if (shouldUpdateScope)
            {
                var missionScope = MissionScope.Create(scopeType, request.ScopeId);

                var scopeResolution = await missionScopeResolver.ResolveScopeOrganizationIdAsync(
                    scopeType,
                    request.ScopeId,
                    cancellationToken: cancellationToken);
                if (!scopeResolution.IsSuccess)
                {
                    return Result<Mission>.NotFound(scopeResolution.Error ?? "Escopo não encontrado.");
                }

                mission.OrganizationId = scopeResolution.Value;
                mission.SetScope(missionScope);
            }

            mission.MarkAsUpdated();
            await unitOfWork.CommitAsync(missionRepository.SaveChangesAsync, cancellationToken);

            return Result<Mission>.Success(mission);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Mission>.Failure(ex.Message, ErrorType.Validation);
        }
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
