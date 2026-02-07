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
    public MissionService(ApplicationDbContext dbContext)
        : this(dbContext, new MissionScopeResolver(dbContext))
    {
    }

    public async Task<ServiceResult<Mission>> CreateAsync(CreateMissionRequest request, CancellationToken cancellationToken = default)
    {
        var scopeResolution = await missionScopeResolver.ResolveScopeOrganizationIdAsync(
            request.ScopeType,
            request.ScopeId,
            cancellationToken: cancellationToken);
        if (!scopeResolution.IsSuccess)
        {
            return ServiceResult<Mission>.NotFound(scopeResolution.Error ?? "Escopo não encontrado.");
        }

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            StartDate = NormalizeToUtc(request.StartDate),
            EndDate = NormalizeToUtc(request.EndDate),
            Status = request.Status,
            OrganizationId = scopeResolution.Value,
        };

        ApplyScope(mission, request.ScopeType, request.ScopeId);

        dbContext.Missions.Add(mission);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<Mission>.Success(mission);
    }

    public async Task<ServiceResult<Mission>> UpdateAsync(Guid id, UpdateMissionRequest request, CancellationToken cancellationToken = default)
    {
        var mission = await dbContext.Missions.FindAsync([id], cancellationToken);

        if (mission is null)
        {
            return ServiceResult<Mission>.NotFound("Missão não encontrada.");
        }

        mission.Name = request.Name.Trim();
        mission.Description = request.Description?.Trim();
        mission.StartDate = NormalizeToUtc(request.StartDate);
        mission.EndDate = NormalizeToUtc(request.EndDate);
        mission.Status = request.Status;

        var shouldUpdateScope = request.ScopeId != Guid.Empty;
        if (shouldUpdateScope)
        {
            var scopeResolution = await missionScopeResolver.ResolveScopeOrganizationIdAsync(
                request.ScopeType,
                request.ScopeId,
                cancellationToken: cancellationToken);
            if (!scopeResolution.IsSuccess)
            {
                return ServiceResult<Mission>.NotFound(scopeResolution.Error ?? "Escopo não encontrado.");
            }

            mission.OrganizationId = scopeResolution.Value;
            ApplyScope(mission, request.ScopeType, request.ScopeId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<Mission>.Success(mission);
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

        // Buscar o colaborador com navegação para Team, Workspace e Organization
        // IgnoreQueryFilters() permite encontrar o colaborador mesmo se o tenant selecionado for diferente
        var collaborator = await dbContext.Collaborators
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(c => c.Team!)
                .ThenInclude(t => t.Workspace!)
                    .ThenInclude(w => w.Organization)
            .FirstOrDefaultAsync(c => c.Id == collaboratorId, cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult<PagedResult<Mission>>.NotFound("Colaborador não encontrado.");
        }

        var teamId = collaborator.TeamId;
        var workspaceId = collaborator.Team?.WorkspaceId;
        var organizationId = collaborator.OrganizationId;

        // Buscar missões do colaborador, team, workspace ou organization
        // Org-scoped missions: OrganizationId matches AND no other scope FK is set
        var query = dbContext.Missions
            .AsNoTracking()
            .Where(m =>
                m.CollaboratorId == collaboratorId ||
                (teamId.HasValue && m.TeamId == teamId) ||
                (workspaceId.HasValue && m.WorkspaceId == workspaceId) ||
                (m.OrganizationId == organizationId && m.WorkspaceId == null && m.TeamId == null && m.CollaboratorId == null));

        // Aplicar filtro de busca por nome
        query = new MissionSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        // Paginação
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

    private static void ApplyScope(Mission mission, MissionScopeType scopeType, Guid scopeId)
    {
        // OrganizationId is always set as tenant discriminator (set in CreateAsync)
        // Scope FKs indicate the mission's scope level
        mission.WorkspaceId = null;
        mission.TeamId = null;
        mission.CollaboratorId = null;

        switch (scopeType)
        {
            case MissionScopeType.Organization:
                // No additional scope FK needed — OrganizationId already set
                break;
            case MissionScopeType.Workspace:
                mission.WorkspaceId = scopeId;
                break;
            case MissionScopeType.Team:
                mission.TeamId = scopeId;
                break;
            case MissionScopeType.Collaborator:
                mission.CollaboratorId = scopeId;
                break;
        }
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
