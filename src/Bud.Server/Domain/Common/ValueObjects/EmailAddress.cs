namespace Bud.Server.Domain.Common.ValueObjects;

public readonly record struct EmailAddress
{
    private EmailAddress(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static bool TryCreate(string? raw, out EmailAddress emailAddress)
    {
        emailAddress = default;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var normalized = raw.Trim().ToLowerInvariant();
        if (!IsValidFormat(normalized))
        {
            return false;
        }

        emailAddress = new EmailAddress(normalized);
        return true;
    }

    private static bool IsValidFormat(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0 || at != email.LastIndexOf('@'))
        {
            return false;
        }

        var domainPart = email[(at + 1)..];
        return !string.IsNullOrWhiteSpace(domainPart) && domainPart.Contains('.');
    }

    public override string ToString() => Value;
}
