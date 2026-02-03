using Microsoft.AspNetCore.Mvc;

namespace Bud.Server.MultiTenancy;

public sealed class TenantRequiredMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/logout",
        "/api/auth/my-organizations"
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

        // Check if user is authenticated via X-User-Email header
        var userEmail = context.Request.Headers["X-User-Email"].FirstOrDefault();
        var isAuthenticated = !string.IsNullOrWhiteSpace(userEmail);

        // Allow access if user is authenticated (has email) or is admin
        // This allows "TODOS" mode (no tenant ID) for authenticated users
        if (isAuthenticated || tenantProvider.IsAdmin)
        {
            await next(context);
            return;
        }

        // Unauthenticated users are not allowed
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 401,
            Title = "Authentication required",
            Detail = "You must be logged in to access this resource."
        });
    }
}
