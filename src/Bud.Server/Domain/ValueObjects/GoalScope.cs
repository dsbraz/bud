using Bud.Server.Domain.Model;

namespace Bud.Server.Domain.ValueObjects;

public readonly record struct GoalScope
{
    private GoalScope(GoalScopeType scopeType, Guid? scopeId)
    {
        ScopeType = scopeType;
        ScopeId = scopeId;
    }

    public GoalScopeType ScopeType { get; }
    public Guid? ScopeId { get; }

    public static bool TryCreate(GoalScopeType scopeType, Guid scopeId, out GoalScope scope)
    {
        scope = default;

        if (scopeType == GoalScopeType.Organization)
        {
            scope = new GoalScope(scopeType, null);
            return true;
        }

        if (scopeId == Guid.Empty)
        {
            return false;
        }

        scope = new GoalScope(scopeType, scopeId);
        return true;
    }

    public static GoalScope Create(GoalScopeType scopeType, Guid scopeId)
    {
        if (!TryCreate(scopeType, scopeId, out var scope))
        {
            throw new DomainInvariantException("Escopo da meta inválido.");
        }

        return scope;
    }
}
