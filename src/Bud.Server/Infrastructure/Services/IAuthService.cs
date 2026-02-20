using Bud.Server.Domain.ReadModels;
using Bud.Shared.Contracts;
using Bud.Server.Application.Common;

namespace Bud.Server.Infrastructure.Services;

public interface IAuthService
{
    Task<Result<AuthLoginResult>> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<List<OrganizationSummary>>> GetMyOrganizationsAsync(string email, CancellationToken cancellationToken = default);
}
