using Bud.Application;
using Bud.Infrastructure;

namespace Bud.Api.DependencyInjection;

public static class BudCompositionExtensions
{
    public static IServiceCollection AddBudPlatform(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        return services
            .AddBudObservability(configuration, environment)
            .AddBudApi(configuration)
            .AddBudSettings(configuration)
            .AddBudAuthentication(configuration, environment)
            .AddBudAuthorization()
            .AddBudRateLimiting(configuration)
            .AddBudInfrastructure(configuration)
            .AddBudApplication();
    }
}
