using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Indicators;

public sealed partial class CreateCheckin(
    IIndicatorRepository indicatorRepository,
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    ILogger<CreateCheckin> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Checkin>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid indicatorId,
        CreateCheckinRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingCheckin(logger, indicatorId);

        var indicator = await indicatorRepository.GetIndicatorWithGoalAsync(indicatorId, cancellationToken);
        if (indicator is null)
        {
            LogCheckinCreationFailed(logger, indicatorId, "Indicator not found");
            return Result<Checkin>.NotFound(UserErrorMessages.IndicatorNotFound);
        }

        var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, indicator.OrganizationId, cancellationToken);
        if (!hasTenantAccess)
        {
            LogCheckinCreationFailed(logger, indicatorId, "Forbidden (tenant)");
            return Result<Checkin>.Forbidden(UserErrorMessages.CheckinCreateForbidden);
        }

        var collaboratorId = tenantProvider.CollaboratorId;
        if (!collaboratorId.HasValue)
        {
            LogCheckinCreationFailed(logger, indicatorId, "Collaborator not identified");
            return Result<Checkin>.Forbidden(UserErrorMessages.CollaboratorNotIdentified);
        }

        var collaborator = await collaboratorRepository.GetByIdAsync(collaboratorId.Value, cancellationToken);
        if (collaborator is null)
        {
            LogCheckinCreationFailed(logger, indicatorId, "Collaborator not found");
            return Result<Checkin>.NotFound(UserErrorMessages.CollaboratorNotFound);
        }

        var goal = indicator.Goal;
        if (goal.Status != GoalStatus.Active)
        {
            LogCheckinCreationFailed(logger, indicatorId, "Goal not active");
            return Result<Checkin>.Failure(
                "Não é possível fazer check-in em indicadores de metas que não estão ativas.",
                ErrorType.Validation);
        }

        try
        {
            var checkin = indicator.CreateCheckin(
                Guid.NewGuid(),
                collaboratorId.Value,
                request.Value,
                request.Text,
                DateTime.SpecifyKind(request.CheckinDate, DateTimeKind.Utc),
                request.Note,
                request.ConfidenceLevel);

            await indicatorRepository.AddCheckinAsync(checkin, cancellationToken);
            await unitOfWork.CommitAsync(indicatorRepository.SaveChangesAsync, cancellationToken);

            LogCheckinCreated(logger, checkin.Id, indicatorId);
            return Result<Checkin>.Success(checkin);
        }
        catch (DomainInvariantException ex)
        {
            LogCheckinCreationFailed(logger, indicatorId, ex.Message);
            return Result<Checkin>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4060, Level = LogLevel.Information, Message = "Creating checkin for indicator {IndicatorId}")]
    private static partial void LogCreatingCheckin(ILogger logger, Guid indicatorId);

    [LoggerMessage(EventId = 4061, Level = LogLevel.Information, Message = "Checkin created successfully: {CheckinId} for indicator {IndicatorId}")]
    private static partial void LogCheckinCreated(ILogger logger, Guid checkinId, Guid indicatorId);

    [LoggerMessage(EventId = 4062, Level = LogLevel.Warning, Message = "Checkin creation failed for indicator {IndicatorId}: {Reason}")]
    private static partial void LogCheckinCreationFailed(ILogger logger, Guid indicatorId, string reason);
}
