using System.Security.Claims;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.ObjectiveDimensions;

public interface IObjectiveDimensionCommandUseCase
{
    Task<ServiceResult<ObjectiveDimension>> CreateAsync(
        ClaimsPrincipal user,
        CreateObjectiveDimensionRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ObjectiveDimension>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateObjectiveDimensionRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);
}
