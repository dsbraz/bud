using Bud.Server.Application.Auth;
using Bud.Server.Application.Collaborators;
using Bud.Server.Application.Dashboard;
using Bud.Server.Application.MetricCheckins;
using Bud.Server.Application.MissionMetrics;
using Bud.Server.Application.MissionObjectives;
using Bud.Server.Application.Missions;
using Bud.Server.Application.MissionTemplates;
using Bud.Server.Application.ObjectiveDimensions;
using Bud.Server.Application.Notifications;
using Bud.Server.Application.Organizations;
using Bud.Server.Application.Teams;
using Bud.Server.Application.Workspaces;
using Bud.Server.Authorization;
using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Infrastructure.Services;

namespace Bud.Server.DependencyInjection;

public static class BudApplicationCompositionExtensions
{
    public static IServiceCollection AddBudApplication(this IServiceCollection services)
    {
        services.AddScoped<IApplicationAuthorizationGateway, ApplicationAuthorizationGateway>();

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<ICollaboratorRepository, CollaboratorRepository>();
        services.AddScoped<OrganizationCommand>();
        services.AddScoped<OrganizationQuery>();

        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<WorkspaceCommand>();
        services.AddScoped<WorkspaceQuery>();

        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<TeamCommand>();
        services.AddScoped<TeamQuery>();

        services.AddScoped<CollaboratorCommand>();
        services.AddScoped<CollaboratorQuery>();

        services.AddScoped<IMissionRepository, MissionRepository>();
        services.AddScoped<MissionCommand>();
        services.AddScoped<MissionQuery>();

        services.AddScoped<IMissionObjectiveRepository, MissionObjectiveRepository>();
        services.AddScoped<MissionObjectiveCommand>();
        services.AddScoped<MissionObjectiveQuery>();

        services.AddScoped<IMissionMetricRepository, MissionMetricRepository>();
        services.AddScoped<MissionMetricCommand>();
        services.AddScoped<MissionMetricQuery>();

        services.AddScoped<IMetricCheckinRepository, MetricCheckinRepository>();
        services.AddScoped<MetricCheckinCommand>();
        services.AddScoped<MetricCheckinQuery>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<AuthCommand>();
        services.AddScoped<AuthQuery>();

        services.AddScoped<IMissionTemplateRepository, MissionTemplateRepository>();
        services.AddScoped<MissionTemplateCommand>();
        services.AddScoped<MissionTemplateQuery>();

        services.AddScoped<IObjectiveDimensionRepository, ObjectiveDimensionRepository>();
        services.AddScoped<ObjectiveDimensionCommand>();
        services.AddScoped<ObjectiveDimensionQuery>();

        services.AddScoped<IDashboardReadRepository, DashboardReadRepository>();
        services.AddScoped<DashboardQuery>();

        services.AddScoped<IMissionProgressService, MissionProgressService>();
        services.AddScoped<IMissionScopeResolver, MissionScopeResolver>();

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationRecipientResolver, NotificationRecipientResolver>();
        services.AddScoped<NotificationOrchestrator>();
        services.AddScoped<NotificationQuery>();
        services.AddScoped<NotificationCommand>();

        return services;
    }
}
