using System.Reflection;
using Bud.Server.Infrastructure.Serialization;
using Bud.Server.Middleware;
using Bud.Server.Settings;
using Bud.Server.Validators;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;

namespace Bud.Server.DependencyInjection;

public static class BudApiCompositionExtensions
{
    public static IServiceCollection AddBudApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(new LenientEnumJsonConverterFactory());
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

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static IServiceCollection AddBudSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GlobalAdminSettings>(configuration.GetSection("GlobalAdminSettings"));
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<RateLimitSettings>(configuration.GetSection("RateLimitSettings"));
        return services;
    }
}
