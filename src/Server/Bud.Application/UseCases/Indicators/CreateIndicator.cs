using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Domain.Model;
using Bud.Domain.Repositories;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Application.UseCases.Indicators;

public sealed partial class CreateIndicator(
    IIndicatorRepository indicatorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<CreateIndicator> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Indicator>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateIndicatorRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingIndicator(logger, request.Name, request.GoalId);

        var goal = await indicatorRepository.GetGoalByIdAsync(request.GoalId, cancellationToken);

        if (goal is null)
        {
            LogIndicatorCreationFailed(logger, request.Name, "Goal not found");
            return Result<Indicator>.NotFound(UserErrorMessages.GoalNotFound);
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, goal.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            LogIndicatorCreationFailed(logger, request.Name, "Forbidden");
            return Result<Indicator>.Forbidden(UserErrorMessages.IndicatorCreateForbidden);
        }

        try
        {
            var indicator = Indicator.Create(
                Guid.NewGuid(),
                goal.OrganizationId,
                request.GoalId,
                request.Name,
                request.Type);

            indicator.ApplyTarget(request.Type, request.QuantitativeType, request.MinValue, request.MaxValue, request.Unit, request.TargetText);

            await indicatorRepository.AddAsync(indicator, cancellationToken);
            await unitOfWork.CommitAsync(indicatorRepository.SaveChangesAsync, cancellationToken);

            LogIndicatorCreated(logger, indicator.Id, indicator.Name);
            return Result<Indicator>.Success(indicator);
        }
        catch (DomainInvariantException ex)
        {
            LogIndicatorCreationFailed(logger, request.Name, ex.Message);
            return Result<Indicator>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4050, Level = LogLevel.Information, Message = "Creating indicator '{Name}' for goal {GoalId}")]
    private static partial void LogCreatingIndicator(ILogger logger, string name, Guid goalId);

    [LoggerMessage(EventId = 4051, Level = LogLevel.Information, Message = "Indicator created successfully: {IndicatorId} - '{Name}'")]
    private static partial void LogIndicatorCreated(ILogger logger, Guid indicatorId, string name);

    [LoggerMessage(EventId = 4052, Level = LogLevel.Warning, Message = "Indicator creation failed for '{Name}': {Reason}")]
    private static partial void LogIndicatorCreationFailed(ILogger logger, string name, string reason);
}
