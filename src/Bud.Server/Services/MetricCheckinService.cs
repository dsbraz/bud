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

        var query = dbContext.MetricCheckins.AsNoTracking();

        if (missionMetricId.HasValue)
        {
            query = query.Where(mc => mc.MissionMetricId == missionMetricId.Value);
        }

        if (missionId.HasValue)
        {
            query = query.Where(mc => mc.MissionMetric.MissionId == missionId.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(mc => mc.CheckinDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

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
