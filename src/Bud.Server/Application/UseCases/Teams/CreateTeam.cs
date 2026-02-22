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

public sealed class CreateTeam(
    ITeamRepository teamRepository,
    IWorkspaceRepository workspaceRepository,
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Team>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspace = await workspaceRepository.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace is null)
        {
            return Result<Team>.NotFound("Workspace não encontrado.");
        }

        var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, workspace.OrganizationId, cancellationToken);
        if (!canCreate)
        {
            return Result<Team>.Forbidden("Apenas o proprietário da organização pode criar times.");
        }

        if (request.ParentTeamId.HasValue)
        {
            var parentTeam = await teamRepository.GetByIdAsync(request.ParentTeamId.Value, cancellationToken);
            if (parentTeam is null)
            {
                return Result<Team>.NotFound("Time pai não encontrado.");
            }

            if (parentTeam.WorkspaceId != request.WorkspaceId)
            {
                return Result<Team>.Failure("O time pai deve pertencer ao mesmo workspace.");
            }
        }

        var leaderValidation = await TeamLeaderValidation.ValidateAsync(
            collaboratorRepository,
            request.LeaderId,
            workspace.OrganizationId,
            cancellationToken);
        if (leaderValidation is not null)
        {
            return leaderValidation;
        }

        try
        {
            var team = Team.Create(
                Guid.NewGuid(),
                workspace.OrganizationId,
                request.WorkspaceId,
                request.Name,
                request.LeaderId,
                request.ParentTeamId);

            team.CollaboratorTeams.Add(new CollaboratorTeam
            {
                CollaboratorId = request.LeaderId,
                TeamId = team.Id,
                AssignedAt = DateTime.UtcNow
            });

            await teamRepository.AddAsync(team, cancellationToken);
            await unitOfWork.CommitAsync(teamRepository.SaveChangesAsync, cancellationToken);

            return Result<Team>.Success(team);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Team>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

