using Bud.Server.Application.Common.Authorization;
using Bud.Server.Application.Common.Events;
using Bud.Server.Application.Common.Pipeline;
using Bud.Server.Application.Common.ReadModel;
using Bud.Server.Domain.Collaborators.Events;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using System.Security.Claims;

namespace Bud.Server.Application.Collaborators;

public sealed class CollaboratorCommandUseCase(
    ICollaboratorService collaboratorService,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    IApplicationEntityLookup entityLookup,
    IUseCasePipeline? useCasePipeline = null,
    IDomainEventDispatcher? domainEventDispatcher = null) : ICollaboratorCommandUseCase
{
    private readonly IUseCasePipeline _useCasePipeline = useCasePipeline ?? NoOpUseCasePipeline.Instance;
    private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher ?? NoOpDomainEventDispatcher.Instance;

    public async Task<ServiceResult<Collaborator>> CreateAsync(
        ClaimsPrincipal user,
        CreateCollaboratorRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(CollaboratorCommandUseCase), nameof(CreateAsync)),
            async ct =>
            {
                if (tenantProvider.TenantId.HasValue)
                {
                    var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, tenantProvider.TenantId.Value, ct);
                    if (!canCreate)
                    {
                        return ServiceResult<Collaborator>.Forbidden("Apenas o proprietário da organização pode criar colaboradores.");
                    }
                }

                var createResult = await collaboratorService.CreateAsync(request, ct);
                if (createResult.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new CollaboratorCreatedDomainEvent(createResult.Value!.Id, createResult.Value.OrganizationId),
                        ct);
                }

                return createResult;
            },
            cancellationToken);
    }

    public async Task<ServiceResult<Collaborator>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateCollaboratorRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(CollaboratorCommandUseCase), nameof(UpdateAsync)),
            async ct =>
            {
                var collaborator = await entityLookup.GetCollaboratorAsync(id, ct);

                if (collaborator is null)
                {
                    return ServiceResult<Collaborator>.NotFound("Colaborador não encontrado.");
                }

                var canUpdate = await authorizationGateway.IsOrganizationOwnerAsync(user, collaborator.OrganizationId, ct);
                if (!canUpdate)
                {
                    return ServiceResult<Collaborator>.Forbidden("Apenas o proprietário da organização pode editar colaboradores.");
                }

                var result = await collaboratorService.UpdateAsync(id, request, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new CollaboratorUpdatedDomainEvent(result.Value!.Id, result.Value.OrganizationId),
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
            new UseCaseExecutionContext(nameof(CollaboratorCommandUseCase), nameof(DeleteAsync)),
            async ct =>
            {
                var collaborator = await entityLookup.GetCollaboratorAsync(id, ct);

                if (collaborator is null)
                {
                    return ServiceResult.NotFound("Colaborador não encontrado.");
                }

                var canDelete = await authorizationGateway.IsOrganizationOwnerAsync(user, collaborator.OrganizationId, ct);
                if (!canDelete)
                {
                    return ServiceResult.Forbidden("Apenas o proprietário da organização pode excluir colaboradores.");
                }

                var result = await collaboratorService.DeleteAsync(id, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new CollaboratorDeletedDomainEvent(collaborator.Id, collaborator.OrganizationId),
                        ct);
                }

                return result;
            },
            cancellationToken);
    }

    public async Task<ServiceResult> UpdateTeamsAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateCollaboratorTeamsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(CollaboratorCommandUseCase), nameof(UpdateTeamsAsync)),
            async ct =>
            {
                var collaborator = await entityLookup.GetCollaboratorAsync(id, ct);

                if (collaborator is null)
                {
                    return ServiceResult.NotFound("Colaborador não encontrado.");
                }

                var canAssign = await authorizationGateway.IsOrganizationOwnerAsync(user, collaborator.OrganizationId, ct);
                if (!canAssign)
                {
                    return ServiceResult.Forbidden("Apenas o proprietário da organização pode atribuir equipes.");
                }

                return await collaboratorService.UpdateTeamsAsync(id, request, ct);
            },
            cancellationToken);
    }
}
