using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.MultiTenancy;

public sealed class TenantRequiredMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/logout"
    };

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip non-API paths and excluded paths
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
            ExcludedPaths.Contains(path))
        {
            await next(context);
            return;
        }

        // Admin can access without tenant ID
        if (tenantProvider.IsAdmin)
        {
            await next(context);
            return;
        }

        // Non-admin must have a tenant ID
        if (tenantProvider.TenantId is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 401,
                Title = "Authentication required",
                Detail = "You must be logged in to access this resource."
            });
            return;
        }

        await next(context);
    }
}
