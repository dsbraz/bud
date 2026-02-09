using System.Text;
using Bud.Server.Authorization;
using Bud.Server.Authorization.Handlers;
using Bud.Server.Authorization.Requirements;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Bud.Server.DependencyInjection;

public static class BudSecurityCompositionExtensions
{
    public static IServiceCollection AddBudAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"] ?? "dev-secret-key-change-in-production-minimum-32-characters-required";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "bud-dev";
        var jwtAudience = configuration["Jwt:Audience"] ?? "bud-api";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
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

        return services;
    }
}
