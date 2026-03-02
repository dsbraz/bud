using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Indicators;

public sealed partial class PatchIndicator(
    IIndicatorRepository indicatorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<PatchIndicator> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Indicator>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchIndicatorRequest request,
        CancellationToken cancellationToken = default)
    {
        LogPatchingIndicator(logger, id);

        var indicatorForAuthorization = await indicatorRepository.GetByIdAsync(id, cancellationToken);

        if (indicatorForAuthorization is null)
        {
            LogIndicatorPatchFailed(logger, id, "Not found");
            return Result<Indicator>.NotFound(UserErrorMessages.IndicatorNotFound);
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(
            user,
            indicatorForAuthorization.OrganizationId,
            cancellationToken);
        if (!canUpdate)
        {
            LogIndicatorPatchFailed(logger, id, "Forbidden");
            return Result<Indicator>.Forbidden(UserErrorMessages.IndicatorUpdateForbidden);
        }

        var indicator = await indicatorRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (indicator is null)
        {
            LogIndicatorPatchFailed(logger, id, "Not found for update");
            return Result<Indicator>.NotFound(UserErrorMessages.IndicatorNotFound);
        }

        try
        {
            var type = request.Type.HasValue ? request.Type.Value : indicator.Type;
            var quantitativeType = request.QuantitativeType.HasValue
                ? request.QuantitativeType.Value
                : indicator.QuantitativeType;
            var unit = request.Unit.HasValue ? request.Unit.Value : indicator.Unit;
            var name = request.Name.HasValue ? (request.Name.Value ?? indicator.Name) : indicator.Name;
            var minValue = request.MinValue.HasValue ? request.MinValue.Value : indicator.MinValue;
            var maxValue = request.MaxValue.HasValue ? request.MaxValue.Value : indicator.MaxValue;
            var targetText = request.TargetText.HasValue ? request.TargetText.Value : indicator.TargetText;

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
