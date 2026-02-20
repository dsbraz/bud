using System.Security.Claims;
using Bud.Server.Services;
using Bud.Server.Authorization;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Collaborators;

public sealed class CollaboratorCommandUseCase(
    ICollaboratorService collaboratorService,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    IEntityLookupService entityLookup) : ICollaboratorCommandUseCase
{
    public async Task<ServiceResult<Collaborator>> CreateAsync(
        ClaimsPrincipal user,
        CreateCollaboratorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.TenantId.HasValue)
        {
            var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, tenantProvider.TenantId.Value, cancellationToken);
            if (!canCreate)
            {
                return ServiceResult<Collaborator>.Forbidden("Apenas o proprietário da organização pode criar colaboradores.");
            }
        }

        return await collaboratorService.CreateAsync(request, cancellationToken);
    }

    public async Task<ServiceResult<Collaborator>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateCollaboratorRequest request,
        CancellationToken cancellationToken = default)
    {
        var collaborator = await entityLookup.GetCollaboratorAsync(id, cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult<Collaborator>.NotFound("Colaborador não encontrado.");
        }

        var canUpdate = await authorizationGateway.IsOrganizationOwnerAsync(user, collaborator.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return ServiceResult<Collaborator>.Forbidden("Apenas o proprietário da organização pode editar colaboradores.");
        }

        return await collaboratorService.UpdateAsync(id, request, cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var collaborator = await entityLookup.GetCollaboratorAsync(id, cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult.NotFound("Colaborador não encontrado.");
        }

        var canDelete = await authorizationGateway.IsOrganizationOwnerAsync(user, collaborator.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return ServiceResult.Forbidden("Apenas o proprietário da organização pode excluir colaboradores.");
        }

        return await collaboratorService.DeleteAsync(id, cancellationToken);
    }

    public async Task<ServiceResult> UpdateTeamsAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateCollaboratorTeamsRequest request,
        CancellationToken cancellationToken = default)
    {
        var collaborator = await entityLookup.GetCollaboratorAsync(id, cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult.NotFound("Colaborador não encontrado.");
        }

        var canAssign = await authorizationGateway.IsOrganizationOwnerAsync(user, collaborator.OrganizationId, cancellationToken);
        if (!canAssign)
        {
            return ServiceResult.Forbidden("Apenas o proprietário da organização pode atribuir equipes.");
        }

        return await collaboratorService.UpdateTeamsAsync(id, request, cancellationToken);
    }
}
