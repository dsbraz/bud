using Bud.Server.Domain.ReadModels;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Ports;

public interface IAuthService
{
    Task<Result<AuthLoginResult>> LoginAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<Result<List<Bud.Server.Domain.ReadModels.OrganizationSummary>>> GetMyOrganizationsAsync(string email, CancellationToken cancellationToken = default);
}
