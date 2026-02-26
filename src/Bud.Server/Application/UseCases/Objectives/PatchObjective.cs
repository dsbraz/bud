using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Objectives;

public sealed partial class PatchObjective(
    IObjectiveRepository objectiveRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<PatchObjective> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Objective>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchObjectiveRequest request,
        CancellationToken cancellationToken = default)
    {
        LogPatchingObjective(logger, id);

        var objective = await objectiveRepository.GetByIdAsync(id, cancellationToken);

        if (objective is null)
        {
            LogObjectivePatchFailed(logger, id, "Not found");
            return Result<Objective>.NotFound("Objetivo não encontrado.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, objective.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            LogObjectivePatchFailed(logger, id, "Forbidden");
            return Result<Objective>.Forbidden("Você não tem permissão para atualizar objetivos nesta missão.");
        }

        try
        {
            var trackedObjective = await objectiveRepository.GetByIdForUpdateAsync(id, cancellationToken);
            if (trackedObjective is null)
            {
                LogObjectivePatchFailed(logger, id, "Not found for update");
                return Result<Objective>.NotFound("Objetivo não encontrado.");
            }

            var name = request.Name.HasValue ? (request.Name.Value ?? trackedObjective.Name) : trackedObjective.Name;
            var description = request.Description.HasValue ? request.Description.Value : trackedObjective.Description;
            var dimension = request.Dimension.HasValue ? request.Dimension.Value : trackedObjective.Dimension;
            trackedObjective.UpdateDetails(name, description, dimension);
            await unitOfWork.CommitAsync(objectiveRepository.SaveChangesAsync, cancellationToken);

            LogObjectivePatched(logger, id, trackedObjective.Name);
            return Result<Objective>.Success(trackedObjective);
        }
        catch (DomainInvariantException ex)
        {
            LogObjectivePatchFailed(logger, id, ex.Message);
            return Result<Objective>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4084, Level = LogLevel.Information, Message = "Patching objective {ObjectiveId}")]
    private static partial void LogPatchingObjective(ILogger logger, Guid objectiveId);

    [LoggerMessage(EventId = 4085, Level = LogLevel.Information, Message = "Objective patched successfully: {ObjectiveId} - '{Name}'")]
    private static partial void LogObjectivePatched(ILogger logger, Guid objectiveId, string name);

    [LoggerMessage(EventId = 4086, Level = LogLevel.Warning, Message = "Objective patch failed for {ObjectiveId}: {Reason}")]
    private static partial void LogObjectivePatchFailed(ILogger logger, Guid objectiveId, string reason);
}
