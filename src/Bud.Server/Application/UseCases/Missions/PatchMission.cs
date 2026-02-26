using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Missions;

public sealed partial class PatchMission(
    IMissionRepository missionRepository,
    IMissionScopeResolver missionScopeResolver,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<PatchMission> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Mission>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchMissionRequest request,
        CancellationToken cancellationToken = default)
    {
        LogPatchingMission(logger, id);

        var mission = await missionRepository.GetByIdAsync(id, cancellationToken);
        if (mission is null)
        {
            LogMissionPatchFailed(logger, id, "Not found");
            return Result<Mission>.NotFound("Missão não encontrada.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            LogMissionPatchFailed(logger, id, "Forbidden");
            return Result<Mission>.Forbidden("Você não tem permissão para atualizar missões nesta organização.");
        }

        try
        {
            var status = request.Status.HasValue ? request.Status.Value : mission.Status;
            var scopeType = request.ScopeType.HasValue
                ? request.ScopeType.Value
                : mission.WorkspaceId.HasValue
                    ? MissionScopeType.Workspace
                    : mission.TeamId.HasValue
                        ? MissionScopeType.Team
                        : mission.CollaboratorId.HasValue
                            ? MissionScopeType.Collaborator
                            : MissionScopeType.Organization;
            var name = request.Name.HasValue ? (request.Name.Value ?? mission.Name) : mission.Name;
            var description = request.Description.HasValue ? request.Description.Value : mission.Description;
            var startDate = request.StartDate.HasValue ? request.StartDate.Value : mission.StartDate;
            var endDate = request.EndDate.HasValue ? request.EndDate.Value : mission.EndDate;

            mission.UpdateDetails(
                name,
                description,
                NormalizeToUtc(startDate),
                NormalizeToUtc(endDate),
                status);

            var shouldUpdateScope = request.ScopeId.HasValue && request.ScopeId.Value != Guid.Empty;
            if (shouldUpdateScope)
            {
                var scopeId = request.ScopeId.Value;
                var missionScope = MissionScope.Create(scopeType, scopeId);

                var scopeResolution = await missionScopeResolver.ResolveScopeOrganizationIdAsync(
                    scopeType,
                    scopeId,
                    cancellationToken: cancellationToken);
                if (!scopeResolution.IsSuccess)
                {
                    LogMissionPatchFailed(logger, id, scopeResolution.Error ?? "Escopo não encontrado.");
                    return Result<Mission>.NotFound(scopeResolution.Error ?? "Escopo não encontrado.");
                }

                mission.OrganizationId = scopeResolution.Value;
                mission.SetScope(missionScope);
            }

            mission.MarkAsUpdated();
            await unitOfWork.CommitAsync(missionRepository.SaveChangesAsync, cancellationToken);

            LogMissionPatched(logger, id, mission.Name);
            return Result<Mission>.Success(mission);
        }
        catch (DomainInvariantException ex)
        {
            LogMissionPatchFailed(logger, id, ex.Message);
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

    [LoggerMessage(EventId = 4003, Level = LogLevel.Information, Message = "Patching mission {MissionId}")]
    private static partial void LogPatchingMission(ILogger logger, Guid missionId);

    [LoggerMessage(EventId = 4004, Level = LogLevel.Information, Message = "Mission patched successfully: {MissionId} - '{Name}'")]
    private static partial void LogMissionPatched(ILogger logger, Guid missionId, string name);

    [LoggerMessage(EventId = 4005, Level = LogLevel.Warning, Message = "Mission patch failed for {MissionId}: {Reason}")]
    private static partial void LogMissionPatchFailed(ILogger logger, Guid missionId, string reason);
}
