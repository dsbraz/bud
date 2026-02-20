using Bud.Server.Infrastructure.Repositories;
using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionObjectives;

public sealed class MissionObjectiveCommandUseCase(
    IMissionObjectiveRepository objectiveRepository,
    IApplicationAuthorizationGateway authorizationGateway) : IMissionObjectiveCommandUseCase
{
    public async Task<Result<MissionObjective>> CreateAsync(
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
                request.ObjectiveDimensionId.Value, mission.OrganizationId, cancellationToken);

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
            await objectiveRepository.SaveChangesAsync(cancellationToken);

            return Result<MissionObjective>.Success(objective);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MissionObjective>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result<MissionObjective>> UpdateAsync(
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
                request.ObjectiveDimensionId.Value, objective.OrganizationId, cancellationToken);

            if (!dimensionBelongs)
            {
                return Result<MissionObjective>.Failure(
                    "Dimensão do objetivo não encontrada para esta organização.",
                    ErrorType.Validation);
            }
        }

        try
        {
            var tracked = await objectiveRepository.GetByIdTrackedAsync(id, cancellationToken);
            tracked!.UpdateDetails(request.Name, request.Description, request.ObjectiveDimensionId);
            await objectiveRepository.SaveChangesAsync(cancellationToken);

            return Result<MissionObjective>.Success(tracked);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MissionObjective>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result> DeleteAsync(
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

        var tracked = await objectiveRepository.GetByIdTrackedAsync(id, cancellationToken);
        await objectiveRepository.RemoveAsync(tracked!, cancellationToken);
        await objectiveRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
