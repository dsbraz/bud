using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Indicators;

public sealed partial class DeleteIndicator(
    IIndicatorRepository indicatorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<DeleteIndicator> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        LogDeletingIndicator(logger, id);

        var indicatorForAuthorization = await indicatorRepository.GetByIdAsync(id, cancellationToken);

        if (indicatorForAuthorization is null)
        {
            LogIndicatorDeletionFailed(logger, id, "Not found");
            return Result.NotFound("Indicador não encontrado.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(
            user,
            indicatorForAuthorization.OrganizationId,
            cancellationToken);
        if (!canDelete)
        {
            LogIndicatorDeletionFailed(logger, id, "Forbidden");
            return Result.Forbidden("Você não tem permissão para excluir indicadores nesta meta.");
        }

        var indicator = await indicatorRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (indicator is null)
        {
            LogIndicatorDeletionFailed(logger, id, "Not found for update");
            return Result.NotFound("Indicador não encontrado.");
        }

        await indicatorRepository.RemoveAsync(indicator, cancellationToken);
        await unitOfWork.CommitAsync(indicatorRepository.SaveChangesAsync, cancellationToken);

        LogIndicatorDeleted(logger, id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4056, Level = LogLevel.Information, Message = "Deleting indicator {IndicatorId}")]
    private static partial void LogDeletingIndicator(ILogger logger, Guid indicatorId);

    [LoggerMessage(EventId = 4057, Level = LogLevel.Information, Message = "Indicator deleted successfully: {IndicatorId}")]
    private static partial void LogIndicatorDeleted(ILogger logger, Guid indicatorId);

    [LoggerMessage(EventId = 4058, Level = LogLevel.Warning, Message = "Indicator deletion failed for {IndicatorId}: {Reason}")]
    private static partial void LogIndicatorDeletionFailed(ILogger logger, Guid indicatorId, string reason);
}
