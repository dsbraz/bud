using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Projections;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Infrastructure.Services;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.MissionObjectives;

public sealed class DefineMissionObjective(
    IMissionObjectiveRepository objectiveRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<MissionObjective>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateMissionObjectiveRequest request,
        CancellationToken cancellationToken = default)
    {
        var mission = await objectiveRepository.GetMissionAsync(request.MissionId, cancellationToken);

        if (mission is null)
        {
            return Result<MissionObjective>.NotFound("Missão não encontrada.");
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            return Result<MissionObjective>.Forbidden("Você não tem permissão para criar objetivos nesta missão.");
        }

        if (request.ObjectiveDimensionId.HasValue)
        {
            var dimensionBelongs = await objectiveRepository.DimensionBelongsToOrganizationAsync(
                request.ObjectiveDimensionId.Value,
                mission.OrganizationId,
                cancellationToken);

            if (!dimensionBelongs)
            {
                return Result<MissionObjective>.Failure(
                    "Dimensão do objetivo não encontrada para esta organização.",
                    ErrorType.Validation);
            }
        }

        try
        {
            var objective = MissionObjective.Create(
                Guid.NewGuid(),
                mission.OrganizationId,
                request.MissionId,
                request.Name,
                request.Description,
                request.ObjectiveDimensionId);

            await objectiveRepository.AddAsync(objective, cancellationToken);
            await unitOfWork.CommitAsync(objectiveRepository.SaveChangesAsync, cancellationToken);

            return Result<MissionObjective>.Success(objective);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MissionObjective>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public sealed class ReviseMissionObjective(
    IMissionObjectiveRepository objectiveRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<MissionObjective>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionObjectiveRequest request,
        CancellationToken cancellationToken = default)
    {
        var objective = await objectiveRepository.GetByIdAsync(id, cancellationToken);

        if (objective is null)
        {
            return Result<MissionObjective>.NotFound("Objetivo não encontrado.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, objective.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return Result<MissionObjective>.Forbidden("Você não tem permissão para atualizar objetivos nesta missão.");
        }

        if (request.ObjectiveDimensionId.HasValue)
        {
            var dimensionBelongs = await objectiveRepository.DimensionBelongsToOrganizationAsync(
                request.ObjectiveDimensionId.Value,
                objective.OrganizationId,
                cancellationToken);

            if (!dimensionBelongs)
            {
                return Result<MissionObjective>.Failure(
                    "Dimensão do objetivo não encontrada para esta organização.",
                    ErrorType.Validation);
            }
        }

        try
        {
            var trackedObjective = await objectiveRepository.GetByIdTrackedAsync(id, cancellationToken);
            trackedObjective!.UpdateDetails(request.Name, request.Description, request.ObjectiveDimensionId);
            await unitOfWork.CommitAsync(objectiveRepository.SaveChangesAsync, cancellationToken);

            return Result<MissionObjective>.Success(trackedObjective);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MissionObjective>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public sealed class RemoveMissionObjective(
    IMissionObjectiveRepository objectiveRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var objective = await objectiveRepository.GetByIdAsync(id, cancellationToken);

        if (objective is null)
        {
            return Result.NotFound("Objetivo não encontrado.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, objective.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return Result.Forbidden("Você não tem permissão para excluir objetivos nesta missão.");
        }

        var trackedObjective = await objectiveRepository.GetByIdTrackedAsync(id, cancellationToken);
        await objectiveRepository.RemoveAsync(trackedObjective!, cancellationToken);
        await unitOfWork.CommitAsync(objectiveRepository.SaveChangesAsync, cancellationToken);

        return Result.Success();
    }
}

public sealed class ViewMissionObjectiveDetails(IMissionObjectiveRepository objectiveRepository)
{
    public async Task<Result<MissionObjective>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var objective = await objectiveRepository.GetByIdAsync(id, cancellationToken);

        return objective is null
            ? Result<MissionObjective>.NotFound("Objetivo não encontrado.")
            : Result<MissionObjective>.Success(objective);
    }
}

public sealed class ListMissionObjectives(IMissionObjectiveRepository objectiveRepository)
{
    public async Task<Result<PagedResult<MissionObjective>>> ExecuteAsync(
        Guid missionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await objectiveRepository.GetByMissionAsync(missionId, page, pageSize, cancellationToken);
        return Result<PagedResult<MissionObjective>>.Success(result);
    }
}

public sealed class CalculateMissionObjectiveProgress(IMissionProgressService missionProgressService)
{
    public async Task<Result<List<ObjectiveProgressDto>>> ExecuteAsync(
        List<Guid> objectiveIds,
        CancellationToken cancellationToken = default)
    {
        var result = await missionProgressService.GetObjectiveProgressAsync(objectiveIds, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<List<ObjectiveProgressDto>>.Failure(
                result.Error ?? "Falha ao calcular progresso dos objetivos.",
                result.ErrorType);
        }

        return Result<List<ObjectiveProgressDto>>.Success(result.Value!.Select(progress => progress.ToContract()).ToList());
    }
}
