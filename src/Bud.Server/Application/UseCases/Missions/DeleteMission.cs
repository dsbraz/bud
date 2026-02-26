using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Missions;

public sealed partial class DeleteMission(
    IMissionRepository missionRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<DeleteMission> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        LogDeletingMission(logger, id);

        var mission = await missionRepository.GetByIdAsync(id, cancellationToken);
        if (mission is null)
        {
            LogMissionDeletionFailed(logger, id, "Not found");
            return Result.NotFound("Missão não encontrada.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            LogMissionDeletionFailed(logger, id, "Forbidden");
            return Result.Forbidden("Você não tem permissão para excluir missões nesta organização.");
        }

        mission.MarkAsDeleted();
        await missionRepository.RemoveAsync(mission, cancellationToken);
        await unitOfWork.CommitAsync(missionRepository.SaveChangesAsync, cancellationToken);

        LogMissionDeleted(logger, id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4006, Level = LogLevel.Information, Message = "Deleting mission {MissionId}")]
    private static partial void LogDeletingMission(ILogger logger, Guid missionId);

    [LoggerMessage(EventId = 4007, Level = LogLevel.Information, Message = "Mission deleted successfully: {MissionId}")]
    private static partial void LogMissionDeleted(ILogger logger, Guid missionId);

    [LoggerMessage(EventId = 4008, Level = LogLevel.Warning, Message = "Mission deletion failed for {MissionId}: {Reason}")]
    private static partial void LogMissionDeletionFailed(ILogger logger, Guid missionId, string reason);
}
