using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class MissionTemplateService(ApplicationDbContext dbContext) : IMissionTemplateService
{
    public async Task<ServiceResult<MissionTemplate>> CreateAsync(CreateMissionTemplateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // OrganizationId será preenchido por TenantSaveChangesInterceptor quando necessário.
            var template = MissionTemplate.Create(
                Guid.NewGuid(),
                Guid.Empty,
                request.Name,
                request.Description,
                request.MissionNamePattern,
                request.MissionDescriptionPattern);

            template.ReplaceMetrics(request.Metrics.Select(m => new MissionTemplateMetricDraft(
                m.Name,
                m.Type,
                m.OrderIndex,
                m.QuantitativeType,
                m.MinValue,
                m.MaxValue,
                m.Unit,
                m.TargetText)));

            dbContext.MissionTemplates.Add(template);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult<MissionTemplate>.Success(template);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<MissionTemplate>.Failure(ex.Message, ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult<MissionTemplate>> UpdateAsync(Guid id, UpdateMissionTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = await dbContext.MissionTemplates
            .Include(t => t.Metrics)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (template is null)
        {
            return ServiceResult<MissionTemplate>.NotFound("Template de missão não encontrado.");
        }

        try
        {
            template.UpdateBasics(
                request.Name,
                request.Description,
                request.MissionNamePattern,
                request.MissionDescriptionPattern);
            template.SetActive(request.IsActive);

            var previousMetrics = template.Metrics.ToList();
            template.ReplaceMetrics(request.Metrics.Select(m => new MissionTemplateMetricDraft(
                m.Name,
                m.Type,
                m.OrderIndex,
                m.QuantitativeType,
                m.MinValue,
                m.MaxValue,
                m.Unit,
                m.TargetText)));

            dbContext.MissionTemplateMetrics.RemoveRange(previousMetrics);
            dbContext.MissionTemplateMetrics.AddRange(template.Metrics);
            await dbContext.SaveChangesAsync(cancellationToken);

        // Reload to include new metrics in result
            template = await dbContext.MissionTemplates
                .Include(t => t.Metrics.OrderBy(m => m.OrderIndex))
                .FirstAsync(t => t.Id == id, cancellationToken);

            return ServiceResult<MissionTemplate>.Success(template);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<MissionTemplate>.Failure(ex.Message, ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await dbContext.MissionTemplates.FindAsync([id], cancellationToken);

        if (template is null)
        {
            return ServiceResult.NotFound("Template de missão não encontrado.");
        }

        dbContext.MissionTemplates.Remove(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<MissionTemplate>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await dbContext.MissionTemplates
            .AsNoTracking()
            .Include(t => t.Metrics.OrderBy(m => m.OrderIndex))
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return template is null
            ? ServiceResult<MissionTemplate>.NotFound("Template de missão não encontrado.")
            : ServiceResult<MissionTemplate>.Success(template);
    }

    public async Task<ServiceResult<PagedResult<MissionTemplate>>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.MissionTemplates
            .AsNoTracking()
            .Include(t => t.Metrics.OrderBy(m => m.OrderIndex));

        IQueryable<MissionTemplate> filteredQuery = query;

        if (!string.IsNullOrWhiteSpace(search))
        {
            filteredQuery = dbContext.Database.IsNpgsql()
                ? filteredQuery.Where(t => EF.Functions.ILike(t.Name, $"%{search}%"))
                : filteredQuery.Where(t => t.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var total = await filteredQuery.CountAsync(cancellationToken);
        var items = await filteredQuery
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<MissionTemplate>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<MissionTemplate>>.Success(result);
    }
}
