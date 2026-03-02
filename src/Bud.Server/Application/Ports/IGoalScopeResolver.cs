using Bud.Server.Application.Common;

namespace Bud.Server.Application.Ports;

public interface IGoalScopeResolver
{
    Task<Result<Guid>> ResolveScopeOrganizationIdAsync(
        GoalScopeType scopeType,
        Guid scopeId,
        bool ignoreQueryFilters = false,
        CancellationToken ct = default);
}
