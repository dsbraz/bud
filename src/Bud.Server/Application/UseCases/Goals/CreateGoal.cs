using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Policies;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Goals;

public sealed partial class CreateGoal(
    IGoalRepository goalRepository,
    ICollaboratorRepository collaboratorRepository,
    ITenantProvider tenantProvider,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<CreateGoal> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Goal>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateGoalRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingGoal(logger, request.Name);

        var organizationId = tenantProvider.TenantId;
        if (!organizationId.HasValue)
        {
            LogGoalCreationFailed(logger, request.Name, "Tenant not selected");
            return Result<Goal>.Forbidden(UserErrorMessages.GoalCreateForbidden);
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, organizationId.Value, cancellationToken);
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
                organizationId.Value,
                request.Name,
                request.Description,
                request.Dimension,
                UtcDateTimeNormalizer.Normalize(request.StartDate),
                UtcDateTimeNormalizer.Normalize(request.EndDate),
                request.Status,
                parentGoal?.Id,
                tenantProvider.CollaboratorId);

            if (request.CollaboratorId.HasValue)
            {
                var collaborator = await collaboratorRepository.GetByIdAsync(request.CollaboratorId.Value, cancellationToken);
                if (collaborator is null)
                {
                    LogGoalCreationFailed(logger, request.Name, UserErrorMessages.CollaboratorNotFound);
                    return Result<Goal>.NotFound(UserErrorMessages.CollaboratorNotFound);
                }

                if (collaborator.OrganizationId != organizationId.Value)
                {
                    LogGoalCreationFailed(logger, request.Name, "Collaborator belongs to different organization");
                    return Result<Goal>.Forbidden(UserErrorMessages.GoalCreateForbidden);
                }

                goal.CollaboratorId = request.CollaboratorId.Value;
            }

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

    [LoggerMessage(EventId = 4000, Level = LogLevel.Information, Message = "Creating goal '{Name}'")]
    private static partial void LogCreatingGoal(ILogger logger, string name);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Goal created successfully: {GoalId} - '{Name}'")]
    private static partial void LogGoalCreated(ILogger logger, Guid goalId, string name);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "Goal creation failed for '{Name}': {Reason}")]
    private static partial void LogGoalCreationFailed(ILogger logger, string name, string reason);
}
