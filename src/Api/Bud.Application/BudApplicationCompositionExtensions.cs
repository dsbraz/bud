using Bud.Application.EventHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace Bud.Application;

public static class BudApplicationCompositionExtensions
{
    public static IServiceCollection AddBudApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateOrganization>();
        services.AddScoped<PatchOrganization>();
        services.AddScoped<DeleteOrganization>();
        services.AddScoped<GetOrganizationById>();
        services.AddScoped<ListOrganizations>();
        services.AddScoped<ListOrganizationWorkspaces>();
        services.AddScoped<ListOrganizationCollaborators>();

        services.AddScoped<CreateWorkspace>();
        services.AddScoped<PatchWorkspace>();
        services.AddScoped<DeleteWorkspace>();
        services.AddScoped<GetWorkspaceById>();
        services.AddScoped<ListWorkspaces>();
        services.AddScoped<ListWorkspaceTeams>();

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

        services.AddScoped<CreateGoal>();
        services.AddScoped<PatchGoal>();
        services.AddScoped<DeleteGoal>();
        services.AddScoped<GetGoalById>();
        services.AddScoped<ListGoals>();
        services.AddScoped<ListGoalChildren>();
        services.AddScoped<ListGoalIndicators>();
        services.AddScoped<ListGoalProgress>();

        services.AddScoped<CreateTask>();
        services.AddScoped<PatchTask>();
        services.AddScoped<DeleteTask>();
        services.AddScoped<ListTasks>();

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

        services.AddScoped<CreateSession>();
        services.AddScoped<ListMyOrganizations>();
        services.AddScoped<DeleteCurrentSession>();

        services.AddScoped<CreateTemplate>();
        services.AddScoped<PatchTemplate>();
        services.AddScoped<DeleteTemplate>();
        services.AddScoped<GetTemplateById>();
        services.AddScoped<ListTemplates>();

        services.AddScoped<GetMyDashboard>();

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
