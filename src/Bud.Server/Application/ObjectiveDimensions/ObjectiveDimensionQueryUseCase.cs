using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.ObjectiveDimensions;

public sealed class ObjectiveDimensionQueryUseCase(
    IObjectiveDimensionService objectiveDimensionService) : IObjectiveDimensionQueryUseCase
{
    public Task<ServiceResult<ObjectiveDimension>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => objectiveDimensionService.GetByIdAsync(id, cancellationToken);

    public Task<ServiceResult<PagedResult<ObjectiveDimension>>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => objectiveDimensionService.GetAllAsync(search, page, pageSize, cancellationToken);
}
