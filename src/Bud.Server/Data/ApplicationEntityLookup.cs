using Bud.Server.Application.Common.ReadModel;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Data;

public sealed class ApplicationEntityLookup(ApplicationDbContext dbContext) : IApplicationEntityLookup
{
    public async Task<Workspace?> GetWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);
    }

    public async Task<Team?> GetTeamAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
    }

    public async Task<Collaborator?> GetCollaboratorAsync(Guid collaboratorId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Collaborators
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == collaboratorId, cancellationToken);
    }

    public async Task<Mission?> GetMissionAsync(Guid missionId, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Missions.AsNoTracking();
        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.FirstOrDefaultAsync(m => m.Id == missionId, cancellationToken);
    }

    public async Task<MissionMetric?> GetMissionMetricAsync(
        Guid missionMetricId,
        bool ignoreQueryFilters = false,
        bool includeMission = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.MissionMetrics.AsNoTracking();
        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (includeMission)
        {
            query = query.Include(m => m.Mission);
        }

        return await query.FirstOrDefaultAsync(m => m.Id == missionMetricId, cancellationToken);
    }

    public async Task<MetricCheckin?> GetMetricCheckinAsync(
        Guid metricCheckinId,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.MetricCheckins.AsNoTracking();
        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.FirstOrDefaultAsync(mc => mc.Id == metricCheckinId, cancellationToken);
    }
}
