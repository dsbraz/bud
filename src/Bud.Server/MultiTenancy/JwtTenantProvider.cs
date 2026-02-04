using System.Security.Claims;

namespace Bud.Server.MultiTenancy;

public sealed class JwtTenantProvider : ITenantProvider
{
    public Guid? TenantId { get; }
    public Guid? CollaboratorId { get; }
    public bool IsGlobalAdmin { get; }
    public string? UserEmail { get; }

    public JwtTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var user = httpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // Claims vêm do JWT VALIDADO pelo ASP.NET Core
        UserEmail = user.FindFirst("email")?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value;

        // Tenant (pode ser enviado via header X-Tenant-Id ou estar no claim)
        var tenantHeader = httpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (Guid.TryParse(tenantHeader, out var tenantId))
        {
            TenantId = tenantId;
        }
        else
        {
            // Fallback: usar organization_id do claim (para single-org users)
            var orgClaim = user.FindFirst("organization_id")?.Value;
            if (Guid.TryParse(orgClaim, out var orgId))
            {
                TenantId = orgId;
            }
        }

        var collaboratorClaim = user.FindFirst("collaborator_id")?.Value;
        if (Guid.TryParse(collaboratorClaim, out var collabId))
        {
            CollaboratorId = collabId;
        }

        IsGlobalAdmin = user.IsInRole("GlobalAdmin");
    }
}
