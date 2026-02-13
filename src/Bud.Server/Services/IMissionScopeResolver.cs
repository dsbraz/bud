using Bud.Shared.Domain;

namespace Bud.Server.Services;

public interface IMissionScopeResolver
{
    Task<ServiceResult<Guid>> ResolveScopeOrganizationIdAsync(
        MissionScopeType scopeType,
        Guid scopeId,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default);
}
