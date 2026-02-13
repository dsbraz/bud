using Bud.Server.Data;
using Bud.Server.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.DependencyInjection;

public static class BudDataCompositionExtensions
{
    public static IServiceCollection AddBudDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not configured.");
        }

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, JwtTenantProvider>();
        services.AddScoped<TenantSaveChangesInterceptor>();

        services.AddScoped<ApplicationDbContext>(sp =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            optionsBuilder.AddInterceptors(sp.GetRequiredService<TenantSaveChangesInterceptor>());

            var tenantProvider = sp.GetRequiredService<ITenantProvider>();
            return new ApplicationDbContext(optionsBuilder.Options, tenantProvider);
        });

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgres", tags: ["db", "ready"]);

        return services;
    }
}
