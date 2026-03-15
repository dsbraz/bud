using Bud.Application.Common;

namespace Bud.Application.Me;

public interface IMyOrganizationsReadStore
{
    Task<Result<List<OrganizationSnapshot>>> GetMyOrganizationsAsync(
        string email,
        CancellationToken cancellationToken = default);
}
