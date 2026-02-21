using Bud.Server.Infrastructure.Services;
using Bud.Server.Application.Common;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Auth;

public sealed class AuthQuery(IAuthService authService)
{
    public async Task<Result<List<OrganizationSummaryDto>>> GetMyOrganizationsAsync(string email, CancellationToken cancellationToken = default)
    {
        var result = await authService.GetMyOrganizationsAsync(email, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<List<OrganizationSummaryDto>>.Failure(result.Error ?? "Falha ao carregar organizações.", result.ErrorType);
        }

        return Result<List<OrganizationSummaryDto>>.Success(result.Value!.Select(o => o.ToContract()).ToList());
    }
}
