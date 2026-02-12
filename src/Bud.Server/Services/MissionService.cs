using Bud.Server.Data;
using Bud.Server.Domain.Common.Specifications;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class MissionService(
    ApplicationDbContext dbContext,
    IMissionScopeResolver missionScopeResolver) : IMissionService
{
    public async Task<ServiceResult<Mission>> CreateAsync(CreateMissionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var missionScope = MissionScope.Create(request.ScopeType, request.ScopeId);

            var scopeResolution = await missionScopeResolver.ResolveScopeOrganizationIdAsync(
                request.ScopeType,
                request.ScopeId,
                cancellationToken: cancellationToken);
            if (!scopeResolution.IsSuccess)
            {
                return ServiceResult<Mission>.NotFound(scopeResolution.Error ?? "Escopo não encontrado.");
            }

            var mission = Mission.Create(
                Guid.NewGuid(),
                scopeResolution.Value,
                request.Name,
                request.Description,
                NormalizeToUtc(request.StartDate),
                NormalizeToUtc(request.EndDate),
                request.Status);

            mission.SetScope(missionScope);

            dbContext.Missions.Add(mission);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult<Mission>.Success(mission);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<Mission>.Failure(ex.Message, ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult<Mission>> UpdateAsync(Guid id, UpdateMissionRequest request, CancellationToken cancellationToken = default)
    {
        var mission = await dbContext.Missions.FindAsync([id], cancellationToken);

        if (mission is null)
        {
            return ServiceResult<Mission>.NotFound("Missão não encontrada.");
        }

        try
        {
            mission.UpdateDetails(
                request.Name,
                request.Description,
                NormalizeToUtc(request.StartDate),
                NormalizeToUtc(request.EndDate),
                request.Status);

            var shouldUpdateScope = request.ScopeId != Guid.Empty;
            if (shouldUpdateScope)
            {
                var missionScope = MissionScope.Create(request.ScopeType, request.ScopeId);

                var scopeResolution = await missionScopeResolver.ResolveScopeOrganizationIdAsync(
                    request.ScopeType,
                    request.ScopeId,
                    cancellationToken: cancellationToken);
                if (!scopeResolution.IsSuccess)
                {
                    return ServiceResult<Mission>.NotFound(scopeResolution.Error ?? "Escopo não encontrado.");
                }

                mission.OrganizationId = scopeResolution.Value;
                mission.SetScope(missionScope);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult<Mission>.Success(mission);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<Mission>.Failure(ex.Message, ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var mission = await dbContext.Missions.FindAsync([id], cancellationToken);

        if (mission is null)
        {
            return ServiceResult.NotFound("Missão não encontrada.");
        }

        dbContext.Missions.Remove(mission);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<Mission>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var mission = await dbContext.Missions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        return mission is null
            ? ServiceResult<Mission>.NotFound("Missão não encontrada.")
            : ServiceResult<Mission>.Success(mission);
    }

    public async Task<ServiceResult<PagedResult<Mission>>> GetAllAsync(
        MissionScopeType? scopeType,
        Guid? scopeId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.Missions.AsNoTracking();

        query = new MissionScopeSpecification(scopeType, scopeId).Apply(query);
        query = new MissionSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<Mission>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<Mission>>.Success(result);
    }

    public async Task<ServiceResult<PagedResult<Mission>>> GetMyMissionsAsync(
        Guid collaboratorId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var collaborator = await FindCollaboratorForMyMissionsAsync(collaboratorId, cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult<PagedResult<Mission>>.NotFound("Colaborador não encontrado.");
        }

        var teamIds = await GetCollaboratorTeamIdsAsync(collaboratorId, collaborator.TeamId, cancellationToken);
        var workspaceIds = await GetWorkspaceIdsForTeamsAsync(teamIds, cancellationToken);
        var query = BuildMyMissionsQuery(collaboratorId, collaborator.OrganizationId, teamIds, workspaceIds);
        query = new MissionSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(m => m.StartDate)
            .ThenBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<Mission>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<Mission>>.Success(result);
    }

    private async Task<Collaborator?> FindCollaboratorForMyMissionsAsync(Guid collaboratorId, CancellationToken cancellationToken)
    {
        return await dbContext.Collaborators
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(c => c.Team!)
                .ThenInclude(t => t.Workspace!)
                    .ThenInclude(w => w.Organization)
            .FirstOrDefaultAsync(c => c.Id == collaboratorId, cancellationToken);
    }

    private async Task<List<Guid>> GetCollaboratorTeamIdsAsync(Guid collaboratorId, Guid? primaryTeamId, CancellationToken cancellationToken)
    {
        var additionalTeamIds = await dbContext.CollaboratorTeams
            .AsNoTracking()
            .Where(ct => ct.CollaboratorId == collaboratorId)
            .Select(ct => ct.TeamId)
            .ToListAsync(cancellationToken);

        var allTeamIds = new HashSet<Guid>(additionalTeamIds);
        if (primaryTeamId.HasValue)
        {
            allTeamIds.Add(primaryTeamId.Value);
        }

        return allTeamIds.ToList();
    }

    private async Task<List<Guid>> GetWorkspaceIdsForTeamsAsync(List<Guid> teamIds, CancellationToken cancellationToken)
    {
        if (teamIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Teams
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => teamIds.Contains(t.Id))
            .Select(t => t.WorkspaceId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Mission> BuildMyMissionsQuery(
        Guid collaboratorId,
        Guid organizationId,
        List<Guid> teamIds,
        List<Guid> workspaceIds)
    {
        return dbContext.Missions
            .AsNoTracking()
            .Where(m =>
                m.CollaboratorId == collaboratorId ||
                (m.TeamId.HasValue && teamIds.Contains(m.TeamId.Value)) ||
                (m.WorkspaceId.HasValue && workspaceIds.Contains(m.WorkspaceId.Value)) ||
                (m.OrganizationId == organizationId && m.WorkspaceId == null && m.TeamId == null && m.CollaboratorId == null));
    }

    public async Task<ServiceResult<PagedResult<MissionMetric>>> GetMetricsAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var missionExists = await dbContext.Missions.AnyAsync(m => m.Id == id, cancellationToken);
        if (!missionExists)
        {
            return ServiceResult<PagedResult<MissionMetric>>.NotFound("Missão não encontrada.");
        }

        var query = dbContext.MissionMetrics
            .AsNoTracking()
            .Where(metric => metric.MissionId == id);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(metric => metric.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<MissionMetric>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<MissionMetric>>.Success(result);
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };
    }

}
