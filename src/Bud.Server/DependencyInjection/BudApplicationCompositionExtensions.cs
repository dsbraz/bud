using Bud.Server.Application.UseCases.Collaborators;
using Bud.Server.Application.UseCases.Me;
using Bud.Server.Application.UseCases.Metrics;
using Bud.Server.Application.UseCases.Objectives;
using Bud.Server.Application.UseCases.Missions;
using Bud.Server.Application.UseCases.Templates;
using Bud.Server.Application.UseCases.Notifications;
using Bud.Server.Application.EventHandlers;
using Bud.Server.Application.UseCases.Organizations;
using Bud.Server.Application.UseCases.Sessions;
using Bud.Server.Application.UseCases.Teams;
using Bud.Server.Application.UseCases.Workspaces;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Events;
using Bud.Server.Domain.Repositories;
using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Infrastructure.Services;
using Bud.Server.Application.Ports;

namespace Bud.Server.DependencyInjection;

public static class BudApplicationCompositionExtensions
{
    public static IServiceCollection AddBudApplication(this IServiceCollection services)
    {
        services.AddScoped<IApplicationAuthorizationGateway, ApplicationAuthorizationGateway>();

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<ICollaboratorRepository, CollaboratorRepository>();
        services.AddScoped<CreateOrganization>();
        services.AddScoped<PatchOrganization>();
        services.AddScoped<DeleteOrganization>();
        services.AddScoped<GetOrganizationById>();
        services.AddScoped<ListOrganizations>();
        services.AddScoped<ListOrganizationWorkspaces>();
        services.AddScoped<ListOrganizationCollaborators>();

        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<CreateWorkspace>();
        services.AddScoped<PatchWorkspace>();
        services.AddScoped<DeleteWorkspace>();
        services.AddScoped<GetWorkspaceById>();
        services.AddScoped<ListWorkspaces>();
        services.AddScoped<ListWorkspaceTeams>();

        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<CreateTeam>();
        services.AddScoped<PatchTeam>();
        services.AddScoped<DeleteTeam>();
        services.AddScoped<GetTeamById>();
        services.AddScoped<ListTeams>();
        services.AddScoped<ListSubTeams>();
        services.AddScoped<ListTeamCollaborators>();
        services.AddScoped<ListTeamCollaboratorOptions>();
        services.AddScoped<PatchTeamCollaborators>();
        services.AddScoped<ListAvailableCollaboratorsForTeam>();

        services.AddScoped<CreateCollaborator>();
        services.AddScoped<PatchCollaborator>();
        services.AddScoped<DeleteCollaborator>();
        services.AddScoped<GetCollaboratorById>();
        services.AddScoped<ListLeaderCollaborators>();
        services.AddScoped<ListCollaborators>();
        services.AddScoped<GetCollaboratorHierarchy>();
        services.AddScoped<ListCollaboratorTeams>();
        services.AddScoped<PatchCollaboratorTeams>();
        services.AddScoped<ListAvailableTeamsForCollaborator>();
        services.AddScoped<ListCollaboratorOptions>();

        services.AddScoped<IMissionRepository, MissionRepository>();
        services.AddScoped<CreateMission>();
        services.AddScoped<PatchMission>();
        services.AddScoped<DeleteMission>();
        services.AddScoped<GetMissionById>();
        services.AddScoped<ListMissionsByScope>();
        services.AddScoped<ListCollaboratorMissions>();
        services.AddScoped<ListMissionProgress>();
        services.AddScoped<ListMissionMetrics>();

        services.AddScoped<IObjectiveRepository, ObjectiveRepository>();
        services.AddScoped<CreateObjective>();
        services.AddScoped<PatchObjective>();
        services.AddScoped<DeleteObjective>();
        services.AddScoped<GetObjectiveById>();
        services.AddScoped<ListObjectives>();
        services.AddScoped<ListObjectivesByMission>();
        services.AddScoped<ListObjectiveMetrics>();
        services.AddScoped<ListObjectiveProgress>();

        services.AddScoped<IMetricRepository, MetricRepository>();
        services.AddScoped<CreateMetric>();
        services.AddScoped<PatchMetric>();
        services.AddScoped<DeleteMetric>();
        services.AddScoped<GetMetricById>();
        services.AddScoped<ListMetrics>();
        services.AddScoped<ListMetricProgress>();

        services.AddScoped<CreateMetricCheckin>();
        services.AddScoped<PatchMetricCheckin>();
        services.AddScoped<DeleteMetricCheckin>();
        services.AddScoped<GetMetricCheckinById>();
        services.AddScoped<ListMetricCheckins>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<CreateSession>();
        services.AddScoped<ListMyOrganizations>();
        services.AddScoped<DeleteCurrentSession>();

        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<CreateTemplate>();
        services.AddScoped<PatchTemplate>();
        services.AddScoped<DeleteTemplate>();
        services.AddScoped<GetTemplateById>();
        services.AddScoped<ListTemplates>();

        services.AddScoped<IMyDashboardReadStore, DashboardReadStore>();
        services.AddScoped<GetMyDashboard>();

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
        services.AddScoped<PatchNotification>();
        services.AddScoped<PatchNotifications>();

        return services;
    }
}
