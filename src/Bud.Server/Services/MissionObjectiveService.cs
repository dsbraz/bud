using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class MissionObjectiveService(ApplicationDbContext dbContext) : IMissionObjectiveService
{
    public async Task<ServiceResult<MissionObjective>> CreateAsync(
        CreateMissionObjectiveRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var mission = await dbContext.Missions
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == request.MissionId, cancellationToken);

            if (mission is null)
            {
                return ServiceResult<MissionObjective>.NotFound("Missão não encontrada.");
            }

            if (request.ParentObjectiveId.HasValue)
            {
                var parent = await dbContext.MissionObjectives
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == request.ParentObjectiveId.Value, cancellationToken);

                if (parent is null)
                {
                    return ServiceResult<MissionObjective>.NotFound("Objetivo pai não encontrado.");
                }

                if (parent.MissionId != request.MissionId)
                {
                    return ServiceResult<MissionObjective>.Failure(
                        "Objetivo pai deve pertencer à mesma missão.", ServiceErrorType.Validation);
                }
            }

            var objective = MissionObjective.Create(
                Guid.NewGuid(),
                mission.OrganizationId,
                request.MissionId,
                request.Name,
                request.Description);

            objective.SetParent(request.ParentObjectiveId);

            dbContext.MissionObjectives.Add(objective);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult<MissionObjective>.Success(objective);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<MissionObjective>.Failure(ex.Message, ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult<MissionObjective>> UpdateAsync(
        Guid id, UpdateMissionObjectiveRequest request, CancellationToken cancellationToken = default)
    {
        var objective = await dbContext.MissionObjectives.FindAsync([id], cancellationToken);

        if (objective is null)
        {
            return ServiceResult<MissionObjective>.NotFound("Objetivo não encontrado.");
        }

        try
        {
            objective.UpdateDetails(request.Name, request.Description);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult<MissionObjective>.Success(objective);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<MissionObjective>.Failure(ex.Message, ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var objective = await dbContext.MissionObjectives.FindAsync([id], cancellationToken);

        if (objective is null)
        {
            return ServiceResult.NotFound("Objetivo não encontrado.");
        }

        var hasChildren = await dbContext.MissionObjectives
            .AnyAsync(o => o.ParentObjectiveId == id, cancellationToken);

        if (hasChildren)
        {
            return ServiceResult.Failure(
                "Não é possível excluir um objetivo que possui sub-objetivos. Remova os sub-objetivos primeiro.",
                ServiceErrorType.Validation);
        }

        dbContext.MissionObjectives.Remove(objective);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<MissionObjective>> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var objective = await dbContext.MissionObjectives
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return objective is null
            ? ServiceResult<MissionObjective>.NotFound("Objetivo não encontrado.")
            : ServiceResult<MissionObjective>.Success(objective);
    }

    public async Task<ServiceResult<PagedResult<MissionObjective>>> GetByMissionAsync(
        Guid missionId, Guid? parentObjectiveId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.MissionObjectives
            .AsNoTracking()
            .Where(o => o.MissionId == missionId);

        // Filter by parent: null = top-level objectives only
        query = parentObjectiveId.HasValue
            ? query.Where(o => o.ParentObjectiveId == parentObjectiveId.Value)
            : query.Where(o => o.ParentObjectiveId == null);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(o => o.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<MissionObjective>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<MissionObjective>>.Success(result);
    }
}
