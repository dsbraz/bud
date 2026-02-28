using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Policies;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Goals;

public sealed partial class CreateGoal(
    IGoalRepository goalRepository,
    IGoalScopeResolver goalScopeResolver,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<CreateGoal> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Goal>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateGoalRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingGoal(logger, request.Name, request.ScopeType);

        var scopeResolution = await goalScopeResolver.ResolveScopeOrganizationIdAsync(
            request.ScopeType,
            request.ScopeId,
            ignoreQueryFilters: true,
            ct: cancellationToken);

        if (!scopeResolution.IsSuccess)
        {
            var error = scopeResolution.Error ?? UserErrorMessages.ScopeNotFound;
            LogGoalCreationFailed(logger, request.Name, error);
            return Result<Goal>.NotFound(error);
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, scopeResolution.Value, cancellationToken);
        if (!canCreate)
        {
            LogGoalCreationFailed(logger, request.Name, "Forbidden");
            return Result<Goal>.Forbidden(UserErrorMessages.GoalCreateForbidden);
        }

        Goal? parentGoal = null;
        if (request.ParentId.HasValue)
        {
            parentGoal = await goalRepository.GetByIdReadOnlyAsync(request.ParentId.Value, cancellationToken);
            if (parentGoal is null)
            {
                LogGoalCreationFailed(logger, request.Name, UserErrorMessages.ParentGoalNotFound);
                return Result<Goal>.NotFound(UserErrorMessages.ParentGoalNotFound);
            }
        }

        if (parentGoal is not null)
        {
            var violation = GoalDateRangePolicy.ValidateChildStartDate<Goal>(
                UtcDateTimeNormalizer.Normalize(request.StartDate), parentGoal.StartDate);
            if (violation is not null)
            {
                LogGoalCreationFailed(logger, request.Name, violation.Error!);
                return violation;
            }
        }

        try
        {
            var goal = Goal.Create(
                Guid.NewGuid(),
                scopeResolution.Value,
                request.Name,
                request.Description,
                request.Dimension,
                UtcDateTimeNormalizer.Normalize(request.StartDate),
                UtcDateTimeNormalizer.Normalize(request.EndDate),
                request.Status,
                parentGoal?.Id);

            goal.SetScope(request.ScopeType, request.ScopeId);

            await goalRepository.AddAsync(goal, cancellationToken);
            await unitOfWork.CommitAsync(goalRepository.SaveChangesAsync, cancellationToken);

            LogGoalCreated(logger, goal.Id, goal.Name);
            return Result<Goal>.Success(goal);
        }
        catch (DomainInvariantException ex)
        {
            LogGoalCreationFailed(logger, request.Name, ex.Message);
            return Result<Goal>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4000, Level = LogLevel.Information, Message = "Creating goal '{Name}' with scope type '{ScopeType}'")]
    private static partial void LogCreatingGoal(ILogger logger, string name, GoalScopeType scopeType);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Goal created successfully: {GoalId} - '{Name}'")]
    private static partial void LogGoalCreated(ILogger logger, Guid goalId, string name);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "Goal creation failed for '{Name}': {Reason}")]
    private static partial void LogGoalCreationFailed(ILogger logger, string name, string reason);
}
