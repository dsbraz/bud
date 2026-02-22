using Bud.Server.Application.Common;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Me;

public sealed class ListMyOrganizations(IAuthService authService)
{
    public async Task<Result<List<OrganizationSummaryDto>>> ExecuteAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.GetMyOrganizationsAsync(email, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<List<OrganizationSummaryDto>>.Failure(result.Error ?? "Falha ao carregar organizações.", result.ErrorType);
        }

        return Result<List<OrganizationSummaryDto>>.Success(result.Value!.Select(o => o.ToContract()).ToList());
    }
}
