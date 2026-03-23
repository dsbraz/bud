namespace Bud.Domain.ValueObjects;

public readonly record struct NotificationTitle
{
    private const int MaxLength = 200;

    private NotificationTitle(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static bool TryCreate(string? raw, out NotificationTitle title)
    {
        title = default;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var normalized = raw.Trim();
        if (normalized.Length > MaxLength)
        {
            return false;
        }

        title = new NotificationTitle(normalized);
        return true;
    }

    public static NotificationTitle Create(string? raw)
    {
        if (!TryCreate(raw, out var title))
        {
            throw new DomainInvariantException("O título da notificação é inválido.");
        }

        return title;
    }

    public override string ToString() => Value;
}
