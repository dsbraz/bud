using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Application.Ports;

namespace Bud.Server.Application.UseCases.Me;

public sealed class ListMyOrganizations(IAuthService authService)
{
    public async Task<Result<List<MyOrganizationResponse>>> ExecuteAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.GetMyOrganizationsAsync(email, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<List<MyOrganizationResponse>>.Failure(result.Error ?? UserErrorMessages.ListOrganizationsFailed, result.ErrorType);
        }

        return Result<List<MyOrganizationResponse>>.Success(result.Value!.Select(o => o.ToResponse()).ToList());
    }
}
