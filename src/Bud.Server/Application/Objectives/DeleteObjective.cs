using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.Objectives;

public sealed class DeleteObjective(
    IObjectiveRepository objectiveRepository,
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
