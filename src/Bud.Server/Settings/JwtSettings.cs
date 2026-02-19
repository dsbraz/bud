namespace Bud.Server.Settings;

public sealed class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "bud-dev";
    public string Audience { get; set; } = "bud-api";
    public int TokenExpirationHours { get; set; } = 8;
}
