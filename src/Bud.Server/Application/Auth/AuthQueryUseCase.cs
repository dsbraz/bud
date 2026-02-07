using Bud.Shared.Contracts;

namespace Bud.Server.Application.Auth;

public sealed class AuthQueryUseCase(IAuthService authService) : IAuthQueryUseCase
{
    public Task<ServiceResult<List<OrganizationSummaryDto>>> GetMyOrganizationsAsync(string email, CancellationToken cancellationToken = default)
        => authService.GetMyOrganizationsAsync(email, cancellationToken);
}
