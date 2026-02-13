using Bud.Shared.Domain;

namespace Bud.Server.Data;

public interface IApplicationEntityLookup
{
    Task<Workspace?> GetWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    Task<Team?> GetTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

    Task<Collaborator?> GetCollaboratorAsync(Guid collaboratorId, CancellationToken cancellationToken = default);

    Task<Mission?> GetMissionAsync(Guid missionId, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default);

    Task<MissionMetric?> GetMissionMetricAsync(
        Guid missionMetricId,
        bool ignoreQueryFilters = false,
        bool includeMission = false,
        CancellationToken cancellationToken = default);

    Task<MetricCheckin?> GetMetricCheckinAsync(
        Guid metricCheckinId,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default);
}
