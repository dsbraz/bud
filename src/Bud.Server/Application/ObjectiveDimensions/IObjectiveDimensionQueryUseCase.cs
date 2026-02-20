using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.ObjectiveDimensions;

public interface IObjectiveDimensionQueryUseCase
{
    Task<Result<ObjectiveDimension>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<ObjectiveDimension>>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
