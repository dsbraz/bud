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
        services.AddScoped<IOrganizationCommandUseCase, OrganizationCommandUseCase>();
        services.AddScoped<IOrganizationQueryUseCase, OrganizationQueryUseCase>();

        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IWorkspaceCommandUseCase, WorkspaceCommandUseCase>();
        services.AddScoped<IWorkspaceQueryUseCase, WorkspaceQueryUseCase>();

        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITeamCommandUseCase, TeamCommandUseCase>();
        services.AddScoped<ITeamQueryUseCase, TeamQueryUseCase>();

        services.AddScoped<ICollaboratorCommandUseCase, CollaboratorCommandUseCase>();
        services.AddScoped<ICollaboratorQueryUseCase, CollaboratorQueryUseCase>();

        services.AddScoped<IMissionRepository, MissionRepository>();
        services.AddScoped<IMissionCommandUseCase, MissionCommandUseCase>();
        services.AddScoped<IMissionQueryUseCase, MissionQueryUseCase>();

        services.AddScoped<IMissionObjectiveRepository, MissionObjectiveRepository>();
        services.AddScoped<IMissionObjectiveCommandUseCase, MissionObjectiveCommandUseCase>();
        services.AddScoped<IMissionObjectiveQueryUseCase, MissionObjectiveQueryUseCase>();

        services.AddScoped<IMissionMetricRepository, MissionMetricRepository>();
        services.AddScoped<IMissionMetricCommandUseCase, MissionMetricCommandUseCase>();
        services.AddScoped<IMissionMetricQueryUseCase, MissionMetricQueryUseCase>();

        services.AddScoped<IMetricCheckinRepository, MetricCheckinRepository>();
        services.AddScoped<IMetricCheckinCommandUseCase, MetricCheckinCommandUseCase>();
        services.AddScoped<IMetricCheckinQueryUseCase, MetricCheckinQueryUseCase>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthCommandUseCase, AuthCommandUseCase>();
        services.AddScoped<IAuthQueryUseCase, AuthQueryUseCase>();

        services.AddScoped<IMissionTemplateRepository, MissionTemplateRepository>();
        services.AddScoped<IMissionTemplateCommandUseCase, MissionTemplateCommandUseCase>();
        services.AddScoped<IMissionTemplateQueryUseCase, MissionTemplateQueryUseCase>();

        services.AddScoped<IObjectiveDimensionRepository, ObjectiveDimensionRepository>();
        services.AddScoped<IObjectiveDimensionCommandUseCase, ObjectiveDimensionCommandUseCase>();
        services.AddScoped<IObjectiveDimensionQueryUseCase, ObjectiveDimensionQueryUseCase>();

        services.AddScoped<IDashboardReadRepository, DashboardReadRepository>();
        services.AddScoped<IDashboardQueryUseCase, DashboardQueryUseCase>();

        services.AddScoped<IMissionProgressService, MissionProgressService>();
        services.AddScoped<IMissionScopeResolver, MissionScopeResolver>();

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationRecipientResolver, NotificationRecipientResolver>();
        services.AddScoped<INotificationOrchestrator, NotificationOrchestrator>();
        services.AddScoped<INotificationQueryUseCase, NotificationQueryUseCase>();
        services.AddScoped<INotificationCommandUseCase, NotificationCommandUseCase>();

        return services;
    }
}
