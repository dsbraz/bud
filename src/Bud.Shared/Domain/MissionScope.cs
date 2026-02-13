namespace Bud.Shared.Domain;

public readonly record struct MissionScope
{
    private MissionScope(MissionScopeType scopeType, Guid? scopeId)
    {
        ScopeType = scopeType;
        ScopeId = scopeId;
    }

    public MissionScopeType ScopeType { get; }
    public Guid? ScopeId { get; }

    public static bool TryCreate(MissionScopeType scopeType, Guid scopeId, out MissionScope scope)
    {
        scope = default;

        if (scopeType == MissionScopeType.Organization)
        {
            scope = new MissionScope(scopeType, null);
            return true;
        }

        if (scopeId == Guid.Empty)
        {
            return false;
        }

        scope = new MissionScope(scopeType, scopeId);
        return true;
    }

    public static MissionScope Create(MissionScopeType scopeType, Guid scopeId)
    {
        if (!TryCreate(scopeType, scopeId, out var scope))
        {
            throw new DomainInvariantException("Escopo da missão inválido.");
        }

        return scope;
    }
}
