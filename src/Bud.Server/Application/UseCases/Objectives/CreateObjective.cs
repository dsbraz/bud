using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Objectives;

public sealed partial class CreateObjective(
    IMissionRepository missionRepository,
    IObjectiveRepository objectiveRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<CreateObjective> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Objective>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateObjectiveRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingObjective(logger, request.Name, request.MissionId);

        var mission = await missionRepository.GetByIdAsync(request.MissionId, cancellationToken);

        if (mission is null)
        {
            LogObjectiveCreationFailed(logger, request.Name, "Mission not found");
            return Result<Objective>.NotFound("Missão não encontrada.");
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            LogObjectiveCreationFailed(logger, request.Name, "Forbidden");
            return Result<Objective>.Forbidden("Você não tem permissão para criar objetivos nesta missão.");
        }

        try
        {
            var objective = Objective.Create(
                Guid.NewGuid(),
                mission.OrganizationId,
                request.MissionId,
                request.Name,
                request.Description,
                request.Dimension);

            await objectiveRepository.AddAsync(objective, cancellationToken);
            await unitOfWork.CommitAsync(objectiveRepository.SaveChangesAsync, cancellationToken);

            LogObjectiveCreated(logger, objective.Id, objective.Name);
            return Result<Objective>.Success(objective);
        }
        catch (DomainInvariantException ex)
        {
            LogObjectiveCreationFailed(logger, request.Name, ex.Message);
            return Result<Objective>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4081, Level = LogLevel.Information, Message = "Creating objective '{Name}' for mission {MissionId}")]
    private static partial void LogCreatingObjective(ILogger logger, string name, Guid missionId);

    [LoggerMessage(EventId = 4082, Level = LogLevel.Information, Message = "Objective created successfully: {ObjectiveId} - '{Name}'")]
    private static partial void LogObjectiveCreated(ILogger logger, Guid objectiveId, string name);

    [LoggerMessage(EventId = 4083, Level = LogLevel.Warning, Message = "Objective creation failed for '{Name}': {Reason}")]
    private static partial void LogObjectiveCreationFailed(ILogger logger, string name, string reason);
}
