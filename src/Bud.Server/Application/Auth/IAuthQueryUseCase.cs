using Bud.Shared.Contracts;
using Bud.Server.Application.Common;

namespace Bud.Server.Application.Auth;

public interface IAuthQueryUseCase
{
    Task<Result<List<OrganizationSummaryDto>>> GetMyOrganizationsAsync(string email, CancellationToken cancellationToken = default);
}
