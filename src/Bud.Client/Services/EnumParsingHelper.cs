namespace Bud.Client.Services;

public static class EnumParsingHelper
{
    public static bool TryParseEnum<TEnum>(string? rawValue, out TEnum parsed)
        where TEnum : struct, Enum
    {
        parsed = default;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        return Enum.TryParse(rawValue, ignoreCase: true, out parsed);
    }
}
