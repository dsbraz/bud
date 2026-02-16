using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Services;

public interface IObjectiveDimensionService
{
    Task<ServiceResult<ObjectiveDimension>> CreateAsync(
        CreateObjectiveDimensionRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ObjectiveDimension>> UpdateAsync(
        Guid id,
        UpdateObjectiveDimensionRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ObjectiveDimension>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<ObjectiveDimension>>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
