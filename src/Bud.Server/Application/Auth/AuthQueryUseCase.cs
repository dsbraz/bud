using Bud.Server.Application.Common;
using Bud.Server.Services;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Auth;

public sealed class AuthQueryUseCase(IAuthService authService) : IAuthQueryUseCase
{
    public async Task<ServiceResult<List<OrganizationSummaryDto>>> GetMyOrganizationsAsync(string email, CancellationToken cancellationToken = default)
    {
        var result = await authService.GetMyOrganizationsAsync(email, cancellationToken);
        if (!result.IsSuccess)
        {
            return ServiceResult<List<OrganizationSummaryDto>>.Failure(result.Error ?? "Falha ao carregar organizações.", result.ErrorType);
        }

        return ServiceResult<List<OrganizationSummaryDto>>.Success(result.Value!.Select(o => o.ToContract()).ToList());
    }
}
