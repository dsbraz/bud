using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Objectives;

public sealed class PatchObjective(
    IObjectiveRepository objectiveRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Objective>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchObjectiveRequest request,
        CancellationToken cancellationToken = default)
    {
        var objective = await objectiveRepository.GetByIdAsync(id, cancellationToken);

        if (objective is null)
        {
            return Result<Objective>.NotFound("Objetivo não encontrado.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, objective.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return Result<Objective>.Forbidden("Você não tem permissão para atualizar objetivos nesta missão.");
        }

        try
        {
            var trackedObjective = await objectiveRepository.GetByIdTrackedAsync(id, cancellationToken);
            if (trackedObjective is null)
            {
                return Result<Objective>.NotFound("Objetivo não encontrado.");
            }

            var name = request.Name.HasValue ? (request.Name.Value ?? trackedObjective.Name) : trackedObjective.Name;
            var description = request.Description.HasValue ? request.Description.Value : trackedObjective.Description;
            var dimension = request.Dimension.HasValue ? request.Dimension.Value : trackedObjective.Dimension;
            trackedObjective.UpdateDetails(name, description, dimension);
            await unitOfWork.CommitAsync(objectiveRepository.SaveChangesAsync, cancellationToken);

            return Result<Objective>.Success(trackedObjective);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Objective>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}
