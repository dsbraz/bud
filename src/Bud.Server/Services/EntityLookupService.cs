using Bud.Server.Data;
using Bud.Shared.Domain;

namespace Bud.Server.Services;

public sealed class EntityLookupService(IApplicationEntityLookup applicationEntityLookup) : IEntityLookupService
{
    public Task<Workspace?> GetWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => applicationEntityLookup.GetWorkspaceAsync(workspaceId, cancellationToken);

    public Task<Team?> GetTeamAsync(Guid teamId, CancellationToken cancellationToken = default)
        => applicationEntityLookup.GetTeamAsync(teamId, cancellationToken);

    public Task<Collaborator?> GetCollaboratorAsync(Guid collaboratorId, CancellationToken cancellationToken = default)
        => applicationEntityLookup.GetCollaboratorAsync(collaboratorId, cancellationToken);

    public Task<Mission?> GetMissionAsync(Guid missionId, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
        => applicationEntityLookup.GetMissionAsync(missionId, ignoreQueryFilters, cancellationToken);

    public Task<MissionMetric?> GetMissionMetricAsync(
        Guid missionMetricId,
        bool ignoreQueryFilters = false,
        bool includeMission = false,
        CancellationToken cancellationToken = default)
        => applicationEntityLookup.GetMissionMetricAsync(
            missionMetricId,
            ignoreQueryFilters,
            includeMission,
            cancellationToken);

    public Task<MissionObjective?> GetMissionObjectiveAsync(
        Guid objectiveId,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
        => applicationEntityLookup.GetMissionObjectiveAsync(
            objectiveId,
            ignoreQueryFilters,
            cancellationToken);

    public Task<MetricCheckin?> GetMetricCheckinAsync(
        Guid metricCheckinId,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
        => applicationEntityLookup.GetMetricCheckinAsync(
            metricCheckinId,
            ignoreQueryFilters,
            cancellationToken);
}
