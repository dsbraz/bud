using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Domain.ValueObjects;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Collaborators;

public sealed class PatchCollaboratorTeams(
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchCollaboratorTeamsRequest request,
        CancellationToken cancellationToken = default)
    {
        var collaborator = await collaboratorRepository.GetByIdWithCollaboratorTeamsAsync(id, cancellationToken);
        if (collaborator is null)
        {
            return Result.NotFound("Colaborador não encontrado.");
        }

        var canAssign = await authorizationGateway.IsOrganizationOwnerAsync(user, collaborator.OrganizationId, cancellationToken);
        if (!canAssign)
        {
            return Result.Forbidden("Apenas o proprietário da organização pode atribuir equipes.");
        }

        var distinctTeamIds = request.TeamIds.Distinct().ToList();

        if (distinctTeamIds.Count > 0)
        {
            var validCount = await collaboratorRepository.CountTeamsByIdsAndOrganizationAsync(
                distinctTeamIds,
                collaborator.OrganizationId,
                cancellationToken);

            if (validCount != distinctTeamIds.Count)
            {
                return Result.Failure("Uma ou mais equipes são inválidas ou pertencem a outra organização.", ErrorType.Validation);
            }
        }

        collaborator.CollaboratorTeams.Clear();

        foreach (var teamId in distinctTeamIds)
        {
            collaborator.CollaboratorTeams.Add(new CollaboratorTeam
            {
                CollaboratorId = id,
                TeamId = teamId,
                AssignedAt = DateTime.UtcNow
            });
        }

        await unitOfWork.CommitAsync(collaboratorRepository.SaveChangesAsync, cancellationToken);
        return Result.Success();
    }
}

