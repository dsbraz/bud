using Microsoft.Extensions.Configuration;

namespace Bud.Mcp.Configuration;

public sealed record BudMcpOptions(
    string ApiBaseUrl,
    string? UserEmail,
    Guid? DefaultTenantId,
    int HttpTimeoutSeconds)
{
    public static BudMcpOptions FromEnvironment()
    {
        return FromConfiguration(new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build());
    }

    public static BudMcpOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("BudMcp");

        var apiBaseUrl = section["ApiBaseUrl"] ?? configuration["BUD_API_BASE_URL"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new InvalidOperationException("BudMcp:ApiBaseUrl (ou BUD_API_BASE_URL) é obrigatório.");
        }

        var userEmail = section["UserEmail"] ?? configuration["BUD_USER_EMAIL"];

        Guid? defaultTenantId = null;
        var defaultTenantRaw = section["DefaultTenantId"] ?? configuration["BUD_DEFAULT_TENANT_ID"];
        if (!string.IsNullOrWhiteSpace(defaultTenantRaw))
        {
            if (!Guid.TryParse(defaultTenantRaw, out var parsedTenantId))
            {
                throw new InvalidOperationException("BudMcp:DefaultTenantId (ou BUD_DEFAULT_TENANT_ID) deve ser um GUID válido.");
            }

            defaultTenantId = parsedTenantId;
        }

        var timeoutSeconds = 30;
        var timeoutRaw = section["HttpTimeoutSeconds"] ?? configuration["BUD_HTTP_TIMEOUT_SECONDS"];
        if (!string.IsNullOrWhiteSpace(timeoutRaw))
        {
            if (!int.TryParse(timeoutRaw, out timeoutSeconds) || timeoutSeconds <= 0)
            {
                throw new InvalidOperationException("BudMcp:HttpTimeoutSeconds (ou BUD_HTTP_TIMEOUT_SECONDS) deve ser um número inteiro positivo.");
            }
        }

        return new BudMcpOptions(apiBaseUrl, string.IsNullOrWhiteSpace(userEmail) ? null : userEmail.Trim(), defaultTenantId, timeoutSeconds);
    }
}
