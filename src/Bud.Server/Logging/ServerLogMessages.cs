using Microsoft.Extensions.Logging;

namespace Bud.Server.Logging;

public static class ServerLogMessages
{
    private static readonly Action<ILogger, string, string, Exception?> UseCaseExecuting =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(1000, nameof(LogUseCaseExecuting)), "Executando use case {UseCase}.{Operation}");

    private static readonly Action<ILogger, string, string, Exception?> UseCaseCompleted =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(1001, nameof(LogUseCaseCompleted)), "Use case {UseCase}.{Operation} concluído");

    private static readonly Action<ILogger, string, string, Exception?> UseCaseFailed =
        LoggerMessage.Define<string, string>(LogLevel.Error, new EventId(1002, nameof(LogUseCaseFailed)), "Erro no use case {UseCase}.{Operation}");

    private static readonly Action<ILogger, int, Exception?> MigrationAttemptFailed =
        LoggerMessage.Define<int>(LogLevel.Warning, new EventId(1003, nameof(LogMigrationAttemptFailed)), "Migration attempt {Attempt} failed.");

    private static readonly Action<ILogger, int, Exception?> MigrationFailedAfterAttempts =
        LoggerMessage.Define<int>(LogLevel.Error, new EventId(1004, nameof(LogMigrationFailedAfterAttempts)), "Database migration failed after {Attempts} attempts.");

    private static readonly Action<ILogger, Exception?> DatabaseSeedCompleted =
        LoggerMessage.Define(LogLevel.Information, new EventId(1005, nameof(LogDatabaseSeedCompleted)), "Database seed completed.");

    private static readonly Action<ILogger, string, Exception?> UnhandledException =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(1006, nameof(LogUnhandledException)), "Ocorreu uma exceção não tratada: {Message}");

    private static readonly Action<ILogger, Exception?> OutboxProcessingFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(1007, nameof(LogOutboxProcessingFailed)), "Falha ao processar mensagens de outbox.");

    private static readonly Action<ILogger, Guid, Guid, Exception?> CollaboratorCreatedProcessed =
        LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(2000, nameof(LogCollaboratorCreatedProcessed)), "Evento CollaboratorCreated processado. CollaboratorId={CollaboratorId} OrganizationId={OrganizationId}");

    private static readonly Action<ILogger, Guid, Guid, Exception?> CollaboratorUpdatedProcessed =
        LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(2001, nameof(LogCollaboratorUpdatedProcessed)), "Evento CollaboratorUpdated processado. CollaboratorId={CollaboratorId} OrganizationId={OrganizationId}");

    private static readonly Action<ILogger, Guid, Guid, Exception?> CollaboratorDeletedProcessed =
        LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(2002, nameof(LogCollaboratorDeletedProcessed)), "Evento CollaboratorDeleted processado. CollaboratorId={CollaboratorId} OrganizationId={OrganizationId}");

    private static readonly Action<ILogger, Guid, Guid, Guid, Guid, Exception?> MetricCheckinCreatedProcessed =
        LoggerMessage.Define<Guid, Guid, Guid, Guid>(LogLevel.Information, new EventId(2003, nameof(LogMetricCheckinCreatedProcessed)), "Evento MetricCheckinCreated processado. CheckinId={CheckinId} MetricId={MetricId} OrganizationId={OrganizationId} CollaboratorId={CollaboratorId}");

    private static readonly Action<ILogger, Guid, Guid, Guid, Guid, Exception?> MetricCheckinUpdatedProcessed =
        LoggerMessage.Define<Guid, Guid, Guid, Guid>(LogLevel.Information, new EventId(2004, nameof(LogMetricCheckinUpdatedProcessed)), "Evento MetricCheckinUpdated processado. MetricCheckinId={MetricCheckinId} MissionMetricId={MissionMetricId} OrganizationId={OrganizationId} CollaboratorId={CollaboratorId}");

    private static readonly Action<ILogger, Guid, Guid, Guid, Guid, Exception?> MetricCheckinDeletedProcessed =
        LoggerMessage.Define<Guid, Guid, Guid, Guid>(LogLevel.Information, new EventId(2005, nameof(LogMetricCheckinDeletedProcessed)), "Evento MetricCheckinDeleted processado. MetricCheckinId={MetricCheckinId} MissionMetricId={MissionMetricId} OrganizationId={OrganizationId} CollaboratorId={CollaboratorId}");

    private static readonly Action<ILogger, Guid, Guid, Guid, Exception?> MissionMetricCreatedProcessed =
        LoggerMessage.Define<Guid, Guid, Guid>(LogLevel.Information, new EventId(2006, nameof(LogMissionMetricCreatedProcessed)), "Evento MissionMetricCreated processado. MetricId={MetricId} MissionId={MissionId} OrganizationId={OrganizationId}");

    private static readonly Action<ILogger, Guid, Guid, Guid, Exception?> MissionMetricUpdatedProcessed =
        LoggerMessage.Define<Guid, Guid, Guid>(LogLevel.Information, new EventId(2007, nameof(LogMissionMetricUpdatedProcessed)), "Evento MissionMetricUpdated processado. MissionMetricId={MissionMetricId} MissionId={MissionId} OrganizationId={OrganizationId}");

    private static readonly Action<ILogger, Guid, Guid, Guid, Exception?> MissionMetricDeletedProcessed =
        LoggerMessage.Define<Guid, Guid, Guid>(LogLevel.Information, new EventId(2008, nameof(LogMissionMetricDeletedProcessed)), "Evento MissionMetricDeleted processado. MissionMetricId={MissionMetricId} MissionId={MissionId} OrganizationId={OrganizationId}");

    private static readonly Action<ILogger, Guid, Guid, Exception?> MissionCreatedProcessed =
        LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(2009, nameof(LogMissionCreatedProcessed)), "Evento MissionCreated processado. MissionId={MissionId} OrganizationId={OrganizationId}");

    private static readonly Action<ILogger, Guid, Guid, Exception?> MissionUpdatedProcessed =
        LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(2010, nameof(LogMissionUpdatedProcessed)), "Evento MissionUpdated processado. MissionId={MissionId} OrganizationId={OrganizationId}");

    private static readonly Action<ILogger, Guid, Guid, Exception?> MissionDeletedProcessed =
        LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(2011, nameof(LogMissionDeletedProcessed)), "Evento MissionDeleted processado. MissionId={MissionId} OrganizationId={OrganizationId}");

    private static readonly Action<ILogger, Guid, Exception?> OrganizationCreatedProcessed =
        LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(2012, nameof(LogOrganizationCreatedProcessed)), "Evento OrganizationCreated processado. OrganizationId={OrganizationId}");

    private static readonly Action<ILogger, Guid, Exception?> OrganizationUpdatedProcessed =
        LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(2013, nameof(LogOrganizationUpdatedProcessed)), "Evento OrganizationUpdated processado. OrganizationId={OrganizationId}");

    private static readonly Action<ILogger, Guid, Exception?> OrganizationDeletedProcessed =
        LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(2014, nameof(LogOrganizationDeletedProcessed)), "Evento OrganizationDeleted processado. OrganizationId={OrganizationId}");

    private static readonly Action<ILogger, Guid, Guid, Guid, Exception?> TeamCreatedProcessed =
        LoggerMessage.Define<Guid, Guid, Guid>(LogLevel.Information, new EventId(2015, nameof(LogTeamCreatedProcessed)), "Evento TeamCreated processado. TeamId={TeamId} OrganizationId={OrganizationId} WorkspaceId={WorkspaceId}");

    private static readonly Action<ILogger, Guid, Guid, Guid, Exception?> TeamUpdatedProcessed =
        LoggerMessage.Define<Guid, Guid, Guid>(LogLevel.Information, new EventId(2016, nameof(LogTeamUpdatedProcessed)), "Evento TeamUpdated processado. TeamId={TeamId} OrganizationId={OrganizationId} WorkspaceId={WorkspaceId}");

    private static readonly Action<ILogger, Guid, Guid, Guid, Exception?> TeamDeletedProcessed =
        LoggerMessage.Define<Guid, Guid, Guid>(LogLevel.Information, new EventId(2017, nameof(LogTeamDeletedProcessed)), "Evento TeamDeleted processado. TeamId={TeamId} OrganizationId={OrganizationId} WorkspaceId={WorkspaceId}");

    private static readonly Action<ILogger, Guid, Guid, Exception?> WorkspaceCreatedProcessed =
        LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(2018, nameof(LogWorkspaceCreatedProcessed)), "Evento WorkspaceCreated processado. WorkspaceId={WorkspaceId} OrganizationId={OrganizationId}");

    private static readonly Action<ILogger, Guid, Guid, Exception?> WorkspaceUpdatedProcessed =
        LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(2019, nameof(LogWorkspaceUpdatedProcessed)), "Evento WorkspaceUpdated processado. WorkspaceId={WorkspaceId} OrganizationId={OrganizationId}");

    private static readonly Action<ILogger, Guid, Guid, Exception?> WorkspaceDeletedProcessed =
        LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(2020, nameof(LogWorkspaceDeletedProcessed)), "Evento WorkspaceDeleted processado. WorkspaceId={WorkspaceId} OrganizationId={OrganizationId}");

    public static void LogUseCaseExecuting(this ILogger logger, string useCaseName, string operationName)
        => UseCaseExecuting(logger, useCaseName, operationName, null);

    public static void LogUseCaseCompleted(this ILogger logger, string useCaseName, string operationName)
        => UseCaseCompleted(logger, useCaseName, operationName, null);

    public static void LogUseCaseFailed(this ILogger logger, Exception exception, string useCaseName, string operationName)
        => UseCaseFailed(logger, useCaseName, operationName, exception);

    public static void LogMigrationAttemptFailed(this ILogger logger, Exception exception, int attempt)
        => MigrationAttemptFailed(logger, attempt, exception);

    public static void LogMigrationFailedAfterAttempts(this ILogger logger, int attempts)
        => MigrationFailedAfterAttempts(logger, attempts, null);

    public static void LogDatabaseSeedCompleted(this ILogger logger)
        => DatabaseSeedCompleted(logger, null);

    public static void LogUnhandledException(this ILogger logger, Exception exception)
        => UnhandledException(logger, exception.Message, exception);

    public static void LogOutboxProcessingFailed(this ILogger logger, Exception exception)
        => OutboxProcessingFailed(logger, exception);

    public static void LogCollaboratorCreatedProcessed(this ILogger logger, Guid collaboratorId, Guid organizationId)
        => CollaboratorCreatedProcessed(logger, collaboratorId, organizationId, null);

    public static void LogCollaboratorUpdatedProcessed(this ILogger logger, Guid collaboratorId, Guid organizationId)
        => CollaboratorUpdatedProcessed(logger, collaboratorId, organizationId, null);

    public static void LogCollaboratorDeletedProcessed(this ILogger logger, Guid collaboratorId, Guid organizationId)
        => CollaboratorDeletedProcessed(logger, collaboratorId, organizationId, null);

    public static void LogMetricCheckinCreatedProcessed(this ILogger logger, Guid checkinId, Guid metricId, Guid organizationId, Guid collaboratorId)
        => MetricCheckinCreatedProcessed(logger, checkinId, metricId, organizationId, collaboratorId, null);

    public static void LogMetricCheckinUpdatedProcessed(this ILogger logger, Guid metricCheckinId, Guid missionMetricId, Guid organizationId, Guid collaboratorId)
        => MetricCheckinUpdatedProcessed(logger, metricCheckinId, missionMetricId, organizationId, collaboratorId, null);

    public static void LogMetricCheckinDeletedProcessed(this ILogger logger, Guid metricCheckinId, Guid missionMetricId, Guid organizationId, Guid collaboratorId)
        => MetricCheckinDeletedProcessed(logger, metricCheckinId, missionMetricId, organizationId, collaboratorId, null);

    public static void LogMissionMetricCreatedProcessed(this ILogger logger, Guid metricId, Guid missionId, Guid organizationId)
        => MissionMetricCreatedProcessed(logger, metricId, missionId, organizationId, null);

    public static void LogMissionMetricUpdatedProcessed(this ILogger logger, Guid missionMetricId, Guid missionId, Guid organizationId)
        => MissionMetricUpdatedProcessed(logger, missionMetricId, missionId, organizationId, null);

    public static void LogMissionMetricDeletedProcessed(this ILogger logger, Guid missionMetricId, Guid missionId, Guid organizationId)
        => MissionMetricDeletedProcessed(logger, missionMetricId, missionId, organizationId, null);

    public static void LogMissionCreatedProcessed(this ILogger logger, Guid missionId, Guid organizationId)
        => MissionCreatedProcessed(logger, missionId, organizationId, null);

    public static void LogMissionUpdatedProcessed(this ILogger logger, Guid missionId, Guid organizationId)
        => MissionUpdatedProcessed(logger, missionId, organizationId, null);

    public static void LogMissionDeletedProcessed(this ILogger logger, Guid missionId, Guid organizationId)
        => MissionDeletedProcessed(logger, missionId, organizationId, null);

    public static void LogOrganizationCreatedProcessed(this ILogger logger, Guid organizationId)
        => OrganizationCreatedProcessed(logger, organizationId, null);

    public static void LogOrganizationUpdatedProcessed(this ILogger logger, Guid organizationId)
        => OrganizationUpdatedProcessed(logger, organizationId, null);

    public static void LogOrganizationDeletedProcessed(this ILogger logger, Guid organizationId)
        => OrganizationDeletedProcessed(logger, organizationId, null);

    public static void LogTeamCreatedProcessed(this ILogger logger, Guid teamId, Guid organizationId, Guid workspaceId)
        => TeamCreatedProcessed(logger, teamId, organizationId, workspaceId, null);

    public static void LogTeamUpdatedProcessed(this ILogger logger, Guid teamId, Guid organizationId, Guid workspaceId)
        => TeamUpdatedProcessed(logger, teamId, organizationId, workspaceId, null);

    public static void LogTeamDeletedProcessed(this ILogger logger, Guid teamId, Guid organizationId, Guid workspaceId)
        => TeamDeletedProcessed(logger, teamId, organizationId, workspaceId, null);

    public static void LogWorkspaceCreatedProcessed(this ILogger logger, Guid workspaceId, Guid organizationId)
        => WorkspaceCreatedProcessed(logger, workspaceId, organizationId, null);

    public static void LogWorkspaceUpdatedProcessed(this ILogger logger, Guid workspaceId, Guid organizationId)
        => WorkspaceUpdatedProcessed(logger, workspaceId, organizationId, null);

    public static void LogWorkspaceDeletedProcessed(this ILogger logger, Guid workspaceId, Guid organizationId)
        => WorkspaceDeletedProcessed(logger, workspaceId, organizationId, null);
}
