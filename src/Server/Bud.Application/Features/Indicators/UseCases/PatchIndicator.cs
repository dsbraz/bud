using Bud.Application.Common;
using Bud.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Indicators.UseCases;

public sealed record PatchIndicatorCommand(
    Optional<string> Name,
    Optional<IndicatorType> Type,
    Optional<QuantitativeIndicatorType?> QuantitativeType,
    Optional<decimal?> MinValue,
    Optional<decimal?> MaxValue,
    Optional<IndicatorUnit?> Unit,
    Optional<string?> TargetText);

public sealed partial class PatchIndicator(
    IIndicatorRepository indicatorRepository,
    ILogger<PatchIndicator> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Indicator>> ExecuteAsync(
        Guid id,
        PatchIndicatorCommand command,
        CancellationToken cancellationToken = default)
    {
        LogPatchingIndicator(logger, id);

        var indicator = await indicatorRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (indicator is null)
        {
            LogIndicatorPatchFailed(logger, id, "Not found");
            return Result<Indicator>.NotFound(UserErrorMessages.IndicatorNotFound);
        }

        try
        {
            var type = command.Type.HasValue ? command.Type.Value : indicator.Type;
            var quantitativeType = command.QuantitativeType.HasValue
                ? command.QuantitativeType.Value
                : indicator.QuantitativeType;
            var unit = command.Unit.HasValue ? command.Unit.Value : indicator.Unit;
            var name = command.Name.HasValue ? (command.Name.Value ?? indicator.Name) : indicator.Name;
            var minValue = command.MinValue.HasValue ? command.MinValue.Value : indicator.MinValue;
            var maxValue = command.MaxValue.HasValue ? command.MaxValue.Value : indicator.MaxValue;
            var targetText = command.TargetText.HasValue ? command.TargetText.Value : indicator.TargetText;

            indicator.UpdateDefinition(name, type);
            indicator.ApplyTarget(type, quantitativeType, minValue, maxValue, unit, targetText);

            await unitOfWork.CommitAsync(indicatorRepository.SaveChangesAsync, cancellationToken);

            LogIndicatorPatched(logger, id, indicator.Name);
            return Result<Indicator>.Success(indicator);
        }
        catch (DomainInvariantException ex)
        {
            LogIndicatorPatchFailed(logger, id, ex.Message);
            return Result<Indicator>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4053, Level = LogLevel.Information, Message = "Patching indicator {IndicatorId}")]
    private static partial void LogPatchingIndicator(ILogger logger, Guid indicatorId);

    [LoggerMessage(EventId = 4054, Level = LogLevel.Information, Message = "Indicator patched successfully: {IndicatorId} - '{Name}'")]
    private static partial void LogIndicatorPatched(ILogger logger, Guid indicatorId, string name);

    [LoggerMessage(EventId = 4055, Level = LogLevel.Warning, Message = "Indicator patch failed for {IndicatorId}: {Reason}")]
    private static partial void LogIndicatorPatchFailed(ILogger logger, Guid indicatorId, string reason);
}
