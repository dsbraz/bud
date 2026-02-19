namespace Bud.Server.Settings;

public sealed class RateLimitSettings
{
    public int LoginPermitLimit { get; set; } = 10;
    public int LoginWindowSeconds { get; set; } = 60;
}
