using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Common;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.ObjectiveDimensions;

public sealed class ObjectiveDimensionQueryUseCase(
    IObjectiveDimensionRepository dimensionRepository) : IObjectiveDimensionQueryUseCase
{
    public async Task<Result<ObjectiveDimension>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var dimension = await dimensionRepository.GetByIdAsync(id, cancellationToken);
        return dimension is null
            ? Result<ObjectiveDimension>.NotFound("Dimensão do objetivo não encontrada.")
            : Result<ObjectiveDimension>.Success(dimension);
    }

    public async Task<Result<PagedResult<ObjectiveDimension>>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await dimensionRepository.GetAllAsync(search, page, pageSize, cancellationToken);
        return Result<PagedResult<ObjectiveDimension>>.Success(result);
    }
}
