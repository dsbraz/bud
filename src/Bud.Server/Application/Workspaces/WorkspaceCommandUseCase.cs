using System.Security.Claims;
using Bud.Server.Application.Abstractions;
using Bud.Server.Application.Common.Authorization;
using Bud.Server.Application.Common.Events;
using Bud.Server.Application.Common.Pipeline;
using Bud.Server.Application.Common.ReadModel;
using Bud.Server.Domain.Workspaces.Events;
using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Workspaces;

public sealed class WorkspaceCommandUseCase(
    IWorkspaceCommandService workspaceService,
    IApplicationAuthorizationGateway authorizationGateway,
    IApplicationEntityLookup entityLookup,
    IUseCasePipeline? useCasePipeline = null,
    IDomainEventDispatcher? domainEventDispatcher = null) : IWorkspaceCommandUseCase
{
    private readonly IUseCasePipeline _useCasePipeline = useCasePipeline ?? NoOpUseCasePipeline.Instance;
    private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher ?? NoOpDomainEventDispatcher.Instance;

    public async Task<ServiceResult<Workspace>> CreateAsync(
        ClaimsPrincipal user,
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(WorkspaceCommandUseCase), nameof(CreateAsync)),
            async ct =>
            {
                var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, request.OrganizationId, ct);
                if (!canCreate)
                {
                    return ServiceResult<Workspace>.Forbidden("Apenas o proprietário da organização pode criar workspaces.");
                }

                var result = await workspaceService.CreateAsync(request, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new WorkspaceCreatedDomainEvent(result.Value!.Id, result.Value.OrganizationId),
                        ct);
                }

                return result;
            },
            cancellationToken);
    }

    public async Task<ServiceResult<Workspace>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(WorkspaceCommandUseCase), nameof(UpdateAsync)),
            async ct =>
            {
                var workspace = await entityLookup.GetWorkspaceAsync(id, ct);

                if (workspace is null)
                {
                    return ServiceResult<Workspace>.NotFound("Workspace não encontrado.");
                }

                var canUpdate = await authorizationGateway.CanWriteOrganizationAsync(user, workspace.OrganizationId, ct);
                if (!canUpdate)
                {
                    return ServiceResult<Workspace>.Forbidden("Você não tem permissão para atualizar este workspace.");
                }

                var result = await workspaceService.UpdateAsync(id, request, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new WorkspaceUpdatedDomainEvent(result.Value!.Id, result.Value.OrganizationId),
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
            new UseCaseExecutionContext(nameof(WorkspaceCommandUseCase), nameof(DeleteAsync)),
            async ct =>
            {
                var workspace = await entityLookup.GetWorkspaceAsync(id, ct);

                if (workspace is null)
                {
                    return ServiceResult.NotFound("Workspace não encontrado.");
                }

                var canDelete = await authorizationGateway.CanWriteOrganizationAsync(user, workspace.OrganizationId, ct);
                if (!canDelete)
                {
                    return ServiceResult.Forbidden("Você não tem permissão para excluir este workspace.");
                }

                var result = await workspaceService.DeleteAsync(id, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new WorkspaceDeletedDomainEvent(workspace.Id, workspace.OrganizationId),
                        ct);
                }

                return result;
            },
            cancellationToken);
    }
}
