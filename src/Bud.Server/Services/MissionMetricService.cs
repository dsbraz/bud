using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class MissionMetricService(ApplicationDbContext dbContext) : IMissionMetricService
{
    public async Task<ServiceResult<MissionMetric>> CreateAsync(CreateMissionMetricRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var mission = await dbContext.Missions
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == request.MissionId, cancellationToken);

            if (mission is null)
            {
                return ServiceResult<MissionMetric>.NotFound("Missão não encontrada.");
            }

            var metric = MissionMetric.Create(
                Guid.NewGuid(),
                mission.OrganizationId,
                request.MissionId,
                request.Name,
                request.Type);

            metric.ApplyTarget(request.Type, request.QuantitativeType, request.MinValue, request.MaxValue, request.Unit, request.TargetText);

            if (request.MissionObjectiveId.HasValue)
            {
                var objective = await dbContext.MissionObjectives
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == request.MissionObjectiveId.Value, cancellationToken);

                if (objective is null)
                {
                    return ServiceResult<MissionMetric>.NotFound("Objetivo não encontrado.");
                }

                if (objective.MissionId != request.MissionId)
                {
                    return ServiceResult<MissionMetric>.Failure(
                        "Objetivo deve pertencer à mesma missão.", ServiceErrorType.Validation);
                }

                metric.MissionObjectiveId = request.MissionObjectiveId.Value;
            }

            dbContext.MissionMetrics.Add(metric);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult<MissionMetric>.Success(metric);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<MissionMetric>.Failure(ex.Message, ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult<MissionMetric>> UpdateAsync(Guid id, UpdateMissionMetricRequest request, CancellationToken cancellationToken = default)
    {
        var metric = await dbContext.MissionMetrics.FindAsync([id], cancellationToken);

        if (metric is null)
        {
            return ServiceResult<MissionMetric>.NotFound("Métrica da missão não encontrada.");
        }

        try
        {
            metric.UpdateDefinition(request.Name, request.Type);
            metric.ApplyTarget(request.Type, request.QuantitativeType, request.MinValue, request.MaxValue, request.Unit, request.TargetText);

            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult<MissionMetric>.Success(metric);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<MissionMetric>.Failure(ex.Message, ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var metric = await dbContext.MissionMetrics.FindAsync([id], cancellationToken);

        if (metric is null)
        {
            return ServiceResult.NotFound("Métrica da missão não encontrada.");
        }

        dbContext.MissionMetrics.Remove(metric);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<MissionMetric>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var metric = await dbContext.MissionMetrics
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        return metric is null
            ? ServiceResult<MissionMetric>.NotFound("Métrica da missão não encontrada.")
            : ServiceResult<MissionMetric>.Success(metric);
    }

    public async Task<ServiceResult<PagedResult<MissionMetric>>> GetAllAsync(Guid? missionId, Guid? objectiveId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.MissionMetrics.AsNoTracking();

        if (missionId.HasValue)
        {
            query = query.Where(m => m.MissionId == missionId.Value);
        }

        if (objectiveId.HasValue)
        {
            query = query.Where(m => m.MissionObjectiveId == objectiveId.Value);
        }

        query = ApplyMetricNameSearch(query, search);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(m => m.Name)
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

    private IQueryable<MissionMetric> ApplyMetricNameSearch(IQueryable<MissionMetric> query, string? search)
    {
        return SearchQueryHelper.ApplyCaseInsensitiveSearch(
            query,
            search,
            dbContext.Database.IsNpgsql(),
            (q, pattern) => q.Where(m => EF.Functions.ILike(m.Name, pattern)),
            (q, term) => q.Where(m => m.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
