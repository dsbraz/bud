using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Bud.Application.UseCases.Checkins;

public sealed partial class DeleteCheckin(
    IIndicatorRepository indicatorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    ILogger<DeleteCheckin> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid indicatorId,
        Guid checkinId,
        CancellationToken cancellationToken = default)
    {
        LogDeletingCheckin(logger, checkinId, indicatorId);

        var checkin = await indicatorRepository.GetCheckinByIdAsync(checkinId, cancellationToken);
        if (checkin is null || checkin.IndicatorId != indicatorId)
        {
            LogCheckinDeletionFailed(logger, checkinId, "Not found");
            return Result.NotFound(UserErrorMessages.CheckinNotFound);
        }

        var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, checkin.OrganizationId, cancellationToken);
        if (!hasTenantAccess)
        {
            LogCheckinDeletionFailed(logger, checkinId, "Forbidden (tenant)");
            return Result.Forbidden(UserErrorMessages.CheckinDeleteForbidden);
        }

        if (!tenantProvider.IsGlobalAdmin && tenantProvider.CollaboratorId != checkin.CollaboratorId)
        {
            LogCheckinDeletionFailed(logger, checkinId, "Not the author");
            return Result.Forbidden(UserErrorMessages.CheckinDeleteAuthorOnly);
        }

        await indicatorRepository.RemoveCheckinAsync(checkin, cancellationToken);
        await unitOfWork.CommitAsync(indicatorRepository.SaveChangesAsync, cancellationToken);

        LogCheckinDeleted(logger, checkinId, indicatorId);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4066, Level = LogLevel.Information, Message = "Deleting checkin {CheckinId} for indicator {IndicatorId}")]
    private static partial void LogDeletingCheckin(ILogger logger, Guid checkinId, Guid indicatorId);

    [LoggerMessage(EventId = 4067, Level = LogLevel.Information, Message = "Checkin deleted successfully: {CheckinId} for indicator {IndicatorId}")]
    private static partial void LogCheckinDeleted(ILogger logger, Guid checkinId, Guid indicatorId);

    [LoggerMessage(EventId = 4068, Level = LogLevel.Warning, Message = "Checkin deletion failed for {CheckinId}: {Reason}")]
    private static partial void LogCheckinDeletionFailed(ILogger logger, Guid checkinId, string reason);
}
