using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Goals;

public sealed partial class PatchGoal(
    IGoalRepository goalRepository,
    IGoalScopeResolver goalScopeResolver,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<PatchGoal> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Goal>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchGoalRequest request,
        CancellationToken cancellationToken = default)
    {
        LogPatchingGoal(logger, id);

        var goal = await goalRepository.GetByIdAsync(id, cancellationToken);
        if (goal is null)
        {
            LogGoalPatchFailed(logger, id, "Not found");
            return Result<Goal>.NotFound("Meta não encontrada.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, goal.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            LogGoalPatchFailed(logger, id, "Forbidden");
            return Result<Goal>.Forbidden("Você não tem permissão para atualizar metas nesta organização.");
        }

        if (goal.ParentId.HasValue && request.StartDate.HasValue)
        {
            var parentGoal = await goalRepository.GetByIdReadOnlyAsync(goal.ParentId.Value, cancellationToken);
            if (parentGoal is not null && NormalizeToUtc(request.StartDate.Value) < parentGoal.StartDate)
            {
                var msg = $"A data de início da meta não pode ser anterior à do pai ({parentGoal.StartDate:dd/MM/yyyy}).";
                LogGoalPatchFailed(logger, id, msg);
                return Result<Goal>.Failure(msg, ErrorType.Validation);
            }
        }

        try
        {
            var status = request.Status.HasValue ? request.Status.Value : goal.Status;
            var scopeType = request.ScopeType.HasValue
                ? request.ScopeType.Value
                : goal.WorkspaceId.HasValue
                    ? GoalScopeType.Workspace
                    : goal.TeamId.HasValue
                        ? GoalScopeType.Team
                        : goal.CollaboratorId.HasValue
                            ? GoalScopeType.Collaborator
                            : GoalScopeType.Organization;
            var name = request.Name.HasValue ? (request.Name.Value ?? goal.Name) : goal.Name;
            var description = request.Description.HasValue ? request.Description.Value : goal.Description;
            var dimension = request.Dimension.HasValue ? request.Dimension.Value : goal.Dimension;
            var startDate = request.StartDate.HasValue ? request.StartDate.Value : goal.StartDate;
            var endDate = request.EndDate.HasValue ? request.EndDate.Value : goal.EndDate;

            goal.UpdateDetails(name, description, dimension, NormalizeToUtc(startDate), NormalizeToUtc(endDate), status);

            var shouldUpdateScope = request.ScopeId.HasValue && request.ScopeId.Value != Guid.Empty;
            if (shouldUpdateScope)
            {
                var scopeId = request.ScopeId.Value;

                var scopeResolution = await goalScopeResolver.ResolveScopeOrganizationIdAsync(
                    scopeType,
                    scopeId,
                    ct: cancellationToken);
                if (!scopeResolution.IsSuccess)
                {
                    LogGoalPatchFailed(logger, id, scopeResolution.Error ?? "Escopo não encontrado.");
                    return Result<Goal>.NotFound(scopeResolution.Error ?? "Escopo não encontrado.");
                }

                goal.OrganizationId = scopeResolution.Value;
                goal.SetScope(scopeType, scopeId);
            }

            goal.MarkAsUpdated();
            await unitOfWork.CommitAsync(goalRepository.SaveChangesAsync, cancellationToken);

            LogGoalPatched(logger, id, goal.Name);
            return Result<Goal>.Success(goal);
        }
        catch (DomainInvariantException ex)
        {
            LogGoalPatchFailed(logger, id, ex.Message);
            return Result<Goal>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };
    }

    [LoggerMessage(EventId = 4003, Level = LogLevel.Information, Message = "Patching goal {GoalId}")]
    private static partial void LogPatchingGoal(ILogger logger, Guid goalId);

    [LoggerMessage(EventId = 4004, Level = LogLevel.Information, Message = "Goal patched successfully: {GoalId} - '{Name}'")]
    private static partial void LogGoalPatched(ILogger logger, Guid goalId, string name);

    [LoggerMessage(EventId = 4005, Level = LogLevel.Warning, Message = "Goal patch failed for {GoalId}: {Reason}")]
    private static partial void LogGoalPatchFailed(ILogger logger, Guid goalId, string reason);
}
