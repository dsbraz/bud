using Bud.Server.Application.UseCases.Collaborators;
using Bud.Server.Application.UseCases.Me;
using Bud.Server.Application.UseCases.Goals;
using Bud.Server.Application.UseCases.Tasks;
using Bud.Server.Application.UseCases.Indicators;
using Bud.Server.Application.UseCases.Checkins;
using Bud.Server.Application.UseCases.Templates;
using Bud.Server.Application.UseCases.Notifications;
using Bud.Server.Application.EventHandlers;
using Bud.Server.Application.UseCases.Organizations;
using Bud.Server.Application.UseCases.Sessions;
using Bud.Server.Application.UseCases.Teams;
using Bud.Server.Application.UseCases.Workspaces;
using Bud.Server.Application.Ports;
using Bud.Server.Authorization;
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
        services.AddScoped<GetTeamCollaboratorLookup>();
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
        services.AddScoped<GetCollaboratorLookup>();

        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<CreateGoal>();
        services.AddScoped<PatchGoal>();
        services.AddScoped<DeleteGoal>();
        services.AddScoped<GetGoalById>();
        services.AddScoped<ListGoals>();
        services.AddScoped<ListGoalChildren>();
        services.AddScoped<ListGoalIndicators>();
        services.AddScoped<ListGoalProgress>();

        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<CreateTask>();
        services.AddScoped<PatchTask>();
        services.AddScoped<DeleteTask>();
        services.AddScoped<ListTasks>();

        services.AddScoped<IIndicatorRepository, IndicatorRepository>();
        services.AddScoped<CreateIndicator>();
        services.AddScoped<PatchIndicator>();
        services.AddScoped<DeleteIndicator>();
        services.AddScoped<GetIndicatorById>();
        services.AddScoped<ListIndicators>();
        services.AddScoped<GetIndicatorProgress>();

        services.AddScoped<CreateCheckin>();
        services.AddScoped<PatchCheckin>();
        services.AddScoped<DeleteCheckin>();
        services.AddScoped<GetCheckinById>();
        services.AddScoped<ListCheckins>();

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

        services.AddScoped<IGoalProgressService, GoalProgressService>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationRecipientResolver, NotificationRecipientResolver>();
        services.AddScoped<NotificationOrchestrator>();
        services.AddScoped<IDomainEventNotifier<GoalCreatedDomainEvent>, GoalCreatedDomainEventNotifier>();
        services.AddScoped<IDomainEventNotifier<GoalUpdatedDomainEvent>, GoalUpdatedDomainEventNotifier>();
        services.AddScoped<IDomainEventNotifier<GoalDeletedDomainEvent>, GoalDeletedDomainEventNotifier>();
        services.AddScoped<IDomainEventNotifier<CheckinCreatedDomainEvent>, CheckinCreatedDomainEventNotifier>();
        services.AddScoped<ListNotifications>();
        services.AddScoped<PatchNotification>();
        services.AddScoped<PatchNotifications>();

        return services;
    }
}
