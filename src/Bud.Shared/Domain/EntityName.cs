namespace Bud.Shared.Domain;

public readonly record struct EntityName
{
    private const int MaxLength = 200;

    private EntityName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static bool TryCreate(string? raw, out EntityName entityName)
    {
        entityName = default;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var normalized = raw.Trim();
        if (normalized.Length > MaxLength)
        {
            return false;
        }

        entityName = new EntityName(normalized);
        return true;
    }

    public static EntityName Create(string? raw)
    {
        if (!TryCreate(raw, out var entityName))
        {
            throw new DomainInvariantException("O nome informado é inválido.");
        }

        return entityName;
    }

    public override string ToString() => Value;
}
