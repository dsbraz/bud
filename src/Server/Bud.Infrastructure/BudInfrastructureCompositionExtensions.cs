using Bud.Application.Abstractions;
using Bud.Application.Goals;
using Bud.Application.Indicators;
using Bud.Application.Me;
using Bud.Application.Notifications;
using Bud.Application.Organizations;
using Bud.Infrastructure.Persistence;
using Bud.Infrastructure.Authorization;
using Bud.Infrastructure.DomainEvents;
using Bud.Application.Ports;
using Bud.Infrastructure.Me;
using Bud.Infrastructure.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Bud.Infrastructure;

public static class BudInfrastructureCompositionExtensions
{
    public static IServiceCollection AddBudInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not configured.");
        }

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<ICollaboratorRepository, CollaboratorRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IIndicatorRepository, IndicatorRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ISessionAuthenticator, SessionAuthenticator>();
        services.AddScoped<IMyOrganizationsReadStore, MyOrganizationsReadStore>();
        services.AddScoped<IMyDashboardReadStore, DashboardReadStore>();
        services.AddScoped<IGoalProgressReadStore, GoalProgressService>();
        services.AddScoped<IIndicatorProgressReadStore, GoalProgressService>();
        services.AddScoped<INotificationRecipientResolver, NotificationRecipientResolver>();
        services.AddScoped<ITenantAuthorizationService, TenantAuthorizationService>();
        services.AddScoped<IOrganizationAuthorizationService, OrganizationAuthorizationService>();
        services.AddScoped<TenantSaveChangesInterceptor>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddScoped<ApplicationDbContext>(sp =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            optionsBuilder.AddInterceptors(sp.GetRequiredService<TenantSaveChangesInterceptor>());

            var tenantProvider = sp.GetRequiredService<ITenantProvider>();
            return new ApplicationDbContext(optionsBuilder.Options, tenantProvider);
        });
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgres", tags: ["db", "ready"]);

        return services;
    }
}
