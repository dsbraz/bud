using Bud.Server.Services;
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

    public async Task InvokeAsync(
        HttpContext context,
        ITenantProvider tenantProvider,
        ITenantAuthorizationService tenantAuth)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Allow excluded auth paths
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
            ExcludedPaths.Contains(path))
        {
            await next(context);
            return;
        }

        // Check if user is authenticated via JWT (validated by ASP.NET Core)
        var isAuthenticated = context.User.Identity?.IsAuthenticated == true;

        if (!isAuthenticated)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Não autenticado",
                Detail = "É necessário autenticação para acessar este recurso."
            });
            return;
        }

        if (!tenantProvider.IsGlobalAdmin && !tenantProvider.TenantId.HasValue)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Acesso negado",
                Detail = "É necessário selecionar uma organização para acessar este recurso."
            });
            return;
        }

        // Validate tenant access (if X-Tenant-Id header was provided)
        if (tenantProvider.TenantId.HasValue && !tenantProvider.IsGlobalAdmin)
        {
            var hasAccess = await tenantAuth.UserBelongsToTenantAsync(
                tenantProvider.TenantId.Value,
                context.RequestAborted);

            if (!hasAccess)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Acesso negado",
                    Detail = "Você não tem permissão para acessar esta organização."
                });
                return;
            }
        }

        // User is authenticated and has access to the requested tenant
        await next(context);
    }
}
