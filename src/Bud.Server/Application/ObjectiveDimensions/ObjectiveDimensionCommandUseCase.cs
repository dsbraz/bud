using System.Security.Claims;
using Bud.Server.Authorization;
using Bud.Server.MultiTenancy;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.ObjectiveDimensions;

public sealed class ObjectiveDimensionCommandUseCase(
    IObjectiveDimensionService objectiveDimensionService,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider) : IObjectiveDimensionCommandUseCase
{
    public async Task<ServiceResult<ObjectiveDimension>> CreateAsync(
        ClaimsPrincipal user,
        CreateObjectiveDimensionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.TenantId.HasValue)
        {
            var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(
                user,
                tenantProvider.TenantId.Value,
                cancellationToken);

            if (!canCreate)
            {
                return ServiceResult<ObjectiveDimension>.Forbidden(
                    "Você não tem permissão para criar dimensões nesta organização.");
            }
        }

        return await objectiveDimensionService.CreateAsync(request, cancellationToken);
    }

    public async Task<ServiceResult<ObjectiveDimension>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateObjectiveDimensionRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await objectiveDimensionService.GetByIdAsync(id, cancellationToken);
        if (!existing.IsSuccess)
        {
            return ServiceResult<ObjectiveDimension>.NotFound("Dimensão do objetivo não encontrada.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(
            user,
            existing.Value!.OrganizationId,
            cancellationToken);
        if (!canUpdate)
        {
            return ServiceResult<ObjectiveDimension>.Forbidden(
                "Você não tem permissão para atualizar dimensões nesta organização.");
        }

        return await objectiveDimensionService.UpdateAsync(id, request, cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var existing = await objectiveDimensionService.GetByIdAsync(id, cancellationToken);
        if (!existing.IsSuccess)
        {
            return ServiceResult.NotFound("Dimensão do objetivo não encontrada.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(
            user,
            existing.Value!.OrganizationId,
            cancellationToken);
        if (!canDelete)
        {
            return ServiceResult.Forbidden(
                "Você não tem permissão para excluir dimensões nesta organização.");
        }

        return await objectiveDimensionService.DeleteAsync(id, cancellationToken);
    }
}
