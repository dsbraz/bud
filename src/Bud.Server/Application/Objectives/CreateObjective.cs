using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Objectives;

public sealed class CreateObjective(
    IMissionRepository missionRepository,
    IObjectiveRepository objectiveRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Objective>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateObjectiveRequest request,
        CancellationToken cancellationToken = default)
    {
        var mission = await missionRepository.GetByIdAsync(request.MissionId, cancellationToken);

        if (mission is null)
        {
            return Result<Objective>.NotFound("Missão não encontrada.");
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canCreate)
        {
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

            return Result<Objective>.Success(objective);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Objective>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}
