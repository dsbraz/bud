namespace Bud.Server.DependencyInjection;

public static class BudCompositionExtensions
{
    public static IServiceCollection AddBudPlatform(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        return services
            .AddBudObservability(configuration, environment)
            .AddBudApi()
            .AddBudSettings(configuration)
            .AddBudAuthentication(configuration, environment)
            .AddBudAuthorization()
            .AddBudRateLimiting(configuration)
            .AddBudInfrastructure(configuration)
            .AddBudApplication();
    }
}
