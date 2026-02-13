namespace Bud.Shared.Domain;

public readonly record struct PersonName
{
    private PersonName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static bool TryCreate(string? raw, out PersonName personName)
    {
        personName = default;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var normalized = Normalize(raw);
        if (normalized.Length < 2)
        {
            return false;
        }

        personName = new PersonName(normalized);
        return true;
    }

    private static string Normalize(string raw)
        => string.Join(' ', raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public override string ToString() => Value;
}
