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
using Bud.Server.Data;
using Bud.Server.Services;

namespace Bud.Server.DependencyInjection;

public static class BudApplicationCompositionExtensions
{
    public static IServiceCollection AddBudApplication(this IServiceCollection services)
    {
        services.AddScoped<IApplicationAuthorizationGateway, ApplicationAuthorizationGateway>();
        services.AddScoped<IApplicationEntityLookup, ApplicationEntityLookup>();

        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IOrganizationCommandUseCase, OrganizationCommandUseCase>();
        services.AddScoped<IOrganizationQueryUseCase, OrganizationQueryUseCase>();

        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IWorkspaceCommandUseCase, WorkspaceCommandUseCase>();
        services.AddScoped<IWorkspaceQueryUseCase, WorkspaceQueryUseCase>();

        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<ITeamCommandUseCase, TeamCommandUseCase>();
        services.AddScoped<ITeamQueryUseCase, TeamQueryUseCase>();

        services.AddScoped<ICollaboratorService, CollaboratorService>();
        services.AddScoped<ICollaboratorValidationService, CollaboratorValidationService>();
        services.AddScoped<ICollaboratorCommandUseCase, CollaboratorCommandUseCase>();
        services.AddScoped<ICollaboratorQueryUseCase, CollaboratorQueryUseCase>();

        services.AddScoped<IMissionService, MissionService>();
        services.AddScoped<IMissionCommandUseCase, MissionCommandUseCase>();
        services.AddScoped<IMissionQueryUseCase, MissionQueryUseCase>();

        services.AddScoped<IMissionObjectiveService, MissionObjectiveService>();
        services.AddScoped<IMissionObjectiveCommandUseCase, MissionObjectiveCommandUseCase>();
        services.AddScoped<IMissionObjectiveQueryUseCase, MissionObjectiveQueryUseCase>();

        services.AddScoped<IMissionMetricService, MissionMetricService>();
        services.AddScoped<IMissionMetricCommandUseCase, MissionMetricCommandUseCase>();
        services.AddScoped<IMissionMetricQueryUseCase, MissionMetricQueryUseCase>();

        services.AddScoped<IMetricCheckinService, MetricCheckinService>();
        services.AddScoped<IMetricCheckinCommandUseCase, MetricCheckinCommandUseCase>();
        services.AddScoped<IMetricCheckinQueryUseCase, MetricCheckinQueryUseCase>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthCommandUseCase, AuthCommandUseCase>();
        services.AddScoped<IAuthQueryUseCase, AuthQueryUseCase>();

        services.AddScoped<IMissionTemplateService, MissionTemplateService>();
        services.AddScoped<IMissionTemplateCommandUseCase, MissionTemplateCommandUseCase>();
        services.AddScoped<IMissionTemplateQueryUseCase, MissionTemplateQueryUseCase>();

        services.AddScoped<IObjectiveDimensionService, ObjectiveDimensionService>();
        services.AddScoped<IObjectiveDimensionCommandUseCase, ObjectiveDimensionCommandUseCase>();
        services.AddScoped<IObjectiveDimensionQueryUseCase, ObjectiveDimensionQueryUseCase>();

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDashboardQueryUseCase, DashboardQueryUseCase>();

        services.AddScoped<ITenantAuthorizationService, TenantAuthorizationService>();
        services.AddScoped<IOrganizationAuthorizationService, OrganizationAuthorizationService>();
        services.AddScoped<IMissionProgressService, MissionProgressService>();
        services.AddScoped<IMissionScopeResolver, MissionScopeResolver>();

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationRecipientResolver, NotificationRecipientResolver>();
        services.AddScoped<INotificationOrchestrator, NotificationOrchestrator>();
        services.AddScoped<INotificationQueryUseCase, NotificationQueryUseCase>();
        services.AddScoped<INotificationCommandUseCase, NotificationCommandUseCase>();

        return services;
    }
}
