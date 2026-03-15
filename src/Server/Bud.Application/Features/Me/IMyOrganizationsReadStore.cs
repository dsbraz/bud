using Bud.Application.Common;

namespace Bud.Application.Features.Me;

public interface IMyOrganizationsReadStore
{
    Task<Result<List<OrganizationSnapshot>>> GetMyOrganizationsAsync(
        string email,
        CancellationToken cancellationToken = default);
}
