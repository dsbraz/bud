using System.Security.Claims;
using Bud.Server.Application.Common.Authorization;
using Bud.Server.Application.Common.Events;
using Bud.Server.Application.Common.Pipeline;
using Bud.Server.Domain.MissionTemplates.Events;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.MissionTemplates;

public sealed class MissionTemplateCommandUseCase(
    IMissionTemplateService templateService,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    IUseCasePipeline? useCasePipeline = null,
    IDomainEventDispatcher? domainEventDispatcher = null) : IMissionTemplateCommandUseCase
{
    private readonly IUseCasePipeline _useCasePipeline = useCasePipeline ?? NoOpUseCasePipeline.Instance;
    private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher ?? NoOpDomainEventDispatcher.Instance;

    public async Task<ServiceResult<MissionTemplate>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(MissionTemplateCommandUseCase), nameof(CreateAsync)),
            async ct =>
            {
                if (tenantProvider.TenantId.HasValue)
                {
                    var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, tenantProvider.TenantId.Value, ct);
                    if (!canCreate)
                    {
                        return ServiceResult<MissionTemplate>.Forbidden("Você não tem permissão para criar templates nesta organização.");
                    }
                }

                var createResult = await templateService.CreateAsync(request, ct);
                if (createResult.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new MissionTemplateCreatedDomainEvent(createResult.Value!.Id, createResult.Value.OrganizationId),
                        ct);
                }

                return createResult;
            },
            cancellationToken);
    }

    public async Task<ServiceResult<MissionTemplate>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(MissionTemplateCommandUseCase), nameof(UpdateAsync)),
            async ct =>
            {
                var existing = await templateService.GetByIdAsync(id, ct);
                if (!existing.IsSuccess)
                {
                    return ServiceResult<MissionTemplate>.NotFound("Template de missão não encontrado.");
                }

                var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, existing.Value!.OrganizationId, ct);
                if (!canUpdate)
                {
                    return ServiceResult<MissionTemplate>.Forbidden("Você não tem permissão para atualizar templates nesta organização.");
                }

                var result = await templateService.UpdateAsync(id, request, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new MissionTemplateUpdatedDomainEvent(result.Value!.Id, result.Value.OrganizationId),
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
            new UseCaseExecutionContext(nameof(MissionTemplateCommandUseCase), nameof(DeleteAsync)),
            async ct =>
            {
                var existing = await templateService.GetByIdAsync(id, ct);
                if (!existing.IsSuccess)
                {
                    return ServiceResult.NotFound("Template de missão não encontrado.");
                }

                var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, existing.Value!.OrganizationId, ct);
                if (!canDelete)
                {
                    return ServiceResult.Forbidden("Você não tem permissão para excluir templates nesta organização.");
                }

                var result = await templateService.DeleteAsync(id, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new MissionTemplateDeletedDomainEvent(existing.Value.Id, existing.Value.OrganizationId),
                        ct);
                }

                return result;
            },
            cancellationToken);
    }
}
