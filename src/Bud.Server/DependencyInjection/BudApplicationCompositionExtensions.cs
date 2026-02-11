using Bud.Server.Application.Auth;
using Bud.Server.Application.Dashboard;
using Bud.Server.Application.Collaborators;
using Bud.Server.Application.Collaborators.Events;
using Bud.Server.Application.Common.Authorization;
using Bud.Server.Application.Common.Events;
using Bud.Server.Application.Common.Pipeline;
using Bud.Server.Application.Common.ReadModel;
using Bud.Server.Application.MetricCheckins;
using Bud.Server.Application.MetricCheckins.Events;
using Bud.Server.Application.MissionMetrics;
using Bud.Server.Application.MissionMetrics.Events;
using Bud.Server.Application.Missions;
using Bud.Server.Application.Missions.Events;
using Bud.Server.Application.MissionTemplates;
using Bud.Server.Application.MissionTemplates.Events;
using Bud.Server.Application.Notifications;
using Bud.Server.Application.Notifications.Events;
using Bud.Server.Application.Organizations;
using Bud.Server.Application.Organizations.Events;
using Bud.Server.Application.Outbox;
using Bud.Server.Application.Teams;
using Bud.Server.Application.Teams.Events;
using Bud.Server.Application.Workspaces;
using Bud.Server.Application.Workspaces.Events;
using Bud.Server.Authorization;
using Bud.Server.Data;
using Bud.Server.Application.Abstractions;
using Bud.Server.Domain.Collaborators.Events;
using Bud.Server.Domain.MetricCheckins.Events;
using Bud.Server.Domain.MissionMetrics.Events;
using Bud.Server.Domain.Missions.Events;
using Bud.Server.Domain.MissionTemplates.Events;
using Bud.Server.Domain.Organizations.Events;
using Bud.Server.Domain.Teams.Events;
using Bud.Server.Domain.Workspaces.Events;
using Bud.Server.Infrastructure.Events;
using Bud.Server.Services;

namespace Bud.Server.DependencyInjection;

public static class BudApplicationCompositionExtensions
{
    public static IServiceCollection AddBudApplication(this IServiceCollection services)
    {
        services.AddScoped<IUseCasePipeline, UseCasePipeline>();
        services.AddScoped<IUseCaseBehavior, LoggingUseCaseBehavior>();
        services.AddScoped<IOutboxEventSerializer, JsonOutboxEventSerializer>();
        services.AddScoped<IDomainEventDispatcher, OutboxDomainEventDispatcher>();
        services.AddScoped<OutboxEventProcessor>();
        services.AddHostedService<OutboxProcessorBackgroundService>();
        services.AddScoped<IApplicationAuthorizationGateway, ApplicationAuthorizationGateway>();
        services.AddScoped<IApplicationEntityLookup, ApplicationEntityLookup>();

        services.AddScoped<IDomainEventSubscriber<OrganizationCreatedDomainEvent>, OrganizationCreatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<OrganizationUpdatedDomainEvent>, OrganizationUpdatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<OrganizationDeletedDomainEvent>, OrganizationDeletedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<WorkspaceCreatedDomainEvent>, WorkspaceCreatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<WorkspaceUpdatedDomainEvent>, WorkspaceUpdatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<WorkspaceDeletedDomainEvent>, WorkspaceDeletedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<TeamCreatedDomainEvent>, TeamCreatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<TeamUpdatedDomainEvent>, TeamUpdatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<TeamDeletedDomainEvent>, TeamDeletedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<CollaboratorCreatedDomainEvent>, CollaboratorCreatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<CollaboratorUpdatedDomainEvent>, CollaboratorUpdatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<CollaboratorDeletedDomainEvent>, CollaboratorDeletedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<MissionCreatedDomainEvent>, MissionCreatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<MissionUpdatedDomainEvent>, MissionUpdatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<MissionDeletedDomainEvent>, MissionDeletedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<MissionMetricCreatedDomainEvent>, MissionMetricCreatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<MissionMetricUpdatedDomainEvent>, MissionMetricUpdatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<MissionMetricDeletedDomainEvent>, MissionMetricDeletedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<MetricCheckinCreatedDomainEvent>, MetricCheckinCreatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<MetricCheckinUpdatedDomainEvent>, MetricCheckinUpdatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<MetricCheckinDeletedDomainEvent>, MetricCheckinDeletedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<MissionTemplateCreatedDomainEvent>, MissionTemplateCreatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<MissionTemplateUpdatedDomainEvent>, MissionTemplateUpdatedLogHandler>();
        services.AddScoped<IDomainEventSubscriber<MissionTemplateDeletedDomainEvent>, MissionTemplateDeletedLogHandler>();

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
        services.AddScoped<ICollaboratorCommandUseCase, CollaboratorCommandUseCase>();
        services.AddScoped<ICollaboratorQueryUseCase, CollaboratorQueryUseCase>();

        services.AddScoped<IMissionService, MissionService>();
        services.AddScoped<IMissionCommandUseCase, MissionCommandUseCase>();
        services.AddScoped<IMissionQueryUseCase, MissionQueryUseCase>();

        services.AddScoped<IMissionMetricService, MissionMetricService>();
        services.AddScoped<IMissionMetricCommandUseCase, MissionMetricCommandUseCase>();
        services.AddScoped<IMissionMetricQueryUseCase, MissionMetricQueryUseCase>();

        services.AddScoped<IMetricCheckinService, MetricCheckinService>();
        services.AddScoped<IMetricCheckinCommandUseCase, MetricCheckinCommandUseCase>();
        services.AddScoped<IMetricCheckinQueryUseCase, MetricCheckinQueryUseCase>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthCommandUseCase, AuthCommandUseCase>();
        services.AddScoped<IAuthQueryUseCase, AuthQueryUseCase>();

        services.AddScoped<IOutboxAdministrationService, OutboxAdministrationService>();
        services.AddScoped<IOutboxCommandUseCase, OutboxCommandUseCase>();
        services.AddScoped<IOutboxQueryUseCase, OutboxQueryUseCase>();

        services.AddScoped<IMissionTemplateService, MissionTemplateService>();
        services.AddScoped<IMissionTemplateCommandUseCase, MissionTemplateCommandUseCase>();
        services.AddScoped<IMissionTemplateQueryUseCase, MissionTemplateQueryUseCase>();

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDashboardQueryUseCase, DashboardQueryUseCase>();

        services.AddScoped<ITenantAuthorizationService, TenantAuthorizationService>();
        services.AddScoped<IOrganizationAuthorizationService, OrganizationAuthorizationService>();
        services.AddScoped<IMissionProgressService, MissionProgressService>();
        services.AddScoped<IMissionScopeResolver, MissionScopeResolver>();

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationRecipientResolver, NotificationRecipientResolver>();
        services.AddScoped<INotificationQueryUseCase, NotificationQueryUseCase>();
        services.AddScoped<INotificationCommandUseCase, NotificationCommandUseCase>();

        services.AddScoped<IDomainEventSubscriber<MissionCreatedDomainEvent>, MissionCreatedNotificationHandler>();
        services.AddScoped<IDomainEventSubscriber<MissionUpdatedDomainEvent>, MissionUpdatedNotificationHandler>();
        services.AddScoped<IDomainEventSubscriber<MissionDeletedDomainEvent>, MissionDeletedNotificationHandler>();
        services.AddScoped<IDomainEventSubscriber<MetricCheckinCreatedDomainEvent>, MetricCheckinCreatedNotificationHandler>();

        return services;
    }
}
