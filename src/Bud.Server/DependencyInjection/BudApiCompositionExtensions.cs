using Bud.Server.Middleware;
using Bud.Server.Settings;
using Bud.Server.Validators;
using FluentValidation;
using System.Reflection;

namespace Bud.Server.DependencyInjection;

public static class BudApiCompositionExtensions
{
    public static IServiceCollection AddBudApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }
        });

        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddValidatorsFromAssemblyContaining<CreateOrganizationValidator>();
        return services;
    }

    public static IServiceCollection AddBudSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GlobalAdminSettings>(configuration.GetSection("GlobalAdminSettings"));
        return services;
    }
}
