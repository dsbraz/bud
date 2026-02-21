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
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Events;
using Bud.Server.Domain.Repositories;
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
        services.AddScoped<RegisterOrganization>();
        services.AddScoped<RenameOrganization>();
        services.AddScoped<DeleteOrganization>();
        services.AddScoped<ViewOrganizationDetails>();
        services.AddScoped<ListOrganizations>();
        services.AddScoped<ListOrganizationWorkspaces>();
        services.AddScoped<ListOrganizationCollaborators>();

        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<CreateWorkspace>();
        services.AddScoped<RenameWorkspace>();
        services.AddScoped<DeleteWorkspace>();
        services.AddScoped<ViewWorkspaceDetails>();
        services.AddScoped<ListWorkspaces>();
        services.AddScoped<ListWorkspaceTeams>();

        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<CreateTeam>();
        services.AddScoped<UpdateTeam>();
        services.AddScoped<DeleteTeam>();
        services.AddScoped<ViewTeamDetails>();
        services.AddScoped<ListTeams>();
        services.AddScoped<ListSubTeams>();
        services.AddScoped<ListTeamCollaborators>();
        services.AddScoped<ListTeamCollaboratorSummaries>();
        services.AddScoped<UpdateTeamCollaborators>();
        services.AddScoped<ListAvailableTeamCollaborators>();

        services.AddScoped<CreateCollaborator>();
        services.AddScoped<UpdateCollaboratorProfile>();
        services.AddScoped<DeleteCollaborator>();
        services.AddScoped<ViewCollaboratorProfile>();
        services.AddScoped<ListLeaders>();
        services.AddScoped<ListCollaborators>();
        services.AddScoped<GetCollaboratorHierarchy>();
        services.AddScoped<ListCollaboratorTeams>();
        services.AddScoped<UpdateCollaboratorTeams>();
        services.AddScoped<ListAvailableCollaboratorTeams>();
        services.AddScoped<ListCollaboratorSummaries>();

        services.AddScoped<IMissionRepository, MissionRepository>();
        services.AddScoped<PlanMission>();
        services.AddScoped<ReplanMission>();
        services.AddScoped<DeleteMission>();
        services.AddScoped<ViewMissionDetails>();
        services.AddScoped<ListMissionsByScope>();
        services.AddScoped<ListCollaboratorMissions>();
        services.AddScoped<ListMissionProgress>();
        services.AddScoped<ListMissionMetrics>();

        services.AddScoped<IMissionObjectiveRepository, MissionObjectiveRepository>();
        services.AddScoped<DefineMissionObjective>();
        services.AddScoped<ReviseMissionObjective>();
        services.AddScoped<RemoveMissionObjective>();
        services.AddScoped<ViewMissionObjectiveDetails>();
        services.AddScoped<ListMissionObjectives>();
        services.AddScoped<CalculateMissionObjectiveProgress>();

        services.AddScoped<IMissionMetricRepository, MissionMetricRepository>();
        services.AddScoped<DefineMissionMetric>();
        services.AddScoped<ReviseMissionMetricDefinition>();
        services.AddScoped<RemoveMissionMetric>();
        services.AddScoped<ViewMissionMetricDetails>();
        services.AddScoped<BrowseMissionMetrics>();
        services.AddScoped<CalculateMissionMetricProgress>();

        services.AddScoped<IMetricCheckinRepository, MetricCheckinRepository>();
        services.AddScoped<RegisterMetricCheckin>();
        services.AddScoped<CorrectMetricCheckin>();
        services.AddScoped<DeleteMetricCheckin>();
        services.AddScoped<ViewMetricCheckinDetails>();
        services.AddScoped<ListMetricCheckinHistory>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<AuthenticateCollaborator>();
        services.AddScoped<ListAvailableOrganizations>();

        services.AddScoped<IMissionTemplateRepository, MissionTemplateRepository>();
        services.AddScoped<CreateStrategicMissionTemplate>();
        services.AddScoped<ReviseStrategicMissionTemplate>();
        services.AddScoped<RemoveStrategicMissionTemplate>();
        services.AddScoped<ViewStrategicMissionTemplate>();
        services.AddScoped<ListMissionTemplates>();

        services.AddScoped<IObjectiveDimensionRepository, ObjectiveDimensionRepository>();
        services.AddScoped<RegisterStrategicDimension>();
        services.AddScoped<RenameStrategicDimension>();
        services.AddScoped<RemoveStrategicDimension>();
        services.AddScoped<ViewStrategicDimensionDetails>();
        services.AddScoped<ListStrategicDimensions>();

        services.AddScoped<IDashboardReadRepository, DashboardReadRepository>();
        services.AddScoped<GetCollaboratorDashboard>();

        services.AddScoped<IMissionProgressService, MissionProgressService>();
        services.AddScoped<IMissionScopeResolver, MissionScopeResolver>();

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationRecipientResolver, NotificationRecipientResolver>();
        services.AddScoped<NotificationOrchestrator>();
        services.AddScoped<IDomainEventConsumer<MissionCreatedDomainEvent>, MissionCreatedDomainEventConsumer>();
        services.AddScoped<IDomainEventConsumer<MissionUpdatedDomainEvent>, MissionUpdatedDomainEventConsumer>();
        services.AddScoped<IDomainEventConsumer<MissionDeletedDomainEvent>, MissionDeletedDomainEventConsumer>();
        services.AddScoped<IDomainEventConsumer<MetricCheckinCreatedDomainEvent>, MetricCheckinCreatedDomainEventConsumer>();
        services.AddScoped<ListNotifications>();
        services.AddScoped<GetUnreadNotificationCount>();
        services.AddScoped<MarkNotificationAsRead>();
        services.AddScoped<MarkAllNotificationsAsRead>();

        return services;
    }
}
