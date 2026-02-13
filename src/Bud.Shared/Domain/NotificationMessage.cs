namespace Bud.Shared.Domain;

public readonly record struct NotificationMessage
{
    private const int MaxLength = 1000;

    private NotificationMessage(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static bool TryCreate(string? raw, out NotificationMessage message)
    {
        message = default;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var normalized = raw.Trim();
        if (normalized.Length > MaxLength)
        {
            return false;
        }

        message = new NotificationMessage(normalized);
        return true;
    }

    public override string ToString() => Value;
}
