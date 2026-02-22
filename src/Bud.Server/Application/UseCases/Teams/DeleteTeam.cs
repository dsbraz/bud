using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Teams;

public sealed class DeleteTeam(
    ITeamRepository teamRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var team = await teamRepository.GetByIdAsync(id, cancellationToken);
        if (team is null)
        {
            return Result.NotFound("Time não encontrado.");
        }

        var canDelete = await authorizationGateway.CanWriteOrganizationAsync(user, team.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return Result.Forbidden("Você não tem permissão para excluir este time.");
        }

        if (await teamRepository.HasSubTeamsAsync(id, cancellationToken))
        {
            return Result.Failure("Não é possível excluir um time com sub-times. Exclua os sub-times primeiro.", ErrorType.Conflict);
        }

        if (await teamRepository.HasMissionsAsync(id, cancellationToken))
        {
            return Result.Failure(
                "Não é possível excluir o time porque existem missões associadas a ele.",
                ErrorType.Conflict);
        }

        await teamRepository.RemoveAsync(team, cancellationToken);
        await unitOfWork.CommitAsync(teamRepository.SaveChangesAsync, cancellationToken);

        return Result.Success();
    }
}

