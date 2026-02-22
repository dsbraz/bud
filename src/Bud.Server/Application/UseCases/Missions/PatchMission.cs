using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Missions;

public sealed class PatchMission(
    IMissionRepository missionRepository,
    IMissionScopeResolver missionScopeResolver,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Mission>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchMissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var mission = await missionRepository.GetByIdAsync(id, cancellationToken);
        if (mission is null)
        {
            return Result<Mission>.NotFound("Missão não encontrada.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, mission.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return Result<Mission>.Forbidden("Você não tem permissão para atualizar missões nesta organização.");
        }

        try
        {
            var status = request.Status.HasValue ? request.Status.Value.ToDomain() : mission.Status;
            var scopeType = request.ScopeType.HasValue
                ? request.ScopeType.Value.ToDomain()
                : mission.WorkspaceId.HasValue
                    ? Bud.Server.Domain.Model.MissionScopeType.Workspace
                    : mission.TeamId.HasValue
                        ? Bud.Server.Domain.Model.MissionScopeType.Team
                        : mission.CollaboratorId.HasValue
                            ? Bud.Server.Domain.Model.MissionScopeType.Collaborator
                            : Bud.Server.Domain.Model.MissionScopeType.Organization;
            var name = request.Name.HasValue ? (request.Name.Value ?? mission.Name) : mission.Name;
            var description = request.Description.HasValue ? request.Description.Value : mission.Description;
            var startDate = request.StartDate.HasValue ? request.StartDate.Value : mission.StartDate;
            var endDate = request.EndDate.HasValue ? request.EndDate.Value : mission.EndDate;

            mission.UpdateDetails(
                name,
                description,
                NormalizeToUtc(startDate),
                NormalizeToUtc(endDate),
                status);

            var shouldUpdateScope = request.ScopeId.HasValue && request.ScopeId.Value != Guid.Empty;
            if (shouldUpdateScope)
            {
                var scopeId = request.ScopeId.Value;
                var missionScope = MissionScope.Create(scopeType, scopeId);

                var scopeResolution = await missionScopeResolver.ResolveScopeOrganizationIdAsync(
                    scopeType,
                    scopeId,
                    cancellationToken: cancellationToken);
                if (!scopeResolution.IsSuccess)
                {
                    return Result<Mission>.NotFound(scopeResolution.Error ?? "Escopo não encontrado.");
                }

                mission.OrganizationId = scopeResolution.Value;
                mission.SetScope(missionScope);
            }

            mission.MarkAsUpdated();
            await unitOfWork.CommitAsync(missionRepository.SaveChangesAsync, cancellationToken);

            return Result<Mission>.Success(mission);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Mission>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };
    }
}
