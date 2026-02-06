using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class MetricCheckinService(ApplicationDbContext dbContext) : IMetricCheckinService
{
    public async Task<ServiceResult<MetricCheckin>> CreateAsync(CreateMetricCheckinRequest request, Guid collaboratorId, CancellationToken cancellationToken = default)
    {
        var metric = await dbContext.MissionMetrics
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MissionMetricId, cancellationToken);

        if (metric is null)
        {
            return ServiceResult<MetricCheckin>.NotFound("Métrica não encontrada.");
        }

        if (metric.Type == MetricType.Quantitative && request.Value is null)
        {
            return ServiceResult<MetricCheckin>.Failure("Valor é obrigatório para métricas quantitativas.", ServiceErrorType.Validation);
        }

        if (metric.Type == MetricType.Qualitative && string.IsNullOrWhiteSpace(request.Text))
        {
            return ServiceResult<MetricCheckin>.Failure("Texto é obrigatório para métricas qualitativas.", ServiceErrorType.Validation);
        }

        // Garantir que o OrganizationId da métrica está definido
        if (metric.OrganizationId == Guid.Empty)
        {
            return ServiceResult<MetricCheckin>.Failure("Métrica sem organização definida.", ServiceErrorType.Validation);
        }

        var checkin = new MetricCheckin
        {
            Id = Guid.NewGuid(),
            OrganizationId = metric.OrganizationId,
            MissionMetricId = request.MissionMetricId,
            CollaboratorId = collaboratorId,
            Value = request.Value,
            Text = request.Text?.Trim(),
            CheckinDate = DateTime.SpecifyKind(request.CheckinDate, DateTimeKind.Utc),
            Note = request.Note?.Trim(),
            ConfidenceLevel = request.ConfidenceLevel
        };

        dbContext.MetricCheckins.Add(checkin);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<MetricCheckin>.Success(checkin);
    }

    public async Task<ServiceResult<MetricCheckin>> UpdateAsync(Guid id, UpdateMetricCheckinRequest request, CancellationToken cancellationToken = default)
    {
        var checkin = await dbContext.MetricCheckins.FindAsync([id], cancellationToken);

        if (checkin is null)
        {
            return ServiceResult<MetricCheckin>.NotFound("Check-in não encontrado.");
        }

        checkin.Value = request.Value;
        checkin.Text = request.Text?.Trim();
        checkin.CheckinDate = DateTime.SpecifyKind(request.CheckinDate, DateTimeKind.Utc);
        checkin.Note = request.Note?.Trim();
        checkin.ConfidenceLevel = request.ConfidenceLevel;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<MetricCheckin>.Success(checkin);
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var checkin = await dbContext.MetricCheckins.FindAsync([id], cancellationToken);

        if (checkin is null)
        {
            return ServiceResult.NotFound("Check-in não encontrado.");
        }

        dbContext.MetricCheckins.Remove(checkin);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<MetricCheckin>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var checkin = await dbContext.MetricCheckins
            .AsNoTracking()
            .FirstOrDefaultAsync(mc => mc.Id == id, cancellationToken);

        return checkin is null
            ? ServiceResult<MetricCheckin>.NotFound("Check-in não encontrado.")
            : ServiceResult<MetricCheckin>.Success(checkin);
    }

    public async Task<ServiceResult<PagedResult<MetricCheckin>>> GetAllAsync(Guid? missionMetricId, Guid? missionId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        // Buscar check-ins sem Include para evitar problemas com query filters
        var baseQuery = dbContext.MetricCheckins.AsNoTracking();

        if (missionMetricId.HasValue)
        {
            baseQuery = baseQuery.Where(mc => mc.MissionMetricId == missionMetricId.Value);
        }

        if (missionId.HasValue)
        {
            baseQuery = baseQuery.Where(mc => mc.MissionMetric.MissionId == missionId.Value);
        }

        var total = await baseQuery.CountAsync(cancellationToken);
        var items = await baseQuery
            .OrderByDescending(mc => mc.CheckinDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Carregar Collaborators separadamente ignorando query filters
        if (items.Count > 0)
        {
            var collaboratorIds = items.Select(c => c.CollaboratorId).Distinct().ToList();
            var collaborators = await dbContext.Collaborators
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => collaboratorIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, cancellationToken);

            foreach (var item in items)
            {
                if (collaborators.TryGetValue(item.CollaboratorId, out var collaborator))
                {
                    item.Collaborator = collaborator;
                }
            }
        }

        var result = new PagedResult<MetricCheckin>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<MetricCheckin>>.Success(result);
    }
}
