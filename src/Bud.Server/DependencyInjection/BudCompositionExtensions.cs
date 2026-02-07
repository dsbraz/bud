namespace Bud.Server.DependencyInjection;

public static class BudCompositionExtensions
{
    public static IServiceCollection AddBudPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddBudApi()
            .AddBudSettings(configuration)
            .AddBudAuthentication(configuration)
            .AddBudAuthorization()
            .AddBudDataAccess(configuration)
            .AddBudApplication();
    }
}
