using System.Security.Claims;
using Bud.Server.Authorization;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionTemplates;

public sealed class MissionTemplateCommandUseCase(
    IMissionTemplateService templateService,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider) : IMissionTemplateCommandUseCase
{
    public async Task<ServiceResult<MissionTemplate>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.TenantId.HasValue)
        {
            var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, tenantProvider.TenantId.Value, cancellationToken);
            if (!canCreate)
            {
                return ServiceResult<MissionTemplate>.Forbidden("Você não tem permissão para criar templates nesta organização.");
            }
        }

        return await templateService.CreateAsync(request, cancellationToken);
    }

    public async Task<ServiceResult<MissionTemplate>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await templateService.GetByIdAsync(id, cancellationToken);
        if (!existing.IsSuccess)
        {
            return ServiceResult<MissionTemplate>.NotFound("Template de missão não encontrado.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, existing.Value!.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return ServiceResult<MissionTemplate>.Forbidden("Você não tem permissão para atualizar templates nesta organização.");
        }

        return await templateService.UpdateAsync(id, request, cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var existing = await templateService.GetByIdAsync(id, cancellationToken);
        if (!existing.IsSuccess)
        {
            return ServiceResult.NotFound("Template de missão não encontrado.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, existing.Value!.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return ServiceResult.Forbidden("Você não tem permissão para excluir templates nesta organização.");
        }

        return await templateService.DeleteAsync(id, cancellationToken);
    }
}
