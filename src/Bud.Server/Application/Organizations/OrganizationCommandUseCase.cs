using Bud.Server.Application.Common.Events;
using Bud.Server.Application.Common.Pipeline;
using Bud.Server.Domain.Organizations.Events;
using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Organizations;

public sealed class OrganizationCommandUseCase(
    IOrganizationService organizationService,
    IUseCasePipeline? useCasePipeline = null,
    IDomainEventDispatcher? domainEventDispatcher = null) : IOrganizationCommandUseCase
{
    private readonly IUseCasePipeline _useCasePipeline = useCasePipeline ?? NoOpUseCasePipeline.Instance;
    private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher ?? NoOpDomainEventDispatcher.Instance;

    public async Task<ServiceResult<Organization>> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(OrganizationCommandUseCase), nameof(CreateAsync)),
            async ct =>
            {
                var result = await organizationService.CreateAsync(request, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new OrganizationCreatedDomainEvent(result.Value!.Id),
                        ct);
                }

                return result;
            },
            cancellationToken);
    }

    public async Task<ServiceResult<Organization>> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(OrganizationCommandUseCase), nameof(UpdateAsync)),
            async ct =>
            {
                var result = await organizationService.UpdateAsync(id, request, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new OrganizationUpdatedDomainEvent(result.Value!.Id),
                        ct);
                }

                return result;
            },
            cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _useCasePipeline.ExecuteAsync(
            new UseCaseExecutionContext(nameof(OrganizationCommandUseCase), nameof(DeleteAsync)),
            async ct =>
            {
                var result = await organizationService.DeleteAsync(id, ct);
                if (result.IsSuccess)
                {
                    await _domainEventDispatcher.DispatchAsync(
                        new OrganizationDeletedDomainEvent(id),
                        ct);
                }

                return result;
            },
            cancellationToken);
    }
}
