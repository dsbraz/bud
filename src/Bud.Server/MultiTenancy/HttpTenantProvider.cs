using Bud.Server.Settings;
using Microsoft.Extensions.Options;

namespace Bud.Server.MultiTenancy;

public sealed class HttpTenantProvider : ITenantProvider
{
    public Guid? TenantId { get; }
    public Guid? CollaboratorId { get; }
    public bool IsAdmin { get; }

    public HttpTenantProvider(
        IHttpContextAccessor httpContextAccessor,
        IOptions<AdminSettings> adminSettings)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null) return;

        var email = context.Request.Headers["X-User-Email"].FirstOrDefault();
        IsAdmin = !string.IsNullOrEmpty(email) &&
            string.Equals(email, adminSettings.Value.Email, StringComparison.OrdinalIgnoreCase);

        var tenantHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (Guid.TryParse(tenantHeader, out var tenantId))
        {
            TenantId = tenantId;
        }

        var collaboratorHeader = context.Request.Headers["X-Collaborator-Id"].FirstOrDefault();
        if (Guid.TryParse(collaboratorHeader, out var collaboratorId))
        {
            CollaboratorId = collaboratorId;
        }
    }
}
