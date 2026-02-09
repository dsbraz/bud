using System.Security.Claims;
using Bud.Server.Application.Common.Authorization;
using Bud.Server.Application.Common.Events;
using Bud.Server.Application.Common.Pipeline;
using Bud.Server.Application.Common.ReadModel;
using Bud.Server.Domain.Teams.Events;
using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Teams;

public sealed class TeamCommandUseCase(
    ITeamService teamService,
    IApplicationAuthorizationGateway authorizationGateway,
    IApplicationEntityLookup entityLookup,
    IUseCasePipeline? useCasePipeline = null,
    IDomainEventDispatcher? domainEventDispatcher = null) : ITeamCommandUseCase
{
    private readonly IUseCasePipeline _useCasePipeline = useCasePipeline ?? NoOpUseCasePipeline.Instance;
    private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher ?? NoOpDomainEventDispatcher.Instance;

    public async Task<ServiceResult<Team>> CreateAsync(
        ClaimsPrincipal user,
        CreateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(TeamCommandUseCase), nameof(CreateAsync)),
            async ct =>
            {
                var workspace = await entityLookup.GetWorkspaceAsync(request.WorkspaceId, ct);

                if (workspace is null)
                {
                    return ServiceResult<Team>.NotFound("Workspace não encontrado.");
                }

                var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, workspace.OrganizationId, ct);
                if (!canCreate)
                {
                    return ServiceResult<Team>.Forbidden("Apenas o proprietário da organização pode criar times.");
                }

                var createResult = await teamService.CreateAsync(request, ct);
                if (createResult.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new TeamCreatedDomainEvent(
                            createResult.Value!.Id,
                            createResult.Value.OrganizationId,
                            createResult.Value.WorkspaceId),
                        ct);
                }

                return createResult;
            },
            cancellationToken);
    }

    public async Task<ServiceResult<Team>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(TeamCommandUseCase), nameof(UpdateAsync)),
            async ct =>
            {
                var team = await entityLookup.GetTeamAsync(id, ct);

                if (team is null)
                {
                    return ServiceResult<Team>.NotFound("Time não encontrado.");
                }

                var canUpdate = await authorizationGateway.CanWriteOrganizationAsync(user, team.OrganizationId, ct);
                if (!canUpdate)
                {
                    return ServiceResult<Team>.Forbidden("Você não tem permissão para atualizar este time.");
                }

                var result = await teamService.UpdateAsync(id, request, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new TeamUpdatedDomainEvent(result.Value!.Id, result.Value.OrganizationId, result.Value.WorkspaceId),
                        ct);
                }

                return result;
            },
            cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(TeamCommandUseCase), nameof(DeleteAsync)),
            async ct =>
            {
                var team = await entityLookup.GetTeamAsync(id, ct);

                if (team is null)
                {
                    return ServiceResult.NotFound("Time não encontrado.");
                }

                var canDelete = await authorizationGateway.CanWriteOrganizationAsync(user, team.OrganizationId, ct);
                if (!canDelete)
                {
                    return ServiceResult.Forbidden("Você não tem permissão para excluir este time.");
                }

                var result = await teamService.DeleteAsync(id, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new TeamDeletedDomainEvent(team.Id, team.OrganizationId, team.WorkspaceId),
                        ct);
                }

                return result;
            },
            cancellationToken);
    }

    public async Task<ServiceResult> UpdateCollaboratorsAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateTeamCollaboratorsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(TeamCommandUseCase), nameof(UpdateCollaboratorsAsync)),
            async ct =>
            {
                var team = await entityLookup.GetTeamAsync(id, ct);

                if (team is null)
                {
                    return ServiceResult.NotFound("Time não encontrado.");
                }

                var canManage = await authorizationGateway.IsOrganizationOwnerAsync(user, team.OrganizationId, ct);
                if (!canManage)
                {
                    return ServiceResult.Forbidden("Apenas o proprietário da organização pode atribuir colaboradores.");
                }

                return await teamService.UpdateCollaboratorsAsync(id, request, ct);
            },
            cancellationToken);
    }
}
