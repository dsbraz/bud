using Bud.Shared.Domain;
using Bud.Server.Application.Common;

namespace Bud.Server.Infrastructure.Services;

public interface IMissionScopeResolver
{
    Task<Result<Guid>> ResolveScopeOrganizationIdAsync(
        MissionScopeType scopeType,
        Guid scopeId,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default);
}
