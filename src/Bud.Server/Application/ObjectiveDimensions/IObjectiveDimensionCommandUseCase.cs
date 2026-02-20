using System.Security.Claims;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.ObjectiveDimensions;

public interface IObjectiveDimensionCommandUseCase
{
    Task<Result<ObjectiveDimension>> CreateAsync(
        ClaimsPrincipal user,
        CreateObjectiveDimensionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ObjectiveDimension>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateObjectiveDimensionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);
}
