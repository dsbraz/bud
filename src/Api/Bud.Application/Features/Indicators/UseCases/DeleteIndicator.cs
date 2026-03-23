using Bud.Application.Common;
using Bud.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Indicators.UseCases;

public sealed partial class DeleteIndicator(
    IIndicatorRepository indicatorRepository,
    ILogger<DeleteIndicator> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        LogDeletingIndicator(logger, id);

        var indicator = await indicatorRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (indicator is null)
        {
            LogIndicatorDeletionFailed(logger, id, "Not found");
            return Result.NotFound(UserErrorMessages.IndicatorNotFound);
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
