using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Contracts.Requests;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Checkins;

public sealed partial class PatchCheckin(
    IIndicatorRepository indicatorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    ILogger<PatchCheckin> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Checkin>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid indicatorId,
        Guid checkinId,
        PatchCheckinRequest request,
        CancellationToken cancellationToken = default)
    {
        LogPatchingCheckin(logger, checkinId, indicatorId);

        var checkin = await indicatorRepository.GetCheckinByIdAsync(checkinId, cancellationToken);
        if (checkin is null || checkin.IndicatorId != indicatorId)
        {
            LogCheckinPatchFailed(logger, checkinId, "Not found");
            return Result<Checkin>.NotFound("Check-in não encontrado.");
        }

        var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, checkin.OrganizationId, cancellationToken);
        if (!hasTenantAccess)
        {
            LogCheckinPatchFailed(logger, checkinId, "Forbidden (tenant)");
            return Result<Checkin>.Forbidden("Você não tem permissão para atualizar este check-in.");
        }

        if (!tenantProvider.IsGlobalAdmin && tenantProvider.CollaboratorId != checkin.CollaboratorId)
        {
            LogCheckinPatchFailed(logger, checkinId, "Not the author");
            return Result<Checkin>.Forbidden("Apenas o autor pode editar este check-in.");
        }

        var indicator = await indicatorRepository.GetByIdAsync(indicatorId, cancellationToken);
        if (indicator is null)
        {
            LogCheckinPatchFailed(logger, checkinId, "Indicator not found");
            return Result<Checkin>.NotFound("Indicador não encontrado.");
        }

        try
        {
            indicator.UpdateCheckin(
                checkin,
                request.Value,
                request.Text,
                DateTime.SpecifyKind(request.CheckinDate, DateTimeKind.Utc),
                request.Note,
                request.ConfidenceLevel);

            await unitOfWork.CommitAsync(indicatorRepository.SaveChangesAsync, cancellationToken);
            LogCheckinPatched(logger, checkinId, indicatorId);
            return Result<Checkin>.Success(checkin);
        }
        catch (DomainInvariantException ex)
        {
            LogCheckinPatchFailed(logger, checkinId, ex.Message);
            return Result<Checkin>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4063, Level = LogLevel.Information, Message = "Patching checkin {CheckinId} for indicator {IndicatorId}")]
    private static partial void LogPatchingCheckin(ILogger logger, Guid checkinId, Guid indicatorId);

    [LoggerMessage(EventId = 4064, Level = LogLevel.Information, Message = "Checkin patched successfully: {CheckinId} for indicator {IndicatorId}")]
    private static partial void LogCheckinPatched(ILogger logger, Guid checkinId, Guid indicatorId);

    [LoggerMessage(EventId = 4065, Level = LogLevel.Warning, Message = "Checkin patch failed for {CheckinId}: {Reason}")]
    private static partial void LogCheckinPatchFailed(ILogger logger, Guid checkinId, string reason);
}
