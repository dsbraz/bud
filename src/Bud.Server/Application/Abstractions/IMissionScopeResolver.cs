using Bud.Shared.Models;

namespace Bud.Server.Application.Abstractions;

public interface IMissionScopeResolver
{
    Task<ServiceResult<Guid>> ResolveScopeOrganizationIdAsync(
        MissionScopeType scopeType,
        Guid scopeId,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default);
}
