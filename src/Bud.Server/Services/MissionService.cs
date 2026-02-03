using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class MissionService(ApplicationDbContext dbContext) : IMissionService
{
    public async Task<ServiceResult<Mission>> CreateAsync(CreateMissionRequest request, CancellationToken cancellationToken = default)
    {
        var scopeResult = await ResolveScopeAsync(request.ScopeType, request.ScopeId, cancellationToken);
        if (scopeResult.IsFailure)
        {
            return ServiceResult<Mission>.NotFound(scopeResult.Error ?? "Escopo não encontrado.");
        }

        var organizationId = await ResolveOrganizationIdAsync(request.ScopeType, request.ScopeId, cancellationToken);
        if (organizationId is null)
        {
            return ServiceResult<Mission>.NotFound("Não foi possível determinar a organização para o escopo fornecido.");
        }

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            StartDate = NormalizeToUtc(request.StartDate),
            EndDate = NormalizeToUtc(request.EndDate),
            Status = request.Status,
            OrganizationId = organizationId.Value,
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
        mission.StartDate = NormalizeToUtc(request.StartDate);
        mission.EndDate = NormalizeToUtc(request.EndDate);
        mission.Status = request.Status;

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
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var query = dbContext.Missions.AsNoTracking();

        if (scopeType.HasValue && scopeId.HasValue)
        {
            query = scopeType.Value switch
            {
                // Org-scoped missions: OrganizationId matches AND no other scope FK is set
                MissionScopeType.Organization => query.Where(m =>
                    m.OrganizationId == scopeId.Value &&
                    m.WorkspaceId == null &&
                    m.TeamId == null &&
                    m.CollaboratorId == null),
                MissionScopeType.Workspace => query.Where(m => m.WorkspaceId == scopeId.Value),
                MissionScopeType.Team => query.Where(m => m.TeamId == scopeId.Value),
                MissionScopeType.Collaborator => query.Where(m => m.CollaboratorId == scopeId.Value),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m => m.Name.Contains(search.Trim()));
        }

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
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        // Buscar o colaborador com navegação para Team, Workspace e Organization
        var collaborator = await dbContext.Collaborators
            .AsNoTracking()
            .Include(c => c.Team)
                .ThenInclude(t => t.Workspace)
                    .ThenInclude(w => w.Organization)
            .FirstOrDefaultAsync(c => c.Id == collaboratorId, cancellationToken);

        if (collaborator is null)
        {
            return ServiceResult<PagedResult<Mission>>.NotFound("Colaborador não encontrado.");
        }

        var teamId = collaborator.TeamId;
        var workspaceId = collaborator.Team.WorkspaceId;
        var organizationId = collaborator.OrganizationId;

        // Buscar missões do colaborador, team, workspace ou organization
        // Org-scoped missions: OrganizationId matches AND no other scope FK is set
        var query = dbContext.Missions
            .AsNoTracking()
            .Where(m =>
                m.CollaboratorId == collaboratorId ||
                m.TeamId == teamId ||
                m.WorkspaceId == workspaceId ||
                (m.OrganizationId == organizationId && m.WorkspaceId == null && m.TeamId == null && m.CollaboratorId == null));

        // Aplicar filtro de busca por nome
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m => m.Name.Contains(search.Trim()));
        }

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
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

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

    private async Task<ServiceResult> ResolveScopeAsync(MissionScopeType scopeType, Guid scopeId, CancellationToken cancellationToken)
    {
        var exists = scopeType switch
        {
            MissionScopeType.Organization => await dbContext.Organizations.AnyAsync(o => o.Id == scopeId, cancellationToken),
            MissionScopeType.Workspace => await dbContext.Workspaces.AnyAsync(w => w.Id == scopeId, cancellationToken),
            MissionScopeType.Team => await dbContext.Teams.AnyAsync(t => t.Id == scopeId, cancellationToken),
            MissionScopeType.Collaborator => await dbContext.Collaborators.AnyAsync(c => c.Id == scopeId, cancellationToken),
            _ => false
        };

        if (!exists)
        {
            return ServiceResult.NotFound($"{scopeType} not found.");
        }

        return ServiceResult.Success();
    }

    private async Task<Guid?> ResolveOrganizationIdAsync(MissionScopeType scopeType, Guid scopeId, CancellationToken cancellationToken)
    {
        return scopeType switch
        {
            MissionScopeType.Organization => scopeId,
            MissionScopeType.Workspace => await dbContext.Workspaces
                .Where(w => w.Id == scopeId)
                .Select(w => (Guid?)w.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken),
            MissionScopeType.Team => await dbContext.Teams
                .Where(t => t.Id == scopeId)
                .Select(t => (Guid?)t.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken),
            MissionScopeType.Collaborator => await dbContext.Collaborators
                .Where(c => c.Id == scopeId)
                .Select(c => (Guid?)c.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken),
            _ => null
        };
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
