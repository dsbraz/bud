using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Missions;

public sealed class DeleteMission(
    IMissionRepository missionRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var mission = await missionRepository.GetByIdAsync(id, cancellationToken);
        if (mission is null)
        {
            return Result.NotFound("Missão não encontrada.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return Result.Forbidden("Você não tem permissão para excluir missões nesta organização.");
        }

        mission.MarkAsDeleted();
        await missionRepository.RemoveAsync(mission, cancellationToken);
        await unitOfWork.CommitAsync(missionRepository.SaveChangesAsync, cancellationToken);

        return Result.Success();
    }
}
