using System.Text;
using System.Threading.RateLimiting;
using Bud.Server.Authorization;
using Bud.Server.Authorization.Handlers;
using Bud.Server.Authorization.Requirements;
using Bud.Server.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bud.Server.DependencyInjection;

public static class BudSecurityCompositionExtensions
{
    private const string DevFallbackKey = "dev-secret-key-change-in-production-minimum-32-characters-required";

    public static IServiceCollection AddBudAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

        var jwtKey = jwtSettings.Key;
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "A chave JWT (Jwt:Key) é obrigatória em ambientes que não sejam Development. " +
                    "Configure via variável de ambiente Jwt__Key ou Secret Manager.");
            }

            jwtKey = DevFallbackKey;
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        return services;
    }

    public static IServiceCollection AddBudAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.TenantSelected, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new TenantSelectedRequirement());
            });
            options.AddPolicy(AuthorizationPolicies.GlobalAdmin, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new GlobalAdminRequirement());
            });
            options.AddPolicy(AuthorizationPolicies.TenantOrganizationMatch, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new TenantOrganizationMatchRequirement());
            });
            options.AddPolicy(AuthorizationPolicies.OrganizationOwner, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new OrganizationOwnerRequirement());
            });
            options.AddPolicy(AuthorizationPolicies.OrganizationWrite, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new OrganizationWriteRequirement());
            });
            options.AddPolicy(AuthorizationPolicies.MissionScopeAccess, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new MissionScopeAccessRequirement());
            });
        });

        services.AddScoped<IAuthorizationHandler, TenantSelectedHandler>();
        services.AddScoped<IAuthorizationHandler, GlobalAdminHandler>();
        services.AddScoped<IAuthorizationHandler, TenantOrganizationMatchHandler>();
        services.AddScoped<IAuthorizationHandler, OrganizationOwnerHandler>();
        services.AddScoped<IAuthorizationHandler, OrganizationWriteHandler>();
        services.AddScoped<IAuthorizationHandler, MissionScopeAccessHandler>();

        services.AddScoped<ITenantAuthorizationService, TenantAuthorizationService>();
        services.AddScoped<IOrganizationAuthorizationService, OrganizationAuthorizationService>();

        return services;
    }

    public static IServiceCollection AddBudRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("RateLimitSettings").Get<RateLimitSettings>() ?? new RateLimitSettings();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Muitas requisições",
                    status = 429,
                    detail = "Limite de requisições excedido. Tente novamente em alguns instantes."
                }, cancellationToken);
            };

            options.AddFixedWindowLimiter("auth-login", limiterOptions =>
            {
                limiterOptions.PermitLimit = settings.LoginPermitLimit;
                limiterOptions.Window = TimeSpan.FromSeconds(settings.LoginWindowSeconds);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });
        });

        return services;
    }
}
